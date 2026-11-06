using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed class DsrP7DragonKingSettingsWindow
{
    public DsrP7DragonKingStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.FirstInvulnTank = PartyRole.MainTank;

        if (SettingsGrid.Begin("##dsrp7dragonking"))
        {
            DrawAkhMornRow("Akh Morn 2 (6/1/1):", Overrides.FirstInvulnTank, "##akh2");
            DrawAkhMornRow("Akh Morn 3 (6/1/1):", DsrP7DragonKingState.OtherTank(Overrides.FirstInvulnTank), "##akh3");
            SettingsGrid.End();
        }
    }

    // No tank invuln recovers in the 80s between the two, so picking one row sets the other.
    private void DrawAkhMornRow(string label, PartyRole invulnTank, string id)
    {
        var second = id == "##akh3";
        SettingsGrid.Row(label);
        foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank })
        {
            if (tank == PartyRole.OffTank) ImGui.SameLine();
            var kitchenSink = SettingsGrid.RoleLabel(DsrP7DragonKingState.OtherTank(tank));
            if (ImGui.RadioButton($"{SettingsGrid.RoleLabel(tank)} invulns south, {kitchenSink} kitchen sinks NE{id}{tank}", invulnTank == tank))
                Overrides.FirstInvulnTank = second ? DsrP7DragonKingState.OtherTank(tank) : tank;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Bot tanks follow this plan. No invuln comes back in time for the next one, so the tanks trade.");
    }
}
