using System.Text;
using ABox.Features.Threads;
using ABox.Infrastructure.Storage;

namespace ABox.Threads.Tests.Unit;

public sealed class ThreadFilesTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("thread-files-").FullName;
    private readonly Guid _threadId = Guid.NewGuid();

    private ThreadFiles NewFiles() => new(new StorageRoot(_dir));

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));

    [Rule("ThreadFiles.Save → the file written once; a taken name is refused forever")]
    [Fact]
    public async Task A_taken_name_is_refused()
    {
        var files = NewFiles();
        await files.Save(_threadId, "artifacts/sketch.md", Bytes("first"));

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            files.Save(_threadId, "artifacts/sketch.md", Bytes("second")));

        Assert.Contains("immutable", refused.Message);
        using var reader = new StreamReader((await files.Get(_threadId, "artifacts/sketch.md"))!);
        Assert.Equal("first", await reader.ReadToEndAsync());
    }

    [Rule("ThreadFiles content round-trips by the DocRef it minted")]
    [Fact]
    public async Task Content_round_trips_by_doc_ref()
    {
        var files = NewFiles();

        var doc = await files.Save(_threadId, "sessions/2026-07-05.jsonl", Bytes("{\"line\":1}"));

        Assert.Equal("sessions/2026-07-05.jsonl", doc.Path);
        using var reader = new StreamReader((await files.Get(_threadId, doc.Path))!);
        Assert.Equal("{\"line\":1}", await reader.ReadToEndAsync());
        Assert.Null(await files.Get(_threadId, "sessions/absent.jsonl"));
    }

    [Rule("ThreadFiles refuses paths that escape the thread's folder")]
    [Fact]
    public async Task Escaping_paths_are_refused()
    {
        var files = NewFiles();

        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "../other/steal.md", Bytes("x")));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "a/../../../steal.md", Bytes("x")));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "/etc/steal.md", Bytes("x")));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "  ", Bytes("x")));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Get(_threadId, "../other/steal.md"));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "artifacts/", Bytes("x")));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "artifacts/note/", Bytes("x")));
        Assert.False(Directory.Exists(Path.Combine(_dir, "threads", "other")));
        Assert.False(Directory.Exists(Path.Combine(_dir, "threads", _threadId.ToString())));
    }

    [Rule("ThreadFiles refuses to read or write through a symlink out of the folder")]
    [Fact]
    public async Task Symlinked_escapes_are_refused()
    {
        var files = NewFiles();
        var outside = Path.Combine(_dir, "outside");
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.txt"), "top secret");
        var threadDir = Path.Combine(_dir, "threads", _threadId.ToString());
        Directory.CreateDirectory(threadDir);
        Directory.CreateSymbolicLink(Path.Combine(threadDir, "leak.txt"), Path.Combine(outside, "secret.txt"));
        Directory.CreateSymbolicLink(Path.Combine(threadDir, "outdir"), outside);

        await Assert.ThrowsAsync<ArgumentException>(() => files.Get(_threadId, "leak.txt"));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Get(_threadId, "outdir/secret.txt"));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "outdir/planted.md", Bytes("x")));
        Assert.Empty(await files.List(_threadId));
        Assert.False(File.Exists(Path.Combine(outside, "planted.md")));
    }

    [Rule("ThreadFiles.Save → an empty body never claims a name")]
    [Fact]
    public async Task An_empty_upload_is_refused()
    {
        var files = NewFiles();

        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "artifacts/empty.md", new MemoryStream()));

        Assert.Null(await files.Get(_threadId, "artifacts/empty.md"));
        var doc = await files.Save(_threadId, "artifacts/empty.md", Bytes("now it has bytes"));
        Assert.Equal("artifacts/empty.md", doc.Path);
    }

    [Rule("ThreadFiles reserves the .tmp suffix for in-flight uploads")]
    [Fact]
    public async Task The_tmp_suffix_is_reserved_and_never_listed()
    {
        var files = NewFiles();
        await Assert.ThrowsAsync<ArgumentException>(() => files.Save(_threadId, "artifacts/note.TMP", Bytes("x")));

        await files.Save(_threadId, "artifacts/real.md", Bytes("kept"));
        var orphan = Path.Combine(_dir, "threads", _threadId.ToString(), "artifacts", ".crashed.abc123.tmp");
        await File.WriteAllTextAsync(orphan, "leftover");

        Assert.Equal(["artifacts/real.md"], await files.List(_threadId));
        await Assert.ThrowsAsync<ArgumentException>(() => files.Get(_threadId, "artifacts/.crashed.abc123.tmp"));
    }

    [Rule("ThreadFiles.List → every file as a folder-prefixed relative path")]
    [Fact]
    public async Task List_returns_folder_prefixed_paths()
    {
        var files = NewFiles();
        Assert.Empty(await files.List(_threadId));
        await files.Save(_threadId, "sessions/one.jsonl", Bytes("s"));
        await files.Save(_threadId, "artifacts/sketch.md", Bytes("a"));

        var listed = await files.List(_threadId);

        Assert.Equal(["artifacts/sketch.md", "sessions/one.jsonl"], listed);
        Assert.Empty(await NewFiles().List(Guid.NewGuid()));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
