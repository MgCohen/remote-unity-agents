using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Features.Threads.Files.Get;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Files.Save;

internal sealed class SaveFileEndpoint(IRepository<Thread> threads, IThreadFiles files) : EndpointWithoutRequest<ThreadFileDto>
{
    public override void Configure()
    {
        Put("/threads/{id}/files/{**path}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var path = Route<string>("path", isRequired: false) ?? string.Empty;

        if (await threads.GetById(id, ct) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        DocRef doc;
        try
        {
            doc = await files.Save(id, path, HttpContext.Request.Body, ct);
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(409, ct);
            return;
        }

        await Send.CreatedAtAsync<GetFileEndpoint>(new { id, path = doc.Path }, new ThreadFileDto(doc.Path), cancellation: ct);
    }
}
