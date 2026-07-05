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
        Assert.False(Directory.Exists(Path.Combine(_dir, "other")));
    }

    [Rule("ThreadFiles.List → every file as a folder-prefixed relative path")]
    [Fact]
    public async Task List_returns_folder_prefixed_paths()
    {
        var files = NewFiles();
        Assert.Empty(files.List(_threadId));
        await files.Save(_threadId, "sessions/one.jsonl", Bytes("s"));
        await files.Save(_threadId, "artifacts/sketch.md", Bytes("a"));

        var listed = files.List(_threadId);

        Assert.Equal(["artifacts/sketch.md", "sessions/one.jsonl"], listed);
        Assert.Empty(NewFiles().List(Guid.NewGuid()));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
