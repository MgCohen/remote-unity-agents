using ABox.Features.Threads.Api;

namespace ABox.Features.Threads;

internal sealed record ThreadEntry(DateTimeOffset At, Author Author, string Summary, EntryLink? Link);
