using ABox.Domain.Agents;

namespace ABox.Agents.Tests.Support;

// Wire-test double for ISessionSurface: deterministic and CLI-free, so open/save can be driven end-to-end
// before the real Claude-backed surface (Threads phase 4 step 3) exists. It echoes its inputs, so a test can
// pin the wiring — the SessionId that open mints, and the recap that save turns into a receipt + proposal.
public sealed class FakeSessionSurface : ISessionSurface
{
    private int _opened;

    public Task<SessionStart> Start(string prompt, CancellationToken ct = default)
    {
        var sessionId = $"fake-session-{Interlocked.Increment(ref _opened)}";
        return Task.FromResult(new SessionStart(sessionId, $"opened: {prompt}"));
    }

    public Task<string> Turn(string sessionId, string prompt, CancellationToken ct = default) =>
        Task.FromResult($"recap[{sessionId}]: {prompt}");
}
