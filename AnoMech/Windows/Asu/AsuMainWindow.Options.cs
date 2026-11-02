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
            ImGui.SameLine();
            DrawRolePill();
        }
        if (!inSession && scenario.SupportsSolo)
        {
            ImGui.SameLine();
            var running = RunState.Live(game);
            ImGui.BeginDisabled(running);
            var solo = Picker.Solo;
            if (ImGui.Checkbox("Solo", ref solo)) Picker.Solo = solo;
            ImGui.EndDisabled();
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

    private void DrawRolePill()
    {
        var labels = ScenarioSetupUi.RoleLabels;
        var idx = Picker.RoleOverride is { } role ? (int)role + 1 : 0;
        bool clicked;
        using (AsuTheme.PushPill())
            clicked = ImGui.Button($"Role: {labels[idx]}##role-pill");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click for the next role, right-click for the previous one.");
        var next = clicked ? idx + 1 : ImGui.IsItemClicked(ImGuiMouseButton.Right) ? idx - 1 + labels.Length : -1;
        if (next < 0) return;
        next %= labels.Length;
        Picker.RoleOverride = next == 0 ? null : (PartyRole)(next - 1);
    }

    private void DrawFooter(Game game, bool inSession)
    {
        if (inSession) game.GodMode = false;
        ImGui.BeginDisabled(inSession);
        var god = game.GodMode;
        if (ImGui.Checkbox("God mode", ref god)) game.GodMode = god;
        ImGui.EndDisabled();
        if (inSession && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(SessionDisabledReason);
        ImGui.SameLine();
        DrawStreak(inSession);

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

    private void DrawScenarioSettings(IScenario scenario, bool inSession)
    {
        if (!ImGui.CollapsingHeader("Scenario settings###asu-scenario-settings")) return;
        ScenarioSetupUi.DrawSetupGrid(Picker, inSession, RunState.IsGuest);
        if (inSession)
            multiplayerUi.DrawScenarioSettings(scenario);
        else
        {
            scenario.DrawSettings();
            scenario.DrawMultiplayerSettings();
        }
    }
}
