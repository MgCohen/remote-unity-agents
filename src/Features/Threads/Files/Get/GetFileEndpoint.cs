using FastEndpoints;

namespace ABox.Features.Threads.Files.Get;

internal sealed class GetFileEndpoint(IThreadFiles files) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/threads/{id}/files/{**path}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var path = Route<string>("path", isRequired: false) ?? string.Empty;

        Stream? stream;
        try
        {
            stream = await files.Get(id, path, ct);
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (stream is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.StreamAsync(stream, contentType: "application/octet-stream", cancellation: ct);
    }
}
