using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AsuMech.Core.Game;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Core.SimObjects;
using AsuMech.Multiplayer;
using AsuMech.Network;
using AsuMech.Scenarios;

namespace AsuMech.Windows.Asu;

// What the last run did that a report needs and the sim doesn't keep: each enemy action as it
// resolved (who it hit and killed) and each failure, in run time. Cleared when a run starts.
internal static class RunJournal
{
    private const int MaxActions = 80;
    private const int MaxFailures = 30;

    public sealed record Resolved(float T, uint ActionId, string Action, string Caster, string[] Hit, string[] Killed);

    public sealed record Failure(float T, string Reason);

    private static readonly List<Resolved> actions = [];
    private static readonly List<Failure> failures = [];
    private static Game? game;

    public static IReadOnlyList<Resolved> Actions => actions;
    public static IReadOnlyList<Failure> Failures => failures;

    public static void Install(Game target)
    {
        game = target;
        target.RunStarted += (_, _) => { actions.Clear(); failures.Clear(); };
        target.MechanicFailed += reason => Add(failures, new Failure(Now(), reason), MaxFailures);
        target.World.ActionResolved += r => Add(actions, new Resolved(Now(), r.Action.ActionId, Natives.Data.ActionName(r.Action.ActionId),
            Who(r.Caster), r.Hits.Select(Who).ToArray(), r.Killed.Select(Who).ToArray()), MaxActions);
    }

    private static float Now() => game?.World.Events.Elapsed ?? 0f;

    // Seats by role, never by a character's name.
    public static string Who(SimCharacter c) => c is ISimPartyMember m ? m.Role.ToString() : c is SimEnemy ? "enemy" : "other";

    private static void Add<T>(List<T> list, T item, int cap)
    {
        list.Add(item);
        if (list.Count > cap) list.RemoveAt(0);
    }
}

// A report's JSON: what the player wrote plus everything needed to replay and fix the run they
// were in. No character name or world: party seats are roles.
internal static class RunReport
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static JsonObject Build(string kind, string text, IScenario? selected, string? selectedStrat)
    {
        var game = Plugin.Game;
        var run = game.LastRun;
        var scenario = run?.Scenario ?? selected;
        var report = new JsonObject
        {
            ["kind"] = kind,
            ["text"] = text.Length <= RunReportWire.MaxTextChars ? text : text[..RunReportWire.MaxTextChars],
            ["version"] = PluginBuildInfo.Version,
            ["build"] = PluginBuildInfo.ShortChecksum,
            ["createdUtc"] = DateTime.UtcNow,
        };
        if (scenario is null) return report;
        report["scenario"] = new JsonObject
        {
            ["type"] = scenario.GetType().Name,
            ["name"] = scenario.DisplayName(),
            ["zone"] = scenario.Phase.Zone.ShortName,
            ["strat"] = run?.SelectedAi is { } i && i >= 0 && i < scenario.AiStrats.Count ? scenario.AiStrats[i].Name : selectedStrat,
        };
        if (kind != RunReportWire.Bug || run is null) return report;

        var world = game.World;
        var learn = Learn.LearnSession.Current;
        report["run"] = new JsonObject
        {
            ["status"] = run.Status.ToString(),
            ["mode"] = learn is { } l && ReferenceEquals(l.Scenario, scenario) ? $"learn:{l.Level}" : Core.SimObjects.DebugBotControl.Enabled ? "watch" : RunState.InSession ? "multiplayer" : "practice",
            ["elapsedSeconds"] = Math.Round(world.Events.Elapsed, 2),
            ["speed"] = game.EventTimeScale,
            ["seed"] = world.Rng.Seed,
            ["playerRole"] = world.Party.PlayerRole.ToString(),
            ["godMode"] = game.GodMode,
            ["issues"] = new JsonArray(run.Issues.Select(x => (JsonNode)new JsonObject { ["t"] = Math.Round(x.Time, 2), ["description"] = x.Description }).ToArray()),
        };
        report["failures"] = new JsonArray(RunJournal.Failures.Select(f => (JsonNode)new JsonObject { ["t"] = Math.Round(f.T, 2), ["reason"] = f.Reason }).ToArray());
        report["recentActions"] = new JsonArray(RunJournal.Actions.Select(a => (JsonNode)new JsonObject
        {
            ["t"] = Math.Round(a.T, 2), ["actionId"] = a.ActionId, ["action"] = a.Action, ["caster"] = a.Caster,
            ["hit"] = new JsonArray(a.Hit.Select(h => (JsonNode)h).ToArray()), ["killed"] = new JsonArray(a.Killed.Select(h => (JsonNode)h).ToArray()),
        }).ToArray());
        report["party"] = Party(world);
        try { report["state"] = JsonSerializer.SerializeToNode(AsuMech.Learn.Tools.StateSnapshot.Of(scenario)); }
        catch (Exception e) { report["state"] = $"unreadable: {e.Message}"; }
        if (learn is { } session && ReferenceEquals(session.Scenario, scenario))
        {
            var step = session.Director.Current;
            report["lesson"] = new JsonObject
            {
                ["id"] = session.Lesson is { } lesson ? $"{lesson.Scenario}|{lesson.Strat}" : null,
                ["level"] = session.Level.ToString(),
                ["role"] = session.Role.ToString(),
                ["step"] = step is null ? null : new JsonObject
                {
                    ["key"] = step.Step is { } s ? AsuMech.Learn.MasteryStore.StepId(s.Key) : null,
                    ["title"] = step.Title, ["rule"] = step.Rule, ["cue"] = step.Cue,
                    ["target"] = Point(step.Evaluation.Target),
                },
            };
        }
        return report;
    }

    private static JsonArray Party(SimWorld world)
    {
        var seats = new JsonArray();
        foreach (var role in Enum.GetValues<Core.Game.Party.PartyRole>())
        {
            if (world.Party.Get(role) is not { } member) continue;
            var seat = new JsonObject
            {
                ["role"] = role.ToString(),
                ["player"] = ReferenceEquals(member, world.Party.Player),
                ["alive"] = member.IsAlive(),
                ["position"] = Point(member.Position),
            };
            if (world.LastOrders.TryGetValue(role, out var order))
                seat["lastOrder"] = new JsonObject { ["issuedAt"] = Math.Round(order.IssuedAt, 2), ["target"] = Point(order.Target), ["sprint"] = order.Sprint };
            seats.Add(seat);
        }
        return seats;
    }

    private static JsonObject Point(System.Numerics.Vector3 p) => new() { ["x"] = Math.Round(p.X, 2), ["z"] = Math.Round(p.Z, 2) };

    // A copy beside the plugin's config, whether or not it reaches the server.
    public static string Save(JsonObject report)
    {
        var dir = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd-HHmmss}_{(string?)report["kind"]}.json");
        File.WriteAllText(path, report.ToJsonString(Json));
        return path;
    }
}
