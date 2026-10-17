using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

public sealed class DsrP3NidhoggSettingsWindow
{
    public DsrP3NidhoggStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerLine = null;
            Overrides.PlayerArrow = null;
        }

        if (SettingsGrid.Begin("##dsrp3nidhogg"))
        {
            SettingsGrid.Row("My debuff:");
            if (ImGui.RadioButton("Random##dsrline", Overrides.PlayerLine == null)) Overrides.PlayerLine = null;
            for (var line = 1; line <= DsrP3NidhoggState.Dives; line++)
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{Ordinal(line)} in Line##dsrline", Overrides.PlayerLine == line)) Overrides.PlayerLine = line;
            }

            SettingsGrid.Row("My arrow:");
            if (ImGui.RadioButton("Random##dsrarrow", Overrides.PlayerArrow == null)) Overrides.PlayerArrow = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Circle##dsrarrow", Overrides.PlayerArrow == DiveArrow.Circle)) Overrides.PlayerArrow = DiveArrow.Circle;
            ImGui.SameLine();
            if (ImGui.RadioButton("Up##dsrarrow", Overrides.PlayerArrow == DiveArrow.Up)) Overrides.PlayerArrow = DiveArrow.Up;
            ImGui.SameLine();
            if (ImGui.RadioButton("Down##dsrarrow", Overrides.PlayerArrow == DiveArrow.Down)) Overrides.PlayerArrow = DiveArrow.Down;
            SettingsGrid.End();
        }
    }

    private static string Ordinal(int line) => line switch { 1 => "First", 2 => "Second", _ => "Third" };
}
