using FastEndpoints;
using ABox.Domain.Agents;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Open;

internal sealed class OpenSessionEndpoint(IRepository<Thread> threads, ISessionSurface sessions)
    : Endpoint<OpenSessionRequest, OpenSessionResponse>
{
    public override void Configure()
    {
        Post("/threads/{id}/open");
        AllowAnonymous();
    }

    public override async Task HandleAsync(OpenSessionRequest req, CancellationToken ct)
    {
        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var start = await sessions.Start(SessionSeed.For(thread), ct);
        await Send.OkAsync(new OpenSessionResponse(start.SessionId, start.Reply), ct);
    }
}
