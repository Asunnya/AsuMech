using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

public sealed class FruP5FulgentBladeSettingsWindow
{
    private static readonly string[] Pockets = ["S", "N", "E", "W"];

    public FruP5FulgentBladeStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup5fulgentblade"))
        {
            SettingsGrid.FightOnlyNote();
            DrawPocket();
            DrawFirstSide();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.PositionIndex = null;
        Overrides.RotationIndex = null;
        Overrides.EastFirst = null;
    }

    private void DrawPocket()
    {
        SettingsGrid.Row("Starting pocket:");
        if (ImGui.RadioButton("Auto##fbpocket", Overrides.PositionIndex == null)) Overrides.PositionIndex = null;
        for (var i = 0; i < Pockets.Length; i++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{Pockets[i]}##fbpocket", Overrides.PositionIndex == i)) Overrides.PositionIndex = i;
        }
    }

    private void DrawFirstSide()
    {
        var v = Overrides.EastFirst;
        SettingsGrid.Row("First train:");
        if (ImGui.RadioButton("Auto##fbfirst", v == null)) Overrides.EastFirst = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("East##fbfirst", v == true)) Overrides.EastFirst = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("West##fbfirst", v == false)) Overrides.EastFirst = false;
    }
}
