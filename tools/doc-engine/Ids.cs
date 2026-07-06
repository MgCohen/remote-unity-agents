using System.Text.RegularExpressions;

namespace ABox.DocEngine;

public static class Ids
{
    private static readonly Regex H2 = new(@"^##\s+(.+?)\s*$");
    private static readonly Regex H3 = new(@"^###\s+(.+?)\s*$");
    private static readonly Regex AnyHeader = new(@"^#{2,5}\s");
    private static readonly Regex IdComment = new(@"^<!--\s*id:\s*([\w-]+)\s*-->\s*$");
    private static readonly Regex Handle = new(@"^b(\d+)$");

    // Stamp a stable `<!-- id: b<N> -->` handle under every singleton/member header that lacks one. The id is
    // a short opaque handle — an integer with a `b` prefix (so it never reads as a step's `##### N.` ordinal),
    // deliberately carrying nothing of the title, so a retitle or a reorder can never perturb it (the client
    // references it). Idempotent: an existing handle is left untouched; new ones continue the highest number.
    public static IReadOnlyList<string> Stamp(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> defs)
    {
        var groupSlugs = GroupSlugs(defs);
        var next = 0;
        foreach (var l in lines)
            if (IdComment.Match(l) is { Success: true } m && Handle.Match(m.Groups[1].Value) is { Success: true } h)
                next = Math.Max(next, int.Parse(h.Groups[1].Value));

        var result = new List<string>();
        var inGroup = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var m2 = H2.Match(line);
            var m3 = m2.Success ? Match.Empty : H3.Match(line);
            result.Add(line);
            if (m2.Success)
            {
                inGroup = groupSlugs.Contains(Catalog.Slug(m2.Groups[1].Value));
                if (!inGroup) StampAfter(lines, i, ref next, result);
            }
            else if (m3.Success && inGroup)
            {
                StampAfter(lines, i, ref next, result);
            }
        }
        return result;
    }

    private static void StampAfter(IReadOnlyList<string> lines, int header, ref int next, List<string> result)
    {
        for (var j = header + 1; j < lines.Count; j++)
        {
            if (AnyHeader.IsMatch(lines[j])) break;
            if (IdComment.IsMatch(lines[j])) return;
        }
        result.Add($"<!-- id: b{++next} -->");
    }

    private static HashSet<string> GroupSlugs(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> defs)
    {
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var def in defs.Values)
            if (Yaml.Truthy(def.GetValueOrDefault("collection")))
                slugs.Add(Catalog.Slug(Yaml.AsString(def.GetValueOrDefault("group")) ?? ""));
        return slugs;
    }
}
