using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Uwu.P1Garuda;

// The rolls worth forcing by hand: where the sisters sing from and who the first song picks.
// Plume spots and target lists are pinned only by a lesson's toolbox example.
public class UwuP1GarudaSettingsWindow
{
    public UwuP1GarudaStateOverrides Overrides { get; } = new();

    private static readonly (string Label, float Bearing)[] Cardinals = [("N", 0f), ("E", 90f), ("S", 180f), ("W", 270f)];

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.Clear();
        if (SettingsGrid.Begin("##uwup1garuda"))
        {
            DrawBearing("Suparna sings from:", "suparna", Overrides.SuparnaSongBearing, b => Overrides.SuparnaSongBearing = b);
            DrawBearing("Chirada sings from:", "chirada", Overrides.ChiradaSongBearing, b => Overrides.ChiradaSongBearing = b);
            DrawSongTarget();
            SettingsGrid.End();
        }
        if (Overrides.SatinPlumesFirst != null || Overrides.SatinPlumesSecond != null || Overrides.SistersSongTargets != null || Overrides.FrictionTargets != null || Overrides.MesohighTargets != null)
            ImGui.TextDisabled("A lesson's toolbox example also pins plumes and targets until the lesson ends.");
    }

    private static void DrawBearing(string label, string id, float? current, System.Action<float?> set)
    {
        SettingsGrid.Row(label);
        if (ImGui.RadioButton($"Random##{id}", current == null)) set(null);
        foreach (var (name, bearing) in Cardinals)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{name}##{id}", current == bearing)) set(bearing);
        }
    }

    private void DrawSongTarget()
    {
        var v = Overrides.MistralSongTarget;
        SettingsGrid.Row("First Mistral Song on:");
        if (ImGui.RadioButton("Random##song", v == null)) Overrides.MistralSongTarget = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("H1##song", v == PartyRole.RegenHealer)) Overrides.MistralSongTarget = PartyRole.RegenHealer;
        ImGui.SameLine();
        if (ImGui.RadioButton("H2##song", v == PartyRole.ShieldHealer)) Overrides.MistralSongTarget = PartyRole.ShieldHealer;
    }
}
