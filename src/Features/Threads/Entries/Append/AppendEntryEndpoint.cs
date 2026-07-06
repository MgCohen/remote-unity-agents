using FastEndpoints;
using ABox.Features.Threads.Api;
using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads.Entries.Append;

internal sealed class AppendEntryEndpoint(IRepository<Thread> threads) : Endpoint<AppendEntryRequest, ThreadDto>
{
    public override void Configure()
    {
        Post("/threads/{id}/entries");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AppendEntryRequest req, CancellationToken ct)
    {
        var summary = req.Summary?.Trim() ?? string.Empty;
        if (summary.Length == 0)
        {
            AddError(r => r.Summary, "An entry needs a summary.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (req.Author is not { } author || !Enum.IsDefined(author))
        {
            AddError(r => r.Author, "An entry needs its author: Human or Agent.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (await threads.GetById(req.Id, ct) is not { } thread)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        EntryLink? link = string.IsNullOrWhiteSpace(req.Artifact)
            ? null
            : new EntryLink.Artifact(new DocRef(req.Artifact.Trim()));
        var entry = new ThreadEntry(DateTimeOffset.UtcNow, author, summary, link);
        var updated = thread with { Entries = [.. thread.Entries, entry] };
        await threads.Update(updated, ct);

        await Send.OkAsync(updated.ToDto(), ct);
    }
}
