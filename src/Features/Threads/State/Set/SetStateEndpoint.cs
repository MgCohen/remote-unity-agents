using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.State.Set;

internal sealed class SetStateEndpoint(IRepository<Thread> threads) : Endpoint<SetStateRequest, ThreadDto>
{
    public override void Configure()
    {
        Put("/threads/{id}/state");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetStateRequest req, CancellationToken ct)
    {
        if (!Enum.IsDefined(req.State))
        {
            AddError(r => r.State, "Unknown state; a thread is Active, Completed, or Archived.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var updated = thread with { State = req.State };
        await threads.Update(updated, ct);

        await Send.OkAsync(updated.ToDto(), ct);
    }
}
