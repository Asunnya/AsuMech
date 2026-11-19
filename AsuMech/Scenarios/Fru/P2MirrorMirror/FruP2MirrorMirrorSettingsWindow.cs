using System;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Fru.P2MirrorMirror;

public sealed class FruP2MirrorMirrorSettingsWindow
{
    public FruP2MirrorMirrorStateOverrides Overrides { get; } = new();

    private static readonly string[] OctantLabels = ["Random", "N", "NE", "E", "SE", "S", "SW", "W", "NW"];
    private static readonly string[] RedLabels = ["Random", "N + E", "NE + SE", "E + S", "SE + SW", "S + W", "SW + NW", "W + N", "NW + NE"];
    private static readonly string[] RoleLabels = ["Random", "MT", "OT", "H1", "H2", "M1", "M2", "R1", "R2"];

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup2mirrormirror"))
        {
            SettingsGrid.FightOnlyNote();
            ComboRow("Hallowed Ray on:", "##mmray", RoleLabels, Overrides.RayTarget is { } r ? (int)r : null,
                v => Overrides.RayTarget = v is { } i ? (PartyRole)i : null);
            ComboRow("Blue mirror:", "##mmblue", OctantLabels, Overrides.BlueOctant, v => Overrides.BlueOctant = v);
            ComboRow("Red mirrors:", "##mmred", RedLabels, Overrides.FirstRedOctant, v => Overrides.FirstRedOctant = v);
            DrawBanish();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.RayTarget = null;
        Overrides.BlueOctant = null;
        Overrides.FirstRedOctant = null;
        Overrides.Pairs = null;
        Overrides.SupportPairs = null;
    }

    private static void ComboRow(string label, string id, string[] labels, int? value, Action<int?> set)
    {
        SettingsGrid.Row(label);
        var selected = (value ?? -1) + 1;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo(id, ref selected, labels, labels.Length))
            set(selected == 0 ? null : selected - 1);
    }

    private void DrawBanish()
    {
        var v = Overrides.Pairs;
        SettingsGrid.Row("Banish III:");
        if (ImGui.RadioButton("Auto##mmbanish", v == null)) Overrides.Pairs = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Pairs##mmbanish", v == true)) Overrides.Pairs = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Spread##mmbanish", v == false)) Overrides.Pairs = false;
    }
}
