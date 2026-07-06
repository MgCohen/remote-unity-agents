namespace ABox.DocEngine;

public static class Grading
{
    // The grading plan: one focused judge call per section — the document against its doctype rubric,
    // then each block type present against its own. Small single-context rubrics per call, instead of
    // one grading blob that blurs doc-level and block-level concerns.
    public static IReadOnlyList<string> Sections(
        IReadOnlyDictionary<string, object?> doctype,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> defs,
        IReadOnlyList<ParsedBlock> blocks)
    {
        var sections = new List<string>();
        Add(sections, $"=== doc {Yaml.AsString(doctype.GetValueOrDefault("docType"))}",
            Yaml.AsMap(doctype.GetValueOrDefault("rubric")));
        foreach (var (type, members) in PresentTypes(blocks))
            if (defs.TryGetValue(type, out var def))
                Add(sections, Header(type, members), Yaml.AsMap(def.GetValueOrDefault("rubric")));
        return sections;
    }

    private static string Header(string type, IReadOnlyList<string> members) =>
        members.Count == 0 ? $"=== block {type}" : $"=== block {type} ({string.Join("; ", members)})";

    private static void Add(List<string> sections, string header, IReadOnlyDictionary<string, object?>? rubric)
    {
        if (rubric is null || rubric.Count == 0) return;
        var lines = rubric.Select(r => $"{r.Key}: {Yaml.AsString(r.Value)}");
        sections.Add(header + "\n" + string.Join("\n", lines));
    }

    private static IEnumerable<(string Type, IReadOnlyList<string> Members)> PresentTypes(IReadOnlyList<ParsedBlock> blocks)
    {
        var order = new List<string>();
        var members = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var b in Flatten(blocks))
        {
            if (!members.TryGetValue(b.Type, out var titles))
            {
                titles = new List<string>();
                members[b.Type] = titles;
                order.Add(b.Type);
            }
            if (b.Title.Length > 0) titles.Add(b.Title);
        }
        return order.Select(t => (t, (IReadOnlyList<string>)members[t]));
    }

    private static IEnumerable<ParsedBlock> Flatten(IReadOnlyList<ParsedBlock> blocks) =>
        blocks.SelectMany(b => new[] { b }.Concat(Flatten(b.Children)));
}
