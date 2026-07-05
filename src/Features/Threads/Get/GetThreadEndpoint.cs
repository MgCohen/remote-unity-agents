using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Get;

internal sealed class GetThreadEndpoint(IRepository<Thread> threads) : Endpoint<ThreadByIdRequest, ThreadDto>
{
    public override void Configure()
    {
        Get("/threads/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ThreadByIdRequest req, CancellationToken ct)
    {
        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new ThreadDto(thread.Id, thread.Title, thread.State, thread.Synthesis, thread.SynthesizedAt, thread.CreatedAt), ct);
    }
}
