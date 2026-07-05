namespace ABox.DocEngine;

public sealed class ParsedBlock
{
    public required string Type { get; init; }
    public string Title { get; set; } = "";
    public string? Group { get; init; }
    public bool Unknown { get; init; }
    public Dictionary<string, string> Attrs { get; } = new();
    public string Body { get; set; } = "";
    public List<string> Lines { get; } = new();
    public List<ParsedBlock> Children { get; } = new();

    // Every block is addressable by a stable id — an explicit `<!-- id: … -->` handle when present,
    // else derived (a member from its title, a singleton from its type). Explicit is the override: the
    // author writes one only to keep a handle stable across a retitle or to break a derived collision.
    public bool HasExplicitId => Attrs.ContainsKey("id");

    public string EffectiveId => Attrs.TryGetValue("id", out var id) ? id
        : Title.Length > 0 ? Catalog.Slug(Title)
        : Type;
}
