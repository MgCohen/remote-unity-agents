using ABox.Features.Threads.Api;

namespace ABox.Features.Threads;

internal static class ThreadWire
{
    public static ThreadDto ToDto(this Thread thread) => new(
        thread.Id,
        thread.Title,
        thread.State,
        thread.Synthesis,
        thread.SynthesizedAt,
        [.. thread.Entries.Select(e => new ThreadEntryDto(e.At, e.Author, e.Summary, ToLink(e.Link)))],
        [.. thread.OpenPoints.Select(p => new OpenPointDto(p.Id, p.At, p.Text))],
        thread.CreatedAt);

    private static EntryLinkDto? ToLink(EntryLink? link) => link switch
    {
        null => null,
        EntryLink.Artifact a => new EntryLinkDto(EntryLinkKind.Artifact, a.Doc.Path),
        EntryLink.Session s => new EntryLinkDto(EntryLinkKind.Session, s.SessionId),
        _ => throw new ArgumentOutOfRangeException(nameof(link), link, "Unknown entry link kind."),
    };
}
