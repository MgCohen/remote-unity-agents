using System.Net;
using System.Net.Http.Json;
using ABox.Features.Threads.Api;
using ABox.Host.Tests.Support;
using ABox.Infrastructure.Json;
using ThreadState = ABox.Features.Threads.Api.ThreadState;

namespace ABox.Threads.Tests.Wire;

public class ThreadsWireTests(WireApp app) : IClassFixture<WireApp>
{
    private HttpClient Client => app.CreateClient();

    private async Task<ThreadDto> Capture(string title)
    {
        using var res = await Client.PostAsJsonAsync("/threads", new AddThreadRequest(title));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options))!;
    }

    [Rule("POST /threads → a created Active thread from a title alone, rejecting a blank title")]
    [Fact]
    public async Task Post_mints_a_whole_thread_from_a_title()
    {
        using var res = await Client.PostAsJsonAsync("/threads", new AddThreadRequest("wire the outbox"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options);
        Assert.NotNull(dto);
        Assert.Equal("wire the outbox", dto.Title);
        Assert.Equal(ThreadState.Active, dto.State);
        Assert.Equal(string.Empty, dto.Synthesis);
        Assert.Null(dto.SynthesizedAt);
        Assert.Empty(dto.Entries);
        Assert.Empty(dto.OpenPoints);
        Assert.Contains(dto.Id.ToString(), res.Headers.Location?.ToString());
    }

    [Rule("POST /threads → a created Active thread from a title alone, rejecting a blank title")]
    [Fact]
    public async Task Post_rejects_a_blank_title()
    {
        using var res = await Client.PostAsJsonAsync("/threads", new AddThreadRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Rule("GET /threads/{id} → the thread with all four sub-surfaces, or 404 when absent")]
    [Fact]
    public async Task Get_returns_the_full_thread()
    {
        var minted = await Capture("rehydrate me");
        await Client.PostAsJsonAsync($"/threads/{minted.Id}/entries",
            new AppendEntryRequest(minted.Id, Author.Agent, "session receipt", "sessions/first.jsonl"));
        await Client.PostAsJsonAsync($"/threads/{minted.Id}/openpoints",
            new AddOpenPointRequest(minted.Id, "check the body cap"));

        var fetched = await Client.GetFromJsonAsync<ThreadDto>($"/threads/{minted.Id}", WireJson.Options);

        Assert.NotNull(fetched);
        Assert.Equal(minted.Id, fetched.Id);
        var entry = Assert.Single(fetched.Entries);
        Assert.Equal(Author.Agent, entry.Author);
        Assert.Equal("session receipt", entry.Summary);
        Assert.Equal("sessions/first.jsonl", entry.Doc);
        Assert.Equal("check the body cap", Assert.Single(fetched.OpenPoints).Text);
    }

    [Rule("GET /threads/{id} → the thread with all four sub-surfaces, or 404 when absent")]
    [Fact]
    public async Task Get_returns_404_for_an_unknown_id()
    {
        using var res = await Client.GetAsync($"/threads/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Rule("GET /threads → Active threads by default; a state query selects any single state")]
    [Fact]
    public async Task List_defaults_to_active_and_filters_by_state()
    {
        var active = await Capture("stays active");
        var archived = await Capture("gets shelved");
        await Client.PutAsJsonAsync($"/threads/{archived.Id}/state",
            new SetStateRequest(archived.Id, ThreadState.Archived));

        var defaults = await Client.GetFromJsonAsync<ThreadDto[]>("/threads", WireJson.Options);
        var shelf = await Client.GetFromJsonAsync<ThreadDto[]>("/threads?state=Archived", WireJson.Options);

        Assert.Contains(defaults!, t => t.Id == active.Id);
        Assert.DoesNotContain(defaults!, t => t.Id == archived.Id);
        Assert.Contains(shelf!, t => t.Id == archived.Id);
        Assert.DoesNotContain(shelf!, t => t.Id == active.Id);
    }

    [Rule("POST /threads/{id}/entries → the entry appended server-stamped, rejecting a blank summary")]
    [Fact]
    public async Task Append_adds_a_server_stamped_entry()
    {
        var thread = await Capture("journal me");
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);

        using var res = await Client.PostAsJsonAsync($"/threads/{thread.Id}/entries",
            new AppendEntryRequest(thread.Id, Author.Human, "a quick jot", null));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options);
        var entry = Assert.Single(dto!.Entries);
        Assert.Equal(Author.Human, entry.Author);
        Assert.Equal("a quick jot", entry.Summary);
        Assert.Null(entry.Doc);
        Assert.InRange(entry.At, before, DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Rule("POST /threads/{id}/entries → the entry appended server-stamped, rejecting a blank summary")]
    [Fact]
    public async Task Append_rejects_a_blank_summary()
    {
        var thread = await Capture("no empty jots");

        using var res = await Client.PostAsJsonAsync($"/threads/{thread.Id}/entries",
            new AppendEntryRequest(thread.Id, Author.Human, "  ", null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Rule("Entries have no update or delete surface")]
    [Fact]
    public async Task The_journal_offers_no_rewrite_verbs()
    {
        var thread = await Capture("append only");
        await Client.PostAsJsonAsync($"/threads/{thread.Id}/entries",
            new AppendEntryRequest(thread.Id, Author.Human, "immutable", null));

        using var put = await Client.PutAsJsonAsync($"/threads/{thread.Id}/entries/0", new { summary = "rewritten" });
        using var delete = await Client.DeleteAsync($"/threads/{thread.Id}/entries/0");
        using var deleteAll = await Client.DeleteAsync($"/threads/{thread.Id}/entries");

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, deleteAll.StatusCode);
    }

    [Rule("PUT /threads/{id}/synthesis → the synthesis replaced and SynthesizedAt stamped")]
    [Fact]
    public async Task Put_synthesis_replaces_and_stamps()
    {
        var thread = await Capture("synthesize me");

        using var res = await Client.PutAsJsonAsync($"/threads/{thread.Id}/synthesis",
            new PutSynthesisRequest(thread.Id, "we decided the margin forgets"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options);
        Assert.Equal("we decided the margin forgets", dto!.Synthesis);
        Assert.NotNull(dto.SynthesizedAt);
    }

    [Rule("POST + DELETE open points → the margin grows by minted id and forgets on removal, idempotently")]
    [Fact]
    public async Task Open_points_add_remove_and_forget()
    {
        var thread = await Capture("margin notes");
        using var added = await Client.PostAsJsonAsync($"/threads/{thread.Id}/openpoints",
            new AddOpenPointRequest(thread.Id, "must check Y"));
        var point = Assert.Single((await added.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options))!.OpenPoints);
        Assert.NotEqual(Guid.Empty, point.Id);

        using var removed = await Client.DeleteAsync($"/threads/{thread.Id}/openpoints/{point.Id}");
        var afterRemove = await removed.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options);
        using var removedAgain = await Client.DeleteAsync($"/threads/{thread.Id}/openpoints/{point.Id}");

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Empty(afterRemove!.OpenPoints);
        Assert.Equal(HttpStatusCode.OK, removedAgain.StatusCode);
    }

    [Rule("POST + DELETE open points → the margin grows by minted id and forgets on removal, idempotently")]
    [Fact]
    public async Task Open_points_reject_blank_text()
    {
        var thread = await Capture("no empty margin");

        using var res = await Client.PostAsJsonAsync($"/threads/{thread.Id}/openpoints",
            new AddOpenPointRequest(thread.Id, " "));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Rule("PUT /threads/{id}/state → every transition legal, and the default List reflects it")]
    [Fact]
    public async Task Archive_is_one_call_from_revival()
    {
        var thread = await Capture("shelve and revive");

        await Client.PutAsJsonAsync($"/threads/{thread.Id}/state", new SetStateRequest(thread.Id, ThreadState.Archived));
        var whileShelved = await Client.GetFromJsonAsync<ThreadDto[]>("/threads", WireJson.Options);
        using var revived = await Client.PutAsJsonAsync($"/threads/{thread.Id}/state",
            new SetStateRequest(thread.Id, ThreadState.Active));
        var afterRevival = await Client.GetFromJsonAsync<ThreadDto[]>("/threads", WireJson.Options);

        Assert.DoesNotContain(whileShelved!, t => t.Id == thread.Id);
        Assert.Equal(ThreadState.Active, (await revived.Content.ReadFromJsonAsync<ThreadDto>(WireJson.Options))!.State);
        Assert.Contains(afterRevival!, t => t.Id == thread.Id);
    }
}
