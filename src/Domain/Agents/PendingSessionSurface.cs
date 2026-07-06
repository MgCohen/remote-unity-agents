namespace ABox.Domain.Agents;

// Provisional: the real session surface — a durable SessionId-keyed HOME driving a human-fed turn loop —
// lands in Threads phase 4 step 3. Registered so the Host boots and the open/save seam is real, yet every
// call fails loudly, because production must never serve fabricated session data.
public sealed class PendingSessionSurface : ISessionSurface
{
    private const string NotWired =
        "The agent session surface lands in Threads phase 4 step 3; open/save are not operable yet.";

    public Task<SessionStart> Start(string prompt, CancellationToken ct = default) =>
        throw new NotSupportedException(NotWired);

    public Task<string> Turn(string sessionId, string prompt, CancellationToken ct = default) =>
        throw new NotSupportedException(NotWired);
}
