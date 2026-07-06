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
        var wrote = 0L;
        try
        {
            await using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                wrote = await CopyCounting(content, fs, ct);
            if (wrote == 0) throw new ArgumentException("A thread file needs a body; an empty upload does not claim a name.", nameof(content));
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
        var found = new List<string>();
        Walk(folder, folder, found);
        found.Sort(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<string>>(found);
    }

    // Manual walk instead of EnumerateFiles(AllDirectories): a symlinked directory is never descended, so a
    // planted link can neither leak an outside tree into the listing nor spin a cycle.
    private static void Walk(string folder, string dir, List<string> into)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
        {
            if (IsSymlink(entry)) continue;
            if (Directory.Exists(entry)) Walk(folder, entry, into);
            else if (!IsStaging(entry)) into.Add(Relative(folder, entry));
        }
    }

    private static InvalidOperationException Taken(Guid threadId, string folder, string file) => new(
        $"'{Relative(folder, file)}' is already written on thread {threadId}; a name, once written, " +
        "is immutable — save the revision under a new name.");

    private string ThreadFolder(Guid threadId) => Path.Combine(root.Folder, "threads", threadId.ToString());

    private static bool IsStaging(string path) => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);

    private static bool IsSymlink(string path) =>
        File.Exists(path) || Directory.Exists(path)
            ? (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0
            : false;

    private static async Task<long> CopyCounting(Stream from, Stream to, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await from.ReadAsync(buffer, ct)) > 0)
        {
            await to.WriteAsync(buffer.AsMemory(0, read), ct);
            total += read;
        }
        return total;
    }

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
        if (Path.EndsInDirectorySeparator(file) || string.IsNullOrEmpty(Path.GetFileName(file)))
            throw new ArgumentException($"'{path}' names a folder, not a file; a thread file needs a filename.", nameof(path));
        if (EscapesBySymlink(folder, file))
            throw new ArgumentException($"'{path}' resolves through a symlink; a thread file must be a real path inside the thread's folder.", nameof(path));

        return (folder, file);
    }

    // GetFullPath does not resolve symlinks, so a planted link inside the folder could still point out. Reject
    // any existing component between the folder and the file that is a reparse point.
    private static bool EscapesBySymlink(string folder, string file)
    {
        var current = file;
        while (current.Length > folder.Length)
        {
            if (IsSymlink(current)) return true;
            current = Path.GetDirectoryName(current)!;
        }
        return false;
    }

    private static string Relative(string folder, string file) =>
        Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');
}
