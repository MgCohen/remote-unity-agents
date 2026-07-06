namespace ABox.Features.Threads.Api;

public sealed record ThreadDto(
    Guid Id,
    string Title,
    ThreadState State,
    string Synthesis,
    DateTimeOffset? SynthesizedAt,
    IReadOnlyList<ThreadEntryDto> Entries,
    IReadOnlyList<OpenPointDto> OpenPoints,
    DateTimeOffset CreatedAt);
