namespace ABox.Infrastructure.Sandbox;

public interface ISandbox : IAsyncDisposable
{
    // The host-side command line that runs `command` headless inside the box (no tty; env
    // applied). The caller drives it as a plain subprocess over stdin/stdout — the transport
    // for a real agent turn under ADR 0013 (`claude --print`, `codex exec`).
    string PipeExecLine(string command, IReadOnlyDictionary<string, string>? env = null);
}
