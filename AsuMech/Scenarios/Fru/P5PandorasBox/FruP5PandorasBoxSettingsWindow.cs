using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Fru.P5PandorasBox;

public sealed class FruP5PandorasBoxSettingsWindow
{
    public FruP5PandorasBoxStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.LimitBreakTank = null;
        if (SettingsGrid.Begin("##frup5pandorasbox"))
        {
            SettingsGrid.FightOnlyNote();
            DrawLimitBreakTank();
            SettingsGrid.End();
        }
    }

    private void DrawLimitBreakTank()
    {
        var v = Overrides.LimitBreakTank;
        SettingsGrid.Row("Tank LB3:");
        if (ImGui.RadioButton("Auto##pblb", v == null)) Overrides.LimitBreakTank = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("MT##pblb", v == PartyRole.MainTank)) Overrides.LimitBreakTank = PartyRole.MainTank;
        ImGui.SameLine();
        if (ImGui.RadioButton("OT##pblb", v == PartyRole.OffTank)) Overrides.LimitBreakTank = PartyRole.OffTank;
    }
}
