using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.Replication2;

public sealed class M12sReplication2SettingsWindow
{
    private static readonly string[] SpotLabels = ["Random", .. M12sReplication2State.SpotNames];
    private static readonly string[] NetherwrathLabels = ["Random", "Near", "Far"];

    public M12sReplication2StateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerSpot = null;
            Overrides.NetherwrathFar = null;
        }

        if (SettingsGrid.Begin("##m12srep2"))
        {
            SettingsGrid.Row("My clone:");
            var spot = (Overrides.PlayerSpot ?? -1) + 1;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12srep2spot", ref spot, SpotLabels, SpotLabels.Length))
                Overrides.PlayerSpot = spot == 0 ? null : spot - 1;

            SettingsGrid.Row("Netherwrath:");
            var far = Overrides.NetherwrathFar is { } f ? (f ? 2 : 1) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12srep2nether", ref far, NetherwrathLabels, NetherwrathLabels.Length))
                Overrides.NetherwrathFar = far == 0 ? null : far == 2;
            SettingsGrid.End();
        }
    }
}
