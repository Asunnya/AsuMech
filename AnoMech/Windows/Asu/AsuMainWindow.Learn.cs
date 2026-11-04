using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Learn;
using AnoMech.Scenarios;
using AnoMech.Windows.Learn;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float ArenaPreviewPx = 120f;
    private const float ArenaMarginYalms = 4f;
    private const float WaymarkDotPx = 4f;

    private WatchSession? watch;

    private void TickWatch(Game game)
    {
        if (watch == null) return;
        watch.Tick(RunState.Live(game), ImGui.GetIO().DeltaTime);
        if (watch.Ended) watch = null;
    }

    // Every way a run stops by hand also ends a watch or a lesson.
    private void EndWatch()
    {
        watch?.Dispose();
        watch = null;
        LearnSession.Current?.Dispose();
    }

    private LearnLevel learnLevel = LearnLevel.Guided;

    private static readonly (LearnLevel Level, string Label, string Tip)[] Levels =
    [
        (LearnLevel.Observe, "Observe", "The strat plays your seat at half speed while the lesson text follows along."),
        (LearnLevel.Guided, "Guided", "Your target is drawn on the ground; the fight slows down until you get there."),
        (LearnLevel.Assisted, "Assisted", "The hint appears later each time you get a step right, and not at all once you've nailed it three times."),
        (LearnLevel.Exam, "Exam", "Nothing on screen; the report comes at the end."),
    ];

    private void DrawLearn(Game game, IScenario scenario, bool inSession)
    {
        using (AsuTheme.PushTextColor(AsuTheme.WaymarkYellow))
            ImGui.TextWrapped("Learn is still being built: lessons, hints, reports and the mitigation trainer may be incomplete or wrong, and can change between versions.");

        DrawStratPill(scenario);
        if (!inSession)
        {
            Flow.SameLineIfFits(Flow.ButtonWidth(RolePillLabel()));
            DrawRolePill();
        }

        var lesson = Picker.SelectedAi is { } ai ? LessonLoader.For(scenario, ai) : null;
        foreach (var (level, label, tip) in Levels)
        {
            if (level != LearnLevel.Observe) ImGui.SameLine(0f, 0f);
            var available = level != LearnLevel.Assisted || lesson != null;
            bool clicked;
            using (learnLevel == level ? AsuTheme.PushAccentButton() : null)
            {
                ImGui.BeginDisabled(!available);
                clicked = ImGui.SmallButton($"{label}##level");
                ImGui.EndDisabled();
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(tip);
            if (clicked) learnLevel = level;
        }

        var why = LearnRefusal(scenario, lesson, inSession);
        ImGui.SameLine();
        ImGui.BeginDisabled(why != null);
        bool start;
        using (AsuTheme.PushAccentButton())
            start = ImGui.Button(LearnSession.Current is { Ended: false } ? "◉ Learning…##learn" : "◉ Start lesson##learn");
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(why ?? (learnLevel == LearnLevel.Exam ? "Counts toward your streak." : "Doesn't count toward your streak."));
        if (start) StartLesson(scenario, lesson);

        DrawArenaPreview(scenario);
        ImGui.SameLine();
        ImGui.BeginGroup();
        if (lesson is { } l)
        {
            var role = LearnRole();
            Muted($"{l.Steps.Count} steps · {Plugin.Mastery.MasteredCount(l, role)} mastered as {ScenarioSetupUi.RoleLabels[(int)role + 1]}{(l.Toolbox != null ? " · toolbox synced" : "")} · sources:");
            foreach (var source in l.Sources)
                if (ImGui.SmallButton($"{source.Label} ↗##{source.Url}")) Util.OpenLink(source.Url);
            if (LearnSession.LastReport is { } report)
            {
                Muted($"Last run: {report.Summary}");
                ImGui.SameLine();
                if (ImGui.SmallButton("Report##lesson-report")) plugin.LessonReport.Toggle();
            }
            DrawDrill(scenario, l, role);
        }
        else
        {
            Muted(Picker.SelectedAi is null ? "Pick a strat to learn." : "No lesson for this strat yet. Observe still works.");
            DrawRequestLink(FeedbackKind.Strat);
        }
        ImGui.EndGroup();
    }

    // Repeats the weakest step until it is mastered; Stop or "End drill" breaks the loop.
    private void DrawDrill(IScenario scenario, Lesson lesson, PartyRole role)
    {
        var runner = plugin.StepDrill;
        if (runner.Active is { } active)
        {
            using (AsuTheme.PushTextColor(AsuTheme.Accent))
                ImGui.TextUnformatted($"Drilling: {active.Step.Title ?? MasteryStore.StepId(active.Step.Key)} · run {runner.Runs} · {Plugin.Mastery.Streak(active.Lesson, active.Role, active.Step.Key)}/{MasteryStore.HitsToMaster}");
            ImGui.SameLine();
            if (ImGui.SmallButton("End drill")) runner.End();
            return;
        }
        var weakest = StepDrillPlanner.Weakest(lesson, role, Plugin.Mastery, LearnSession.LastReport);
        if (weakest is null) { Muted("Every step mastered."); return; }
        var level = learnLevel == LearnLevel.Observe ? LearnLevel.Guided : learnLevel;
        var window = StepDrillPlanner.Window(weakest);
        if (ImGui.SmallButton($"Drill weakest: {weakest.Title ?? MasteryStore.StepId(weakest.Key)}##drill"))
            runner.Start(new StepDrillRunner.Spec(scenario, Picker.SelectedAi!, lesson, weakest, role, level, window));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Runs only {window.From:0}s–{window.To:0}s of the fight, {level}, on repeat. The bots skip the orders before the slice, so adds or spreads set up earlier may be missing.");
    }

    private string? LearnRefusal(IScenario scenario, Lesson? lesson, bool inSession)
    {
        if (inSession) return "Learn is solo only: a session runs at 1× with every seat played.";
        if (LearnSession.Current is { Ended: false }) return "A lesson is already running.";
        if (Picker.SelectedAi == null) return "Learn needs a strat.";
        if (lesson == null && learnLevel != LearnLevel.Observe) return "This strat has no lesson yet; Observe is the only level that works without one.";
        return plugin.Runs.StartRefusal(Picker.Selection, SoloSelected);
    }

    private void StartLesson(IScenario scenario, Lesson? lesson)
    {
        EndWatch();
        Plugin.Practice.AutoRestart = false;
        var role = LearnRole();
        new LearnSession(Plugin.Game, scenario, Picker.SelectedAi!, lesson, role, learnLevel);
        plugin.Runs.Start(Picker.Selection, SoloSelected);
    }

    // The arena and the selected waymark layout, north up.
    private void DrawArenaPreview(IScenario scenario)
    {
        var scale = AsuTheme.Scale;
        var size = ArenaPreviewPx * scale;
        var pos = ImGui.GetCursorScreenPos();
        var center = pos + new Vector2(size * 0.5f);
        var draw = ImGui.GetWindowDrawList();
        var presets = scenario.Phase.Zone.WaymarkPresets;
        var markers = presets.Count > 0 ? presets[Math.Clamp(Picker.Waymark, 0, presets.Count - 1)].Markers : [];
        var extent = markers.Count > 0 ? markers.Max(m => new Vector2(m.Offset.X, m.Offset.Z).Length()) + ArenaMarginYalms : 20f;
        var yalm = size * 0.5f / extent;

        draw.AddCircleFilled(center, size * 0.5f, AsuTheme.U32(AsuTheme.Element));
        draw.AddCircle(center, size * 0.5f, AsuTheme.U32(AsuTheme.Line));
        draw.AddText(new Vector2(center.X - ImGui.CalcTextSize("N").X * 0.5f, pos.Y), AsuTheme.U32(AsuTheme.Muted), "N");
        foreach (var marker in markers)
        {
            var at = center + new Vector2(marker.Offset.X, marker.Offset.Z) * yalm;
            var color = AsuTheme.U32(WaymarkColor(marker.Slot));
            var label = WaymarkLabel(marker.Slot);
            if (marker.Slot <= WaymarkSlot.D) draw.AddCircleFilled(at, WaymarkDotPx * scale, color);
            else draw.AddRectFilled(at - new Vector2(WaymarkDotPx * scale), at + new Vector2(WaymarkDotPx * scale), color);
            draw.AddText(at + new Vector2(WaymarkDotPx * scale + 1f, -ImGui.GetTextLineHeight() * 0.5f), AsuTheme.U32(AsuTheme.Text), label);
        }
        ImGui.Dummy(new Vector2(size));
        if (ImGui.IsItemHovered() && presets.Count > 0)
            ImGui.SetTooltip($"Waymarks: {presets[Math.Clamp(Picker.Waymark, 0, presets.Count - 1)].Name}");
    }

    private static Vector4 WaymarkColor(WaymarkSlot slot) => slot switch
    {
        WaymarkSlot.A or WaymarkSlot.One => AsuTheme.WaymarkRed,
        WaymarkSlot.B or WaymarkSlot.Two => AsuTheme.WaymarkYellow,
        WaymarkSlot.C or WaymarkSlot.Three => AsuTheme.WaymarkBlue,
        _ => AsuTheme.WaymarkPurple,
    };

    private static string WaymarkLabel(WaymarkSlot slot) => slot switch
    {
        WaymarkSlot.A => "A", WaymarkSlot.B => "B", WaymarkSlot.C => "C", WaymarkSlot.D => "D",
        WaymarkSlot.One => "1", WaymarkSlot.Two => "2", WaymarkSlot.Three => "3", _ => "4",
    };

    // The seat the run will give the player: the override, else the one their current job takes.
    private PartyRole LearnRole()
    {
        if (Picker.RoleOverride is { } forced) return forced;
        var player = Natives.BattleCharas.LocalPlayer;
        return player.Exists ? PartyPresets.SkipRoleForJob(player.ClassJob) : Plugin.Game.World.Party.PlayerRole;
    }
}
