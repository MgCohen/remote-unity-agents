using System.Text.Json.Serialization;

namespace ABox.Features.Threads;

[JsonPolymorphic]
[JsonDerivedType(typeof(Artifact), "artifact")]
[JsonDerivedType(typeof(Session), "session")]
internal abstract record EntryLink
{
    internal sealed record Artifact(DocRef Doc) : EntryLink;
    internal sealed record Session(string SessionId) : EntryLink;
}
