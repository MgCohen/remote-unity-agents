using FastEndpoints;
using ABox.Domain.Agents;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Save;

internal sealed class SaveSessionEndpoint(IRepository<Thread> threads, ISessionSurface sessions)
    : Endpoint<SaveSessionRequest, SaveSessionResponse>
{
    public override void Configure()
    {
        Post("/threads/{id}/save");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SaveSessionRequest req, CancellationToken ct)
    {
        var sessionId = req.SessionId?.Trim() ?? string.Empty;
        if (sessionId.Length == 0)
        {
            AddError(r => r.SessionId, "A save needs the session id it is closing out.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var recap = await sessions.Turn(sessionId, SessionSeed.SavePrompt, ct);
        var entry = new ThreadEntry(DateTimeOffset.UtcNow, Author.Agent, recap, new EntryLink.Session(sessionId));
        var updated = thread with { Entries = [.. thread.Entries, entry] };
        await threads.Update(updated, ct);

        await Send.OkAsync(new SaveSessionResponse(updated.ToDto(), recap), ct);
    }
}
