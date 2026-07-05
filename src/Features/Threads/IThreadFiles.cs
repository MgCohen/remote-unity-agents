namespace ABox.Features.Threads;

internal interface IThreadFiles
{
    Task<DocRef> Save(Guid threadId, string path, Stream content, CancellationToken ct = default);
    Task<Stream?> Get(Guid threadId, string path, CancellationToken ct = default);
    Task<IReadOnlyList<string>> List(Guid threadId, CancellationToken ct = default);
}
