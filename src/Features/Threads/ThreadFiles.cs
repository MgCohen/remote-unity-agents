using ABox.Infrastructure.Storage;

namespace ABox.Features.Threads;

internal sealed class ThreadFiles(StorageRoot root) : IThreadFiles
{
    public async Task<DocRef> Save(Guid threadId, string path, Stream content, CancellationToken ct = default)
    {
        var (folder, file) = Resolve(threadId, path);
        if (File.Exists(file)) throw Taken(threadId, folder, file);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        // Staged whole, then moved: a failed upload never claims the name (same-name races stay the accepted lost-update posture).
        var tmp = Path.Combine(Path.GetDirectoryName(file)!, $".{Path.GetFileName(file)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await content.CopyToAsync(fs, ct);
            File.Move(tmp, file);
        }
        catch (IOException) when (File.Exists(file))
        {
            throw Taken(threadId, folder, file);
        }
        finally
        {
            File.Delete(tmp);
        }

        return new DocRef(Relative(folder, file));
    }

    public Task<Stream?> Get(Guid threadId, string path, CancellationToken ct = default)
    {
        var (_, file) = Resolve(threadId, path);
        return Task.FromResult<Stream?>(File.Exists(file) ? File.OpenRead(file) : null);
    }

    public Task<IReadOnlyList<string>> List(Guid threadId, CancellationToken ct = default)
    {
        var folder = ThreadFolder(threadId);
        if (!Directory.Exists(folder)) return Task.FromResult<IReadOnlyList<string>>([]);
        return Task.FromResult<IReadOnlyList<string>>([.. Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => !IsStaging(f))
            .Select(f => Relative(folder, f))
            .OrderBy(f => f, StringComparer.Ordinal)]);
    }

    private static InvalidOperationException Taken(Guid threadId, string folder, string file) => new(
        $"'{Relative(folder, file)}' is already written on thread {threadId}; a name, once written, " +
        "is immutable — save the revision under a new name.");

    private string ThreadFolder(Guid threadId) => Path.Combine(root.Folder, "threads", threadId.ToString());

    private static bool IsStaging(string path) => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);

    private (string Folder, string File) Resolve(Guid threadId, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A file needs a name; pass a path relative to the thread's folder, e.g. 'artifacts/sketch.md'.", nameof(path));
        if (Path.IsPathRooted(path))
            throw new ArgumentException($"'{path}' is absolute; a thread file's path is relative to the thread's folder.", nameof(path));
        if (IsStaging(path))
            throw new ArgumentException($"'{path}' ends in '.tmp', the suffix reserved for in-flight uploads; pick another name.", nameof(path));

        var folder = ThreadFolder(threadId);
        var file = Path.GetFullPath(Path.Combine(folder, path));
        if (!file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException($"'{path}' escapes the thread's folder; a thread file lives inside it.", nameof(path));

        return (folder, file);
    }

    private static string Relative(string folder, string file) =>
        Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');
}
