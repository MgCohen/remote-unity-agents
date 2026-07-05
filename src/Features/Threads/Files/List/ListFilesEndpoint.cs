using FastEndpoints;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Files.List;

internal sealed class ListFilesEndpoint(IRepository<Thread> threads, IThreadFiles files) : EndpointWithoutRequest<IReadOnlyList<string>>
{
    public override void Configure()
    {
        Get("/threads/{id}/files");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        if (await threads.GetById(id, ct) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(files.List(id), ct);
    }
}
