using ABox.Features.Threads;
using ABox.Infrastructure.Storage;
using Thread = ABox.Features.Threads.Thread;
using ThreadState = ABox.Features.Threads.Api.ThreadState;

namespace ABox.Threads.Tests.Unit;

public sealed class ThreadTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("threads-").FullName;

    private JsonRepository<Thread> NewRepository() => new(new StorageRoot(_dir));

    [Rule("Thread.Capture → an Active thread with only a title, every other surface empty")]
    [Fact]
    public void Capture_starts_active_and_empty()
    {
        var thread = Thread.Capture("outbox as a first-class surface");

        Assert.NotEqual(Guid.Empty, thread.Id);
        Assert.Equal("outbox as a first-class surface", thread.Title);
        Assert.Equal(ThreadState.Active, thread.State);
        Assert.Equal(string.Empty, thread.Synthesis);
        Assert.Null(thread.SynthesizedAt);
        Assert.Empty(thread.Entries);
        Assert.Empty(thread.OpenPoints);
    }

    [Rule("Thread persisted through the repository → reloads whole from a fresh repository")]
    [Fact]
    public async Task A_captured_thread_reloads_whole_after_a_restart()
    {
        var at = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var thread = Thread.Capture("agent cost budgets") with
        {
            State = ThreadState.Completed,
            Synthesis = "budgets per session, enforced by the guard",
            SynthesizedAt = at,
            Entries = [new ThreadEntry(at, Author.Agent, "session receipt", new DocRef("sessions/2026-07-01.jsonl"))],
            OpenPoints = [new OpenPoint(Guid.NewGuid(), at, "check the Kestrel body cap")],
        };
        await NewRepository().Add(thread);

        var reloaded = await NewRepository().GetById(thread.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(thread.Title, reloaded.Title);
        Assert.Equal(thread.State, reloaded.State);
        Assert.Equal(thread.Synthesis, reloaded.Synthesis);
        Assert.Equal(thread.SynthesizedAt, reloaded.SynthesizedAt);
        Assert.Equal(thread.Entries, reloaded.Entries);
        Assert.Equal(thread.OpenPoints, reloaded.OpenPoints);
        Assert.Equal(thread.CreatedAt, reloaded.CreatedAt);
    }

    [Rule("Thread mutated via with-expressions and updated → the reloaded thread is the mutation")]
    [Fact]
    public async Task An_updated_thread_reloads_as_the_mutation()
    {
        var thread = Thread.Capture("merge as archive plus receipts");
        await NewRepository().Add(thread);

        var jotted = thread with
        {
            State = ThreadState.Archived,
            Entries = [new ThreadEntry(DateTimeOffset.UtcNow, Author.Human, "folded into thread A", null)],
        };
        await NewRepository().Update(jotted);

        var reloaded = await NewRepository().GetById(thread.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(ThreadState.Archived, reloaded.State);
        Assert.Equal(jotted.Entries, reloaded.Entries);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
