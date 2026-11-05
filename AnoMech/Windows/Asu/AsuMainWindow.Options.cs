using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const string SessionDisabledReason = "Disabled in a multiplayer session.";

    private void DrawOptions(Game game, bool inSession)
    {
        if (Selected is not { } scenario) return;
        ImGui.Separator();
        if (mode == Mode.Learn)
            DrawLearn(game, scenario, inSession);
        else
            DrawPracticeOptions(game, scenario, inSession);
        DrawFooter(game, inSession);
        DrawScenarioSettings(scenario, inSession);
    }

    private void DrawPracticeOptions(Game game, IScenario scenario, bool inSession)
    {
        DrawStratPill(scenario);
        if (!inSession)
        {
            Flow.SameLineIfFits(Flow.ButtonWidth(RolePillLabel()));
            DrawRolePill();
        }
        if (!inSession && scenario.SupportsSolo)
        {
            Flow.SameLineIfFits(Pills.Width("Solo"));
            var running = RunState.Live(game);
            if (Pills.Toggle("Solo##solo-pill", Picker.Solo, disabled: running)) Picker.Solo = !Picker.Solo;
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(running
                    ? "Stop the current scenario before changing the run mode."
                    : "Run without simulated party members or party AI.");
        }
    }

    // Opens a list of the region's strats; grouped scenarios pick the region in Scenario settings.
    private void DrawStratPill(IScenario scenario)
    {
        var strats = scenario.AiStrats;
        var name = Picker.SelectedAi?.Name ?? (strats.Count == 0 ? "No strat" : "No strat for this region");
        var choice = ScenarioPicker.HasStratChoice(scenario);
        var guest = RunState.IsGuest;
        bool clicked;
        using (AsuTheme.PushPill())
        {
            ImGui.BeginDisabled(!choice || guest);
            clicked = ImGui.Button($"Strat: {name}{(choice ? " ▾" : "")}##strat-pill");
            ImGui.EndDisabled();
        }
        if (guest && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Only the host's selection is used in multiplayer.");
        if (clicked) ImGui.OpenPopup("strat-list");
        if (!ImGui.BeginPopup("strat-list")) return;
        var groups = ScenarioPicker.StratGroups(scenario);
        if (groups.Count == 0)
        {
            for (var i = 0; i < strats.Count; i++)
                if (ImGui.Selectable($"{strats[i].Name}##strat{i}", Picker.Strat == i)) Picker.SelectStrat(i);
        }
        else
        {
            foreach (var group in groups)
            {
                Muted(group);
                for (var i = 0; i < strats.Count; i++)
                    if (strats[i].Group == group && ImGui.Selectable($"  {strats[i].Name}##strat{i}", Picker.Strat == i))
                        Picker.SelectStrat(i);
            }
        }
        ImGui.EndPopup();
    }

    private int RoleIndex => Picker.RoleOverride is { } role ? (int)role + 1 : 0;

    private string RolePillLabel() => $"Role: {ScenarioSetupUi.RoleLabels[RoleIndex]}##role-pill";

    private void DrawRolePill()
    {
        var labels = ScenarioSetupUi.RoleLabels;
        var idx = RoleIndex;
        bool clicked;
        using (AsuTheme.PushPill())
            clicked = ImGui.Button(RolePillLabel());
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click for the next role, right-click for the previous one.");
        var next = clicked ? idx + 1 : ImGui.IsItemClicked(ImGuiMouseButton.Right) ? idx - 1 + labels.Length : -1;
        if (next < 0) return;
        next %= labels.Length;
        Picker.RoleOverride = next == 0 ? null : (PartyRole)(next - 1);
    }

    private void DrawFooter(Game game, bool inSession)
    {
        DrawRunToggles(game, inSession);
        Flow.SameLineIfFits(StreakWidth());
        DrawStreak(inSession);
        DrawMitToggle(inSession);

        if (Plugin.Config.ShowSpeedControl)
        {
            ImGui.BeginDisabled(inSession);
            ImGui.BeginGroup();
            speedControl.Draw(game, "asu-speed");
            ImGui.EndGroup();
            ImGui.EndDisabled();
            if (inSession && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(SessionDisabledReason);
        }
        Muted($"{(inSession ? "Session" : SoloSelected ? "Solo" : "Party")} · Speed {game.EventTimeScale:0.##}× · Seed {(Picker.Seed is { } seed ? seed.ToString() : "random")}");
    }

    private static readonly string[] AssistLabels = ["None", "Reminder", "Strict"];

    private void DrawMitToggle(bool inSession)
    {
        if (Selected is not { } scenario) return;
        var why = Windows.Learn.MitTraining.Refusal(scenario.GetType().Name, inSession);
        var on = Plugin.Config.EnableMitigationTraining && why == null;
        if (Pills.Toggle("Mit plan##mit-pill", on, disabled: why != null))
        {
            Plugin.Config.EnableMitigationTraining = !Plugin.Config.EnableMitigationTraining;
            Plugin.Config.Save();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(why ?? "Checks the plan's mitigation for your job or seat against what you press. Bots press their columns of the plan, so the damage checks see the party mitigated.");
        if (why != null || !Plugin.Config.EnableMitigationTraining) return;
        ImGui.SameLine();
        var plan = Windows.Learn.MitTraining.PlanFor(scenario.GetType().Name);
        var assist = Plugin.Config.MitPracticeAssist;
        ImGui.SetNextItemWidth(110f * Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##mit-assist", ref assist, AssistLabels, AssistLabels.Length))
        {
            Plugin.Config.MitPracticeAssist = assist;
            Plugin.Config.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("None: report only. Reminder: a strip with the next items and a note on each miss. Strict: a miss fails the run.");
        if (assist == (int)Windows.Learn.MitAssist.Reminder)
        {
            ImGui.SameLine();
            var ends = Plugin.Config.MitFailureEndsStreak;
            if (ImGui.Checkbox("ends streak##mit-streak", ref ends))
            {
                Plugin.Config.MitFailureEndsStreak = ends;
                Plugin.Config.Save();
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("A missed mitigation lets the run go on but resets the clean-run streak.");
        }
        ImGui.SameLine();
        var bots = Plugin.Config.BotsFollowMitPlan;
        if (ImGui.Checkbox("bots##mit-bots", ref bots))
        {
            Plugin.Config.BotsFollowMitPlan = bots;
            Plugin.Config.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Bots apply the plan's mitigation for their own columns (needs tank mitigation checks on).");
        if (plan != null)
        {
            ImGui.SameLine();
            Muted($"{plan.Source.Title} · {string.Join(", ", plan.Source.Credits)}");
        }
        if (Windows.Learn.MitTraining.Instance?.LastReport != null)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("Report##mit-report")) plugin.MitReport.Toggle();
        }
    }

    // God mode and Auto-restart are forced off in a session: a host rerunning alone would desync it.
    private static void DrawRunToggles(Game game, bool inSession)
    {
        if (inSession) game.GodMode = false;
        if (Pills.Toggle("God mode##god-pill", game.GodMode, disabled: inSession)) game.GodMode = !game.GodMode;
        if (inSession && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(SessionDisabledReason);
        Flow.SameLineIfFits(Pills.Width("Auto-restart"));
        var practice = Plugin.Practice;
        if (inSession) practice.AutoRestart = false;
        if (Pills.Toggle("Auto-restart##auto-restart-pill", practice.AutoRestart, disabled: inSession)) practice.AutoRestart = !practice.AutoRestart;
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(inSession
                ? SessionDisabledReason
                : "Restart the same scenario immediately after a successful run. A failed run turns this back off.");
    }

    private void DrawScenarioSettings(IScenario scenario, bool inSession)
    {
        if (ImGui.CollapsingHeader("Setup###asu-setup"))
            ScenarioSetupUi.DrawSetupGrid(Picker, inSession, RunState.IsGuest);
        if (ImGui.CollapsingHeader("Scenario settings###asu-scenario-settings"))
        {
            if (inSession)
                multiplayerUi.DrawScenarioSettings(scenario);
            else
            {
                scenario.DrawSettings();
                scenario.DrawMultiplayerSettings();
            }
        }
#if DEBUG
        if (ImGui.CollapsingHeader("Debug###asu-debug"))
            DebugSection.Draw(Picker, plugin.DebugMenu, inSession);
#endif
    }
}
