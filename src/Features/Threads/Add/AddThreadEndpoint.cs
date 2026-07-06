using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Features.Threads.Get;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Add;

internal sealed class AddThreadEndpoint(IRepository<Thread> threads) : Endpoint<AddThreadRequest, ThreadDto>
{
    public override void Configure()
    {
        Post("/threads");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddThreadRequest req, CancellationToken ct)
    {
        var title = req.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            AddError(r => r.Title, "A thread needs a title.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var thread = Thread.Capture(title);
        await threads.Add(thread, ct);
        await Send.CreatedAtAsync<GetThreadEndpoint>(new { id = thread.Id }, thread.ToDto(), cancellation: ct);
    }
}
