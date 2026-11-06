using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace AsuMech.Network;

// A player's report from the plugin (a problem in a run, a scenario or strat request): a JSON
// object whose "kind" and "text" the relay checks; the rest is the run's context, kept as sent.
public static class RunReportWire
{
    public const string Bug = "bug";
    public const string ScenarioRequest = "scenario-request";
    public const string StratRequest = "strat-request";
    public const int MaxBodyBytes = 256 * 1024;
    public const int MaxTextChars = 4000;

    public static bool IsValid(JsonObject report)
        => report["kind"]?.GetValueKind() == System.Text.Json.JsonValueKind.String
           && (string)report["kind"]! is Bug or ScenarioRequest or StratRequest
           && report["text"]?.GetValueKind() == System.Text.Json.JsonValueKind.String
           && ((string)report["text"]!).Length <= MaxTextChars;
}
