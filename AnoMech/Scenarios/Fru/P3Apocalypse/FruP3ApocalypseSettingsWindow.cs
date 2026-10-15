using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P3Apocalypse;

public sealed class FruP3ApocalypseSettingsWindow
{
    private static readonly string[] Spokes = ["N/S", "NE/SW", "E/W", "SE/NW"];

    public FruP3ApocalypseStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup3apocalypse"))
        {
            SettingsGrid.FightOnlyNote();
            DrawStart();
            DrawRotation();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.Rotation = null;
        Overrides.Clockwise = null;
    }

    private void DrawStart()
    {
        SettingsGrid.Row("First lights:");
        if (ImGui.RadioButton("Auto##apocstart", Overrides.Rotation == null)) Overrides.Rotation = null;
        for (var i = 0; i < Spokes.Length; i++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{Spokes[i]}##apocstart", Overrides.Rotation == i)) Overrides.Rotation = i;
        }
    }

    private void DrawRotation()
    {
        var v = Overrides.Clockwise;
        SettingsGrid.Row("Rotation:");
        if (ImGui.RadioButton("Auto##apocrot", v == null)) Overrides.Clockwise = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("CW##apocrot", v == true)) Overrides.Clockwise = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("CCW##apocrot", v == false)) Overrides.Clockwise = false;
    }
}
