using System;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P2DiamondDust;

public sealed class FruP2DiamondDustSettingsWindow
{
    public FruP2DiamondDustStateOverrides Overrides { get; } = new();

    private static readonly string[] OctantLabels = ["Random", "N", "NE", "E", "SE", "S", "SW", "W", "NW"];
    private static readonly string[] IcicleLabels = ["Random", "N/S", "NE/SW", "E/W", "SE/NW"];

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup2diamonddust"))
        {
            SettingsGrid.FightOnlyNote();
            DrawKick();
            DrawTwin();
            DrawMarked();
            OctantRow("First icicles:", "##ddicicle", IcicleLabels, Overrides.FirstIcicle, v => Overrides.FirstIcicle = v);
            OctantRow("Mirror:", "##ddmirror", OctantLabels, Overrides.MirrorOctant, v => Overrides.MirrorOctant = v);
            OctantRow("Gaze:", "##ddgaze", OctantLabels, Overrides.GazeOctant, v => Overrides.GazeOctant = v);
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.Axe = null;
        Overrides.Stillness = null;
        Overrides.FirstIcicle = null;
        Overrides.SupportsMarked = null;
        Overrides.MirrorOctant = null;
        Overrides.GazeOctant = null;
    }

    private void DrawMarked()
    {
        var v = Overrides.SupportsMarked;
        SettingsGrid.Row("Frigid Stone:");
        if (ImGui.RadioButton("Auto##ddmarked", v == null)) Overrides.SupportsMarked = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Supports##ddmarked", v == true)) Overrides.SupportsMarked = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("DPS##ddmarked", v == false)) Overrides.SupportsMarked = false;
    }

    private static void OctantRow(string label, string id, string[] labels, int? value, Action<int?> set)
    {
        SettingsGrid.Row(label);
        var selected = (value ?? -1) + 1;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo(id, ref selected, labels, labels.Length))
            set(selected == 0 ? null : selected - 1);
    }

    private void DrawKick()
    {
        var v = Overrides.Axe;
        SettingsGrid.Row("Opening kick:");
        if (ImGui.RadioButton("Auto##ddkick", v == null)) Overrides.Axe = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Axe (out)##ddkick", v == true)) Overrides.Axe = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Scythe (in)##ddkick", v == false)) Overrides.Axe = false;
    }

    private void DrawTwin()
    {
        var v = Overrides.Stillness;
        SettingsGrid.Row("Twin:");
        if (ImGui.RadioButton("Auto##ddtwin", v == null)) Overrides.Stillness = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Stillness##ddtwin", v == true)) Overrides.Stillness = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Silence##ddtwin", v == false)) Overrides.Stillness = false;
    }
}
