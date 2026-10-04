using System.Linq;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Uwu.PrimalRoulette;

public class PrimalRouletteSettingsWindow
{
    public PrimalRouletteStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.Order = null;
            Overrides.ViscousOnPlayer = false;
        }

        if (SettingsGrid.Begin("##primalroulette"))
        {
            DrawOrder();
            DrawViscous();
            SettingsGrid.End();
        }
    }

    private void DrawOrder()
    {
        SettingsGrid.Row("Roulette order:");
        if (ImGui.RadioButton("Random##rouletteorder", Overrides.Order == null)) Overrides.Order = null;
        for (var i = 0; i < PrimalRouletteState.Orders.Length; i++)
        {
            ImGui.SameLine();
            var label = string.Join(" > ", PrimalRouletteState.Orders[i].Select(p => p.ToString()));
            if (ImGui.RadioButton($"{label}##rouletteorder{i}", Overrides.Order == i)) Overrides.Order = i;
        }
    }

    private void DrawViscous()
    {
        var v = Overrides.ViscousOnPlayer;
        SettingsGrid.Row("Viscous Aetheroplasm:");
        if (ImGui.RadioButton("Random##viscous", !v)) Overrides.ViscousOnPlayer = false;
        ImGui.SameLine();
        if (ImGui.RadioButton("Me##viscous", v)) Overrides.ViscousOnPlayer = true;
    }
}
