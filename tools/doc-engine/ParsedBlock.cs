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

    // Every block is addressable by a stable `<!-- id: … -->` handle. The validator requires one; the stamper
    // (`docengine ids --write`) assigns a short opaque `b<N>` id the author never has to type. A step is the
    // exception — its ordinal (`##### N.`) is its id.
    public bool HasExplicitId => Attrs.ContainsKey("id");
}
