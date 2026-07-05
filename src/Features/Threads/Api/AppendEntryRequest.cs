namespace ABox.Features.Threads.Api;

public sealed record AppendEntryRequest(Guid Id, Author? Author, string? Summary, string? Doc);
