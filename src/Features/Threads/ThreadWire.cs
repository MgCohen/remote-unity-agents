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
        [.. thread.Entries.Select(e => new ThreadEntryDto(e.At, e.Author, e.Summary, e.Doc?.Path))],
        [.. thread.OpenPoints.Select(p => new OpenPointDto(p.Id, p.At, p.Text))],
        thread.CreatedAt);
}
