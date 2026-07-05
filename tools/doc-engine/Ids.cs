using System.Text.RegularExpressions;

namespace ABox.DocEngine;

public static class Ids
{
    private static readonly Regex H2 = new(@"^##\s+(.+?)\s*$");
    private static readonly Regex H3 = new(@"^###\s+(.+?)\s*$");
    private static readonly Regex AnyHeader = new(@"^#{2,5}\s");
    private static readonly Regex IdComment = new(@"^<!--\s*id:\s*([\w-]+)\s*-->\s*$");

    // Stamp a stable `<!-- id: … -->` handle under every singleton/member header that lacks one, so every
    // block is addressable by an id that persists across retitles (the client references it). Idempotent:
    // an existing handle is left untouched; only missing ones are filled. Steps keep their ordinal id.
    public static IReadOnlyList<string> Stamp(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> defs)
    {
        var groupSlugs = GroupSlugs(defs);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var l in lines)
            if (IdComment.Match(l) is { Success: true } m) used.Add(m.Groups[1].Value);

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
                var slug = Catalog.Slug(m2.Groups[1].Value);
                inGroup = groupSlugs.Contains(slug);
                if (!inGroup) StampAfter(lines, i, slug, used, result);
            }
            else if (m3.Success && inGroup)
            {
                StampAfter(lines, i, Catalog.Slug(m3.Groups[1].Value), used, result);
            }
        }
        return result;
    }

    private static void StampAfter(IReadOnlyList<string> lines, int header, string seed, HashSet<string> used, List<string> result)
    {
        for (var j = header + 1; j < lines.Count; j++)
        {
            if (AnyHeader.IsMatch(lines[j])) break;
            if (IdComment.IsMatch(lines[j])) return;
        }
        result.Add($"<!-- id: {Unique(Slug(seed), used)} -->");
    }

    // Id-safe slug: lowercase, every run of non-alphanumerics collapses to one '-', trimmed. Distinct from
    // Catalog.Slug (which only maps a header to its type and must stay a literal space→dash) — a block's id
    // must be a bare [a-z0-9-] handle a client can carry and the parser's `[\w-]` id regex can read back.
    private static string Slug(string text)
    {
        var chars = text.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) && c < 128 ? c : '-');
        var slug = new string(chars.ToArray());
        while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    private static string Unique(string seed, HashSet<string> used)
    {
        if (used.Add(seed)) return seed;
        var n = 2;
        while (!used.Add($"{seed}-{n}")) n++;
        return $"{seed}-{n}";
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
