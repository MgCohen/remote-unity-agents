using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Synthesis.Put;

internal sealed class PutSynthesisEndpoint(IRepository<Thread> threads) : Endpoint<PutSynthesisRequest, ThreadDto>
{
    public override void Configure()
    {
        Put("/threads/{id}/synthesis");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PutSynthesisRequest req, CancellationToken ct)
    {
        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var updated = thread with
        {
            Synthesis = req.Synthesis ?? string.Empty,
            SynthesizedAt = DateTimeOffset.UtcNow,
        };
        await threads.Update(updated, ct);

        await Send.OkAsync(updated.ToDto(), ct);
    }
}
