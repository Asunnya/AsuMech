using System;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Fru.P1UtopianSky;

public sealed class FruP1UtopianSkySettingsWindow
{
    public FruP1UtopianSkyStateOverrides Overrides { get; } = new();

    private static readonly string[] AxisLabels = ["Random", "N/S", "NE/SW", "E/W", "NW/SE"];

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup1utopiansky"))
        {
            SettingsGrid.FightOnlyNote();
            ElementRow("Cyclonic Break:", "##uscyclonic", Overrides.CyclonicFire, v => Overrides.CyclonicFire = v);
            ElementRow("Utopian Sky:", "##usutopian", Overrides.UtopianFire, v => Overrides.UtopianFire = v);
            DrawSafeAxis();
            DrawFireTargets();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.CyclonicFire = null;
        Overrides.UtopianFire = null;
        Overrides.SafeAxis = null;
        Overrides.FireOnTanks = null;
    }

    internal static void ElementRow(string label, string id, bool? value, Action<bool?> set)
    {
        SettingsGrid.Row(label);
        if (ImGui.RadioButton($"Auto{id}", value == null)) set(null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"Fire{id}", value == true)) set(true);
        ImGui.SameLine();
        if (ImGui.RadioButton($"Lightning{id}", value == false)) set(false);
    }

    private void DrawSafeAxis()
    {
        SettingsGrid.Row("Safe line:");
        var selected = (Overrides.SafeAxis ?? -1) + 1;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##ussafe", ref selected, AxisLabels, AxisLabels.Length))
            Overrides.SafeAxis = selected == 0 ? null : selected - 1;
    }

    private void DrawFireTargets()
    {
        var v = Overrides.FireOnTanks;
        SettingsGrid.Row("Sinbound Fire III on:");
        if (ImGui.RadioButton("Auto##usfire", v == null)) Overrides.FireOnTanks = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Tanks##usfire", v == true)) Overrides.FireOnTanks = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Healers##usfire", v == false)) Overrides.FireOnTanks = false;
    }
}
