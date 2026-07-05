namespace ABox.Features.Threads.Api;

public sealed record SetStateRequest(Guid Id, ThreadState State);
