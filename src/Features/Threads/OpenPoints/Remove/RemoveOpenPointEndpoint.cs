using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.OpenPoints.Remove;

internal sealed class RemoveOpenPointEndpoint(IRepository<Thread> threads) : Endpoint<RemoveOpenPointRequest, ThreadDto>
{
    public override void Configure()
    {
        Delete("/threads/{id}/openpoints/{pointId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RemoveOpenPointRequest req, CancellationToken ct)
    {
        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // The margin forgets: removing an already-gone point is a no-op success, not a 404 — DELETE stays idempotent.
        var updated = thread with { OpenPoints = [.. thread.OpenPoints.Where(p => p.Id != req.PointId)] };
        await threads.Update(updated, ct);

        await Send.OkAsync(updated.ToDto(), ct);
    }
}
