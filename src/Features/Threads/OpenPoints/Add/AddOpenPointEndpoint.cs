using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.OpenPoints.Add;

internal sealed class AddOpenPointEndpoint(IRepository<Thread> threads) : Endpoint<AddOpenPointRequest, ThreadDto>
{
    public override void Configure()
    {
        Post("/threads/{id}/openpoints");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddOpenPointRequest req, CancellationToken ct)
    {
        var text = req.Text?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            AddError(r => r.Text, "An open point needs its text.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var point = new OpenPoint(Guid.NewGuid(), DateTimeOffset.UtcNow, text);
        var updated = thread with { OpenPoints = [.. thread.OpenPoints, point] };
        await threads.Update(updated, ct);

        await Send.OkAsync(updated.ToDto(), ct);
    }
}
