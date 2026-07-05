using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;
using ThreadState = ABox.Features.Threads.Api.ThreadState;

namespace ABox.Features.Threads.List;

internal sealed class ListThreadsEndpoint(IRepository<Thread> threads) : Endpoint<ListThreadsRequest, IReadOnlyList<ThreadDto>>
{
    public override void Configure()
    {
        Get("/threads");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListThreadsRequest req, CancellationToken ct)
    {
        if (req.State is { } requested && !Enum.IsDefined(requested))
        {
            AddError(r => r.State, "Unknown state; a thread is Active, Completed, or Archived.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var state = req.State ?? ThreadState.Active;
        var all = await threads.GetAll(ct);

        await Send.OkAsync(
            [.. all.Where(t => t.State == state).OrderBy(t => t.CreatedAt).Select(t => t.ToDto())], ct);
    }
}
