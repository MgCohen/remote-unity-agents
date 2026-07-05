using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Files.List;

internal sealed class ListFilesEndpoint(IRepository<Thread> threads, IThreadFiles files) : Endpoint<ThreadByIdRequest, IReadOnlyList<string>>
{
    public override void Configure()
    {
        Get("/threads/{id}/files");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ThreadByIdRequest req, CancellationToken ct)
    {
        if (await threads.GetById(req.Id, ct) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(await files.List(req.Id, ct), ct);
    }
}
