namespace ABox.Features.Threads.Api;

public sealed record ThreadEntryDto(DateTimeOffset At, Author Author, string Summary, string? Doc);
