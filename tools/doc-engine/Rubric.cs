namespace ABox.DocEngine;

public static class Rubric
{
    // The judge grades one concatenated rubric: the doctype's criteria, then the rubric of every block
    // type present in the instance — block criteria prefixed `<type>.` so ids stay unique per grading run.
    public static IReadOnlyList<string> Lines(
        IReadOnlyDictionary<string, object?> doctype,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> defs,
        IReadOnlyList<ParsedBlock> blocks)
    {
        var lines = new List<string>();
        Append(lines, "", Yaml.AsMap(doctype.GetValueOrDefault("rubric")));
        foreach (var type in PresentTypes(blocks))
            if (defs.TryGetValue(type, out var def))
                Append(lines, type + ".", Yaml.AsMap(def.GetValueOrDefault("rubric")));
        return lines;
    }

    private static void Append(List<string> lines, string prefix, IReadOnlyDictionary<string, object?>? rubric)
    {
        if (rubric is null) return;
        foreach (var (id, rule) in rubric) lines.Add($"{prefix}{id}: {Yaml.AsString(rule)}");
    }

    private static IEnumerable<string> PresentTypes(IReadOnlyList<ParsedBlock> blocks)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return Flatten(blocks).Select(b => b.Type).Where(seen.Add);
    }

    private static IEnumerable<ParsedBlock> Flatten(IReadOnlyList<ParsedBlock> blocks) =>
        blocks.SelectMany(b => new[] { b }.Concat(Flatten(b.Children)));
}
