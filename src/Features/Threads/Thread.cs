using ABox.Infrastructure.Storage;
using ThreadState = ABox.Features.Threads.Api.ThreadState;

namespace ABox.Features.Threads;

internal sealed record Thread(
    Guid Id,
    string Title,
    ThreadState State,
    string Synthesis,
    DateTimeOffset? SynthesizedAt,
    IReadOnlyList<ThreadEntry> Entries,
    IReadOnlyList<OpenPoint> OpenPoints,
    DateTimeOffset CreatedAt) : IEntity
{
    public static Thread Capture(string title) =>
        new(Guid.NewGuid(), title, ThreadState.Active, string.Empty, null, [], [], DateTimeOffset.UtcNow);
}
