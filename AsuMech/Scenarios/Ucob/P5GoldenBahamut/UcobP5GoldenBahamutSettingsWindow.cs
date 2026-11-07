using System;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P5GoldenBahamut;

public sealed class UcobP5GoldenBahamutSettingsWindow
{
    public UcobP5GoldenBahamutStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.FirstInvulnTank = PartyRole.MainTank;
            Array.Clear(Overrides.ExaflareDirections);
        }

        if (SettingsGrid.Begin("##ucobp5golden"))
        {
            SettingsGrid.Row("Akh Morn invulns:");
            foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank })
            {
                if (tank == PartyRole.OffTank) ImGui.SameLine();
                var other = UcobP5GoldenBahamutState.OtherTank(tank);
                if (ImGui.RadioButton($"{SettingsGrid.RoleLabel(tank)} 2nd, {SettingsGrid.RoleLabel(other)} 3rd##akhmorn{tank}", Overrides.FirstInvulnTank == tank))
                    Overrides.FirstInvulnTank = tank;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Akh Morn 1 and 4 are shared by both tanks; 2 and 3 are taken alone under an invuln.");

            for (var set = 0; set < UcobP5GoldenBahamutState.ExaflareSets; set++)
            {
                SettingsGrid.Row($"Exaflare {set + 1} rolls toward:");
                if (ImGui.RadioButton($"Auto##exadir{set}", Overrides.ExaflareDirections[set] == null)) Overrides.ExaflareDirections[set] = null;
                foreach (var d in Direction.All)
                {
                    ImGui.SameLine();
                    if (ImGui.RadioButton($"{d.Name()}##exadir{set}{d.Name()}", Overrides.ExaflareDirections[set] == d)) Overrides.ExaflareDirections[set] = d;
                }
            }
            SettingsGrid.End();
        }
    }
}
