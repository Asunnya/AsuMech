using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Implementations;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const string AllExpansions = "All expansions";
    private const float ExpansionComboPx = 140f;
    private const float ActionButtonPx = 96f;

    private static string? ExpansionScope => Plugin.Config.SelectedExpansion.Length == 0 ? null : Plugin.Config.SelectedExpansion;
    private static string? TypeScope => Plugin.Config.SelectedType.Length == 0 ? null : Plugin.Config.SelectedType;

    private static void SetScope(string? expansion, string? type)
    {
        Plugin.Config.SelectedExpansion = expansion ?? "";
        Plugin.Config.SelectedType = type ?? "";
        Plugin.Config.Save();
    }

    // A pick outside the current scope widens the scope rather than hiding the pick.
    private static void RevealInScope(IScenario scenario)
    {
        var zone = scenario.Phase.Zone;
        var expansion = ExpansionScope;
        var type = TypeScope;
        if (expansion != null && zone.Expansion != expansion) expansion = zone.Expansion;
        if (type != null && zone.Category != type) type = null;
        if (expansion != ExpansionScope || type != TypeScope) SetScope(expansion, type);
    }

    private void DrawLauncher(Game game, bool inSession)
    {
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetNextItemWidth(ExpansionComboPx * scale);
        if (ImGui.BeginCombo("##expansion", ExpansionScope ?? AllExpansions))
        {
            if (ImGui.Selectable(AllExpansions, ExpansionScope == null)) SetScope(null, TypeScope);
            foreach (var expansion in CatalogView.Expansions(Catalog.Zones))
                if (ImGui.Selectable(expansion, ExpansionScope == expansion)) SetScope(expansion, TypeScope);
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        var actions = ActionsWidth(game, inSession);
        DrawSelector(ImGui.GetContentRegionAvail().X - actions - ImGui.GetStyle().ItemSpacing.X);
        ImGui.SameLine();
        DrawActions(game, inSession);
    }

    private float ActionsWidth(Game game, bool inSession)
    {
        var width = ActionButtonPx * ImGuiHelpers.GlobalScale;
        return HasSecondaryAction(game, inSession) ? width * 2 + ImGui.GetStyle().ItemSpacing.X : width;
    }

    private static bool HasSecondaryAction(Game game, bool inSession)
        => (!inSession && RunState.Live(game)) || game.World.Map.IsInInstance;

    private string SelectorLabel()
        => Selected is { } s ? $"{s.Phase.Zone.ShortName} › {s.DisplayName()}" : "Select a scenario";

    private void DrawActions(Game game, bool inSession)
    {
        var size = new Vector2(ActionButtonPx * ImGuiHelpers.GlobalScale, 0f);
        if (!inSession)
            DrawStartButton(game, size);
        else if (RunState.Active)
        {
            if (ImGui.Button("Stop##stop-session", size)) { EndWatch(); plugin.Runs.Reset(); }
        }
        else
            multiplayerUi.DrawStartButton(size, Picker.Selection);

        if (!inSession && RunState.Live(game))
        {
            ImGui.SameLine();
            using (AsuTheme.PushTextColor(AsuTheme.Bad))
                if (ImGui.Button("Stop##stop", size)) { EndWatch(); plugin.Runs.Reset(); }
        }
        else if (game.World.Map.IsInInstance)
        {
            ImGui.SameLine();
            if (ImGui.Button(inSession ? "Return to inn##leave" : "Leave##leave", size)) { EndWatch(); plugin.Runs.Leave(); }
            if (inSession && ImGui.IsItemHovered())
                ImGui.SetTooltip("Takes the whole party back to the inn. Everyone stays in the session.");
        }
    }

    private bool SoloSelected => Selected is { SupportsSolo: true } && Picker.Solo;

    // A start replaces whatever run is standing, so it needs no Stop first.
    private void DrawStartButton(Game game, Vector2 size)
    {
        var solo = SoloSelected;
        var selection = Picker.Selection;
        var refusal = plugin.Runs.StartRefusal(selection, solo);
        var restart = game.ActiveScenario is { } active && active == Selected;
        var label = plugin.Runs.StartWaitingOn != null ? "Waiting..." : restart ? "↻ Restart" : mode == Mode.Learn ? "▶ Try it" : "▶ Start";
        ImGui.BeginDisabled(refusal != null);
        bool clicked;
        using (AsuTheme.PushAccentButton())
            clicked = ImGui.Button($"{label}###start", size);
        ImGui.EndDisabled();
        if (refusal != null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(refusal);
        if (!clicked) return;
        mode = Mode.Practice;
        EndWatch();
        plugin.Runs.Start(selection, solo);
    }

    // Switching scenarios inside a loaded zone skips the zone reload, so another zone's scenario
    // would run on the wrong territory.
    private bool Pickable(IScenario scenario, out string? why)
    {
        why = null;
        if (RunState.IsGuest) why = "The host picks the scenario.";
        else if (RunState.InSession && !scenario.SupportsMultiplayer) why = "This scenario doesn't support multiplayer.";
        else if (Plugin.Game.World.Map.IsInInstance && Picker.Scenario is { } current && current.Phase.Zone != scenario.Phase.Zone)
            why = $"Leave {current.Phase.Zone.Name} first.";
        return why == null;
    }

    private bool Pickable(IZone zone, out string? why)
    {
        why = null;
        if (RunState.IsGuest) why = "The host picks the scenario.";
        else if (Plugin.Game.World.Map.IsInInstance && Picker.Scenario is { } current && current.Phase.Zone != zone)
            why = $"Leave {current.Phase.Zone.Name} first.";
        return why == null;
    }

    private static System.Collections.Generic.IEnumerable<IScenario> ScenariosOf(IZone zone)
        => Catalog.PhasesOf(zone).SelectMany(Catalog.ScenariosOf);
}
