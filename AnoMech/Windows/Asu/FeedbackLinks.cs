using System;
using System.Text;

namespace AnoMech.Windows.Asu;

internal enum FeedbackKind { Scenario, Strat, Bug }

// What a feedback issue may say about the run. Deliberately has no field for the character or
// world, so nothing that identifies the player can reach a public issue.
internal sealed record FeedbackContext(string? Scenario, string? Strat, string? Role, bool InSession, int? Seed, string Version);

internal sealed record FeedbackIssue(string Title, string Body, string Url);

// Pre-filled GitHub issue links. Labels come from the template's front matter: passing them in the
// URL needs triage rights and 404s for everyone else.
internal static class FeedbackLinks
{
    public const string Repository = "https://github.com/Asunnya/AsuMech";

    // GitHub answers 414 to long query strings.
    public const int MaxIssueBodyChars = 4000;
    private const string Truncated = "\n…(truncated)";

    public static FeedbackIssue Build(FeedbackKind kind, FeedbackContext context)
    {
        var (template, title) = kind switch
        {
            FeedbackKind.Scenario => ("scenario-request.md", "Scenario request: "),
            FeedbackKind.Strat => ("strat-request.md", $"Strat request: {context.Scenario}"),
            _ => ("bug-report.md", $"Bug: {context.Scenario}"),
        };
        var body = Truncate(Body(kind, context));
        var url = $"{Repository}/issues/new?template={Uri.EscapeDataString(template)}"
                  + $"&title={Uri.EscapeDataString(title.TrimEnd())}&body={Uri.EscapeDataString(body)}";
        return new FeedbackIssue(title.TrimEnd(), body, url);
    }

    public static string Truncate(string body)
        => body.Length <= MaxIssueBodyChars ? body : body[..(MaxIssueBodyChars - Truncated.Length)] + Truncated;

    private static string Body(FeedbackKind kind, FeedbackContext c)
    {
        var sb = new StringBuilder();
        switch (kind)
        {
            case FeedbackKind.Scenario:
                sb.AppendLine("**Fight and mechanic**").AppendLine().AppendLine();
                sb.AppendLine("**Why it's worth practicing**").AppendLine().AppendLine();
                break;
            case FeedbackKind.Strat:
                sb.AppendLine($"**Scenario**: {c.Scenario ?? "?"}").AppendLine();
                sb.AppendLine($"**Current strat**: {c.Strat ?? "none"}").AppendLine();
                sb.AppendLine("**Strat (raidplan or guide link)**").AppendLine().AppendLine();
                break;
            default:
                sb.AppendLine("**What happened**").AppendLine().AppendLine();
                sb.AppendLine("**What you expected**").AppendLine().AppendLine();
                sb.AppendLine("**Run details**");
                sb.AppendLine($"- Scenario: {c.Scenario ?? "none"}");
                sb.AppendLine($"- Strat: {c.Strat ?? "none"}");
                sb.AppendLine($"- Role: {c.Role ?? "Auto"}");
                sb.AppendLine($"- Mode: {(c.InSession ? "Multiplayer" : "Solo")}");
                sb.AppendLine($"- Seed: {(c.Seed is { } seed ? seed.ToString() : "unknown")}");
                break;
        }
        sb.AppendLine($"_AsuMech {c.Version}. No character name or world is included._");
        return sb.ToString();
    }
}
