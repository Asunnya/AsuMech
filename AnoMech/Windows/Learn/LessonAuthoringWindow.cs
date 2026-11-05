using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Learn;
using AnoMech.Learn.Tools;
using AnoMech.Scenarios;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// What an author needs while a lesson runs: the dev folder the lessons reload from, the current
// order's key (to paste into the JSON), what the sim showed just before it (cue candidates), the
// run's rolls (the State), and the dev lessons' validation errors.
internal sealed class LessonAuthoringWindow : Window
{
    private const float CueWindowSeconds = 8f;
    private const float StateRefreshSeconds = 1f;

    private readonly Game game;
    private string devDir = Plugin.Config.LessonDevDir;
    private readonly List<(float Time, string Kind, string Name, string? Target)> seen = [];
    private readonly HashSet<(string, string, string?)> seenKeys = [];
    private readonly HashSet<ISimObject> knownChildren = [];
    private Dictionary<string, string> state = new();
    private float stateAt = -1f;
    private IScenario? stateOf;

    public LessonAuthoringWindow(Game game) : base("Lesson authoring###AsuMechLessonAuthoring")
    {
        this.game = game;
        Size = new Vector2(620f, 520f);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void PreDraw()
    {
        if (RunState.Live(game)) Observe();
        else if (seen.Count > 0 && game.World.Events.Elapsed < 0.5f) Reset();
    }

    private void Reset()
    {
        seen.Clear();
        seenKeys.Clear();
        knownChildren.Clear();
        state = new();
        stateAt = -1f;
    }

    // Casts, statuses and spawns the player could see, each noted the first time it shows.
    private void Observe()
    {
        var world = game.World;
        var now = world.Events.Elapsed;
        if (now < stateAt) Reset();
        foreach (var child in world.Children.ToList())
        {
            if (child is SimEnemy enemy)
            {
                if (knownChildren.Add(enemy) && now > 0.5f) Note(now, "spawn", enemy.ToString() ?? "enemy", null);
                if (enemy.Casting is { IsCasting: true } cast && cast.ActionId != 0) Note(now, "cast", Natives.Data.ActionName(cast.ActionId), null);
            }
            else if (child is SimTether tether) Note(now, "tether", $"tether {tether.TetherId}", null);
        }
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member) continue;
            foreach (var status in member.ActiveStatuses) Note(now, "status", Natives.Data.StatusName(status.StatusId), role.ToString());
        }
        if (now - stateAt >= StateRefreshSeconds && game.ActiveScenario is { } scenario)
        {
            state = StateSnapshot.Of(scenario);
            stateAt = now;
            stateOf = scenario;
        }
    }

    private void Note(float now, string kind, string name, string? target)
    {
        if (seenKeys.Add((kind, name, target))) seen.Add((now, kind, name, target));
    }

    public override void Draw()
    {
        using var theme = AsuTheme.Push();
        DrawDevFolder();
        ImGui.Separator();
        var session = LearnSession.Current;
        if (session is { Ended: false }) DrawSession(session);
        else ImGui.TextUnformatted("No lesson running. Start one from the Learn tab (Observe works without a lesson).");
        ImGui.Separator();
        DrawState();
    }

    private void DrawDevFolder()
    {
        ImGui.TextUnformatted("Dev folder");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 90f * AsuTheme.Scale);
        if (ImGui.InputText("##lesson-dev-dir", ref devDir, 512, ImGuiInputTextFlags.EnterReturnsTrue) || ImGui.IsItemDeactivatedAfterEdit())
        {
            Plugin.Config.LessonDevDir = devDir;
            Plugin.Config.Save();
            LessonLoader.DevDirectory = devDir;
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Reload##lesson-dev")) LessonLoader.Refresh(force: true);
        if (LessonLoader.DevDirectory is null) Muted("Point this at a folder of lesson JSON files (e.g. the repo's AnoMech/Learn/Lessons). They shadow the embedded lessons and reload on save.");
        else
        {
            Muted($"{LessonLoader.DevCount} lesson(s) from files, read {LessonLoader.LastReload:HH:mm:ss} UTC");
            using (AsuTheme.PushTextColor(AsuTheme.Bad))
                foreach (var error in LessonLoader.DevErrors) ImGui.TextWrapped(error);
            var catalog = game.Catalog.Scenarios;
            using (AsuTheme.PushTextColor(AsuTheme.Accent))
                foreach (var lesson in LessonLoader.All.Where(l => LessonLoader.DevCount > 0))
                    foreach (var problem in LessonLoader.Validate(lesson, catalog)) ImGui.TextWrapped($"{lesson.Scenario} / {lesson.Strat}: {problem}");
        }
    }

    private void DrawSession(LearnSession session)
    {
        var lesson = session.Lesson;
        ImGui.TextUnformatted($"{session.Scenario.GetType().Name} / {session.Strat.Name} · {session.Level} as {session.Role}{(lesson is null ? " · no lesson" : lesson.Draft ? " · draft" : "")}");
        if (session.Director.Current is not { } current)
        {
            Muted("Waiting for the strat's next order to the player's slot.");
            return;
        }
        var key = current.Order.Key;
        var line = key.Source.LastIndexOf(':') is var at and >= 0 ? key.Source[(at + 1)..] : "?";
        ImGui.TextUnformatted($"Step key: {key.StratClass}@{key.Time:0.##} (line {line}){(key.Method is { } m ? $" · {m}" : "")}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Copy key##step-key"))
            ImGui.SetClipboardText($"{{ \"key\": {{ \"cls\": \"{key.StratClass}\", \"time\": {key.Time.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}, \"line\": {line} }}, \"title\": \"{key.Method ?? ""}\", \"rule\": \"\" }}");
        var player = game.World.Party.Player?.Position;
        var distance = player is { } p ? Vector3.Distance(new Vector3(p.X, 0f, p.Z), new Vector3(current.Order.Target.X, 0f, current.Order.Target.Z)) : float.NaN;
        Muted($"target ({current.Order.Target.X:0.0}, {current.Order.Target.Z:0.0}) · {distance:0.0} yalms away · tolerance {current.Evaluation.Tolerance:0.0} · deadline {current.Evaluation.Deadline:0.0}s (now {game.World.Events.Elapsed:0.0}s){(current.Evaluation.Result is { } r ? $" · {r}" : "")}");
        if (current.Step is { } step)
        {
            ImGui.TextWrapped($"rule: {current.Rule ?? "—"}");
            ImGui.TextWrapped($"cue: {current.Cue ?? "—"}");
            if (step.Skip) Muted("(step is skipped by the lesson)");
        }
        else
        {
            using (AsuTheme.PushTextColor(AsuTheme.Accent)) ImGui.TextUnformatted("The lesson has no step for this order.");
        }
        ImGui.Spacing();
        ImGui.TextUnformatted($"Seen in the {CueWindowSeconds:0} s before the order (cue candidates):");
        var from = current.Order.IssuedAt - CueWindowSeconds;
        var candidates = seen.Where(e => e.Time >= from && e.Time <= current.Order.IssuedAt + 0.5f).OrderByDescending(e => e.Time).ToList();
        if (candidates.Count == 0) Muted("nothing new showed up");
        foreach (var (time, kind, name, target) in candidates)
        {
            var own = target == session.Role.ToString();
            using (own ? AsuTheme.PushTextColor(AsuTheme.Ok) : null)
                ImGui.TextUnformatted($"  {current.Order.IssuedAt - time,5:0.0}s before · {kind} {name}{(target is null ? "" : own ? " on you" : $" on {target}")}");
        }
    }

    private void DrawState()
    {
        if (state.Count == 0)
        {
            Muted("The run's rolls (*State) show here while a run is live.");
            return;
        }
        ImGui.TextUnformatted($"Rolls of this run ({stateOf?.GetType().Name}):");
        if (!ImGui.BeginTable("lesson-state", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        foreach (var (name, value) in state.OrderBy(kv => kv.Key))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(name);
            ImGui.TableNextColumn(); ImGui.TextWrapped(value);
        }
        ImGui.EndTable();
    }

    private static void Muted(string text)
    {
        using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextWrapped(text);
    }
}
