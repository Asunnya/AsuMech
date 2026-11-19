using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Umad.P2Trines;

public sealed class UmadP2TrinesSettingsWindow
{
    private static readonly string[] CleaveLabels = ["Random", "East half", "West half"];

    public UmadP2TrinesStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
            Overrides.CleaveWest = null;

        if (!SettingsGrid.Begin("##umadp2trines")) return;
        SettingsGrid.Row("Wings of Destruction hits:");
        var index = Overrides.CleaveWest switch { null => 0, false => 1, true => 2 };
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##umadtrinecleave", ref index, CleaveLabels, CleaveLabels.Length))
            Overrides.CleaveWest = index switch { 1 => false, 2 => true, _ => null };
        SettingsGrid.End();
    }
}
