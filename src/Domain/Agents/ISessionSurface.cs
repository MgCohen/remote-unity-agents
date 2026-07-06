namespace ABox.Domain.Agents;

public interface ISessionSurface
{
    Task<SessionStart> Start(string prompt, CancellationToken ct = default);

    Task<string> Turn(string sessionId, string prompt, CancellationToken ct = default);
}
