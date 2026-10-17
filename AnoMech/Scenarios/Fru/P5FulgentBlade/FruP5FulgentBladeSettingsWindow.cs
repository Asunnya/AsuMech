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
            DrawMirrored();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.PositionIndex = null;
        Overrides.RotationIndex = null;
        Overrides.Mirrored = null;
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

    private void DrawMirrored()
    {
        var v = Overrides.Mirrored;
        SettingsGrid.Row("Mirrored:");
        if (ImGui.RadioButton("Auto##fbmirror", v == null)) Overrides.Mirrored = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("No##fbmirror", v == false)) Overrides.Mirrored = false;
        ImGui.SameLine();
        if (ImGui.RadioButton("Yes##fbmirror", v == true)) Overrides.Mirrored = true;
    }
}
