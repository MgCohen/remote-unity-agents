using ABox.Infrastructure.CommandLine;
using ABox.Infrastructure.Sandbox;

namespace ABox.Domain.Agents.Claude;

public sealed class ClaudeProvider(ClaudeConfig config, IDecisionResolver resolver, AutoPolicy autoPolicy, SandboxSettings sandbox) : IProvider, IAsyncDisposable
{
    private const int PermissionPollMs = 200;
    private const int ResponseCapMs = 5 * 60_000;
    private const int ResolveTimeoutMs = 2_000;
    private const int ResolvePollMs = 100;
    private const int MaxOverallMs = 10 * 60_000;      // oracle A10: wall-clock cap → guaranteed teardown

    // One HOME for the provider's whole life, not per turn: claude writes the session JSONL
    // here, and a --resume turn opens a fresh box that must re-mount the same HOME to find it.
    // Disposed with the provider (which the Agent owns).
    private DirectoryInfo? _home;

    public async Task<DriveResult> DriveAsync(AgentRunRequest request, CancellationToken ct)
    {
        await SubscriptionGuard.CheckAsync(EnvScrub.ClaudeKeys, "claude", ct);

        if (config.Policy == PermissionPolicy.Bypass && DockerSandbox.RunsAsRoot)
            throw new InvalidOperationException(
                "Bypass policy needs claude's bypassPermissions mode, which claude refuses as root; " +
                "run the orchestrator as a non-root user so the box isn't root.");

        sandbox.EnsureCredentialConfined();

        var isResume = request.SessionId is not null;
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

        using var hook = ClaudeHooks.Create(gatePermissions: config.Policy != PermissionPolicy.Bypass, boxDir: DockerSandbox.SessionMount);

        // The system prompt travels as a file on the /session mount (a multiline prompt is not a
        // clean inline arg), referenced by its in-box path.
        var sysPromptName = $"sysprompt-{Guid.NewGuid():N}.txt";
        File.WriteAllText(Path.Combine(hook.HostDir, sysPromptName),
            AgentDirective.ComposeSystemPrompt(config.SystemPrompt, config.Resolution));
        var sysPromptInBox = $"{DockerSandbox.SessionMount}/{sysPromptName}";

        var home = _home ??= PrepareHome();
        var permissionMode = ClaudeProtocol.PermissionMode(config.Policy);
        var args = ClaudeProtocol.BuildArgs(sessionId, isResume, permissionMode, config.Model, sysPromptInBox, hook.SettingsPathInBox);
        var launchCommand = BuildLaunchCommand(hook, args, sandbox.SetupToken);

        using var deadlineCts = new CancellationTokenSource(MaxOverallMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, deadlineCts.Token);
        var dct = linkedCts.Token;

        var options = new SandboxOptions(
            Worktree: new DirectoryInfo(request.ProjectDir),
            SessionDir: new DirectoryInfo(hook.HostDir),
            Home: home,
            Image: sandbox.Image,
            Network: sandbox.Network,
            RequireInternalNetwork: sandbox.SetupToken is not null);
        await using var box = await DockerSandbox.OpenAsync(options, dct);

        // Headless: `claude --print` runs the turn non-interactively over a `docker exec -i`
        // pipe (no PTY/TUI). The prompt goes in on stdin; permission requests are resolved
        // concurrently while claude runs; the Stop hook + JSONL deliver the result as before.
        var psi = Shell.Command(box.PipeExecLine(launchCommand, BoxEnv(hook)));
        psi.RedirectStandardInput = true;
        await using var session = SubprocessSession.Start(psi, dct);

        await session.StandardInput.WriteAsync(request.Prompt);
        session.CompleteStdin();

        var pump = PumpPermissionsAsync(hook, session, dct);
        await session.WaitForExitAsync(ResponseCapMs, dct);
        await pump;

        hook.EmitTurnEnded(request.ProjectDir, sessionId);

        // Stop fired across the /session mount, so the final message + JSONL are on disk;
        // read them before dispose tears the box down (docker rm -f, oracle A10).
        var projectsRoot = Path.Combine(home.FullName, ".claude", "projects");
        var text = hook.ReadFinalMessage();
        if (string.IsNullOrWhiteSpace(text))
            text = await ResolveTextFallbackAsync(sessionId, request.Prompt, projectsRoot, dct);
        var transcript = ClaudeJsonl.TryReadLastTurnTranscript(sessionId, request.Prompt, projectsRoot) ?? [];
        var exitCode = hook.HasFired || !string.IsNullOrWhiteSpace(text) ? 0 : 1;
        return new DriveResult(text ?? "", sessionId, exitCode, session.RawStdout, transcript);
    }

