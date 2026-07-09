namespace ABox.Domain.Agents.Claude;

public static class ClaudeProtocol
{
    // The subscription credential reaches the box here (ADR 0013): an OAuth token, not an API
    // key, so the ANTHROPIC_API_KEY scrub still selects subscription billing (oracle A1).
    public const string OAuthTokenEnvVar = "CLAUDE_CODE_OAUTH_TOKEN";

    // Bypass skips every check; Auto and Ask both run the default prompt-mode so
    // the PreToolUse hook gates each tool — they differ only in who decides
    // (Auto auto-approves, Ask routes to the resolver), not in the launch flag.
    public static string PermissionMode(PermissionPolicy policy) => policy switch
    {
        PermissionPolicy.Bypass => "bypassPermissions",
        PermissionPolicy.Auto => "default",
        PermissionPolicy.Ask => "default",
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown permission policy."),
    };

    // Oracle A8: --session-id and --resume are mutually exclusive.
    // --print runs claude headless (non-interactive): it reads the prompt from stdin, runs the
    // turn, and exits, so no PTY/TUI driving is needed. The system prompt is passed by FILE
    // (a multiline prompt is not a clean inline arg), referenced by its in-box path.
    public static List<string> BuildArgs(string sessionId, bool isResume, string permissionMode, string model, string? systemPromptFile, string? settingsFile = null)
    {
        var args = new List<string>();
        if (isResume) { args.Add("--resume"); args.Add(sessionId); }
        else { args.Add("--session-id"); args.Add(sessionId); }
        args.Add("--print");
        if (!string.IsNullOrEmpty(permissionMode)) { args.Add("--permission-mode"); args.Add(permissionMode); }
        if (!string.IsNullOrEmpty(model)) { args.Add("--model"); args.Add(model); }
        if (!string.IsNullOrEmpty(systemPromptFile)) { args.Add("--append-system-prompt-file"); args.Add(systemPromptFile); }
        if (!string.IsNullOrEmpty(settingsFile)) { args.Add("--settings"); args.Add(settingsFile); }
        return args;
    }

    // The non-secret env injected per turn via `docker exec -e` (ADR 0013): HOME at the mounted
    // onboarding home, the Stop/permission shim paths as in-box mount paths, the egress proxy.
    // Per-turn and per-exec: nothing here is baked into the image. The subscription credential is
    // deliberately NOT here — the box reads it from a session-mounted file at exec time
    // (BuildCredentialLauncher) so it never reaches the docker exec line.
    public static Dictionary<string, string> BuildBoxEnv(
        string homeMount, string signalPathInBox, string? permissionDirInBox, string? proxyUrl)
    {
        var env = new Dictionary<string, string>
        {
            ["HOME"] = homeMount,
            [ClaudeHooks.SignalEnvVar] = signalPathInBox,
        };
        if (permissionDirInBox is not null)
            env[ClaudeHooks.PermissionEnvVar] = permissionDirInBox;
        if (proxyUrl is { } proxy)
        {
            env["HTTPS_PROXY"] = proxy;
            env["HTTP_PROXY"] = proxy;
        }
        return env;
    }

    // A one-shot launcher the box runs in place of `claude`: it reads the subscription token from
    // a 0600 session-mounted file into the OAuth env var, then exec's claude (ADR 0013). The token
    // value is therefore never on the docker exec line and never in the container's `docker inspect`
    // env — only on a short-lived mount file. An OAuth token, not an API key, so the
    // ANTHROPIC_API_KEY scrub still selects subscription billing (oracle A1).
    public static string BuildCredentialLauncher(string credentialPathInBox) =>
        $"export {OAuthTokenEnvVar}=\"$(cat {credentialPathInBox})\"\nexec claude \"$@\"\n";
}
