namespace ABox.Features.Threads;

internal static class SessionSeed
{
    public const string SavePrompt =
        "Summarize where this thread landed in one or two sentences — a recap for the journal that I can refine into the thread's synthesis.";

    public static string For(Thread thread)
    {
        var synthesis = string.IsNullOrWhiteSpace(thread.Synthesis) ? "Nothing captured yet." : thread.Synthesis.Trim();
        var openPoints = thread.OpenPoints.Count == 0
            ? "None."
            : string.Join(Environment.NewLine, thread.OpenPoints.Select(p => $"- {p.Text}"));

        return $"""
            You are picking up a parked thread titled "{thread.Title}".

            Where it stands:
            {synthesis}

            Open questions:
            {openPoints}

            Help me carry it forward.
            """;
    }
}