    // The session HOME outlives each turn (resume re-mounts it), so cleanup is the provider's
    // job, not the turn's — the Agent disposes the provider when the run is done.
    public ValueTask DisposeAsync()
    {
        if (_home is { } home) TryDeleteDir(home);
        _home = null;
        return ValueTask.CompletedTask;
    }

    // The session HOME: a temp dir (writable mount where claude writes the JSONL) seeded once
    // from the non-secret onboarding skeleton so the first-run dialogs don't appear. The
    // credential is NOT here — it rides the box per turn via the credential launcher.
    private DirectoryInfo PrepareHome()
    {
        var home = Directory.CreateTempSubdirectory("ra-claude-home-");
        if (sandbox.OnboardingHome is { Exists: true } onboarding)
            CopyDir(onboarding, home);
        return home;
    }

    private static void CopyDir(DirectoryInfo src, DirectoryInfo dst)
    {
        dst.Create();
        foreach (var dir in src.GetDirectories())
            CopyDir(dir, dst.CreateSubdirectory(dir.Name));
        foreach (var file in src.GetFiles())
            file.CopyTo(Path.Combine(dst.FullName, file.Name), overwrite: true);
    }

    private Dictionary<string, string> BoxEnv(ClaudeHooks hook) =>
        ClaudeProtocol.BuildBoxEnv(
            DockerSandbox.HomeMount, hook.SignalPathInBox, hook.PermissionDirInBox, sandbox.ProxyUrl);

    // Unbilled turn → launch claude directly. Billed turn → write the token to a 0600 file on the
    // /session mount and launch a tiny script that reads it into the env in-box, so the value
    // never lands on the docker exec line nor in the container's `docker inspect` env.
    private static string BuildLaunchCommand(ClaudeHooks hook, List<string> args, string? setupToken)
    {
        var quotedArgs = string.Join(' ', args.Select(Shell.Quote));
        if (string.IsNullOrEmpty(setupToken)) return "claude " + quotedArgs;

        var credFile = Path.Combine(hook.HostDir, "credential");
        File.WriteAllText(credFile, setupToken);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(credFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        var credInBox = $"{DockerSandbox.SessionMount}/credential";
        File.WriteAllText(Path.Combine(hook.HostDir, "launch.sh"), ClaudeProtocol.BuildCredentialLauncher(credInBox));
        return $"sh {DockerSandbox.SessionMount}/launch.sh " + quotedArgs;
    }

    private static void TryDeleteDir(DirectoryInfo dir)
    {
        try { dir.Delete(recursive: true); }
        catch { /* best-effort: temp cleanup is non-fatal */ }
    }

    // A gated tool raises a PreToolUse request mid-turn and its shim blocks until we answer,
    // so the resolver must run concurrently with the headless `claude` process. Poll-drain
    // and resolve each request until claude exits, then drain once more for the ledger.
    private async Task PumpPermissionsAsync(ClaudeHooks hook, SubprocessSession session, CancellationToken ct)
    {
        while (!session.HasExited)
        {
            foreach (var pending in hook.DrainRequests())
                await ResolvePermissionAsync(hook, pending, ct);
            try { await Task.Delay(PermissionPollMs, ct); }
            catch (OperationCanceledException) { return; }
        }
        foreach (var pending in hook.DrainRequests())
            await ResolvePermissionAsync(hook, pending, ct);
    }

    // Auto applies the guardrail policy with no human in the loop (allow unless a
    // denylist rule blocks the command). Ask routes to the resolver; an unresolvable
    // call (null) denies — the safe, non-hanging default that replaces acceptEdits'
    // silent mid-turn block.
    private async Task ResolvePermissionAsync(ClaudeHooks hook, PermissionRequest request, CancellationToken ct)
    {
        if (config.Policy == PermissionPolicy.Auto)
        {
            var verdict = autoPolicy.Evaluate(request);
            hook.Respond(request, ClaudePermission.RenderResponse(verdict.Allow, verdict.Reason));
            return;
        }

        var allow = ClaudePermission.IsAllow(await resolver.ResolveAsync(ClaudePermission.ToQuestion(request), DecisionKind.Permission, ct));
        hook.Respond(request, ClaudePermission.RenderResponse(allow, allow ? "approved" : "denied (no approval)"));
    }

    // Fallback only: if the Stop hook never delivered the final text, recover it
    // from the per-session JSONL (oracle A6), polling briefly for the flush lag.
    private static async Task<string> ResolveTextFallbackAsync(string sessionId, string prompt, string projectsRoot, CancellationToken ct)
    {
        for (var waited = 0; waited <= ResolveTimeoutMs; waited += ResolvePollMs)
        {
            var text = ClaudeJsonl.TryReadLastAssistantText(sessionId, prompt, projectsRoot);
            if (!string.IsNullOrWhiteSpace(text)) return text;
            try { await Task.Delay(ResolvePollMs, ct); }
            catch (OperationCanceledException) { break; }
        }
        return "";
    }
}
