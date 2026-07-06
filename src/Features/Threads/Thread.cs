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
    public static Thread Capture(string title)
    {
        var trimmed = title.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("A thread needs a title; pass the one-line idea being parked.", nameof(title));
        return new(Guid.NewGuid(), trimmed, ThreadState.Active, string.Empty, null, [], [], DateTimeOffset.UtcNow);
    }
}
