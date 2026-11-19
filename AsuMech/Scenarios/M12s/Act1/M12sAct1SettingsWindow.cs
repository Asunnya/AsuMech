using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.Act1;

public sealed class M12sAct1SettingsWindow
{
    private static readonly string[] DebuffLabels = ["Random", "Bursting (spread)", "Stack", "Shared (stack target)"];
    private static readonly string[] ArrowLabels = ["Random", "Front", "Right", "Back", "Left"];
    private static readonly string[] SafeLabels = ["Random", "West (head on D)", "East (head on B)"];
    private static readonly string[] VesselLabels = ["Random", "Safe on west wall and NE", "Safe in NW and east wall"];
    private static readonly string[] HealerLabels = ["Random", "H1", "H2"];

    public M12sAct1StateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerDebuff = null;
            Overrides.PlayerArrow = null;
            Overrides.EastSafe = null;
            Overrides.VesselLayout = null;
            Overrides.StackHealer = null;
        }

        if (SettingsGrid.Begin("##m12sact1"))
        {
            SettingsGrid.Row("My Grotesquerie:");
            var debuff = Overrides.PlayerDebuff is { } d ? (int)d + 1 : 0;
            SettingsGrid.ItemWidth(160);
            if (ImGui.Combo("##m12sact1debuff", ref debuff, DebuffLabels, DebuffLabels.Length))
                Overrides.PlayerDebuff = debuff == 0 ? null : (Act1Debuff)(debuff - 1);

            SettingsGrid.Row("My arrow:");
            var arrow = Overrides.PlayerArrow is { } a ? (int)a + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sact1arrow", ref arrow, ArrowLabels, ArrowLabels.Length))
                Overrides.PlayerArrow = arrow == 0 ? null : (GrotesquerieArrow)(arrow - 1);

            SettingsGrid.Row("Safe side:");
            var safe = Overrides.EastSafe is { } east ? (east ? 2 : 1) : 0;
            SettingsGrid.ItemWidth(160);
            if (ImGui.Combo("##m12sact1safe", ref safe, SafeLabels, SafeLabels.Length))
                Overrides.EastSafe = safe == 0 ? null : safe == 2;

            SettingsGrid.Row("Blood vessels:");
            var layout = Overrides.VesselLayout is { } l ? l + 1 : 0;
            SettingsGrid.ItemWidth(200);
            if (ImGui.Combo("##m12sact1vessels", ref layout, VesselLabels, VesselLabels.Length))
                Overrides.VesselLayout = layout == 0 ? null : layout - 1;

            SettingsGrid.Row("Fourth-wall Fusion on:");
            var healer = Overrides.StackHealer switch
            {
                PartyRole.RegenHealer => 1,
                PartyRole.ShieldHealer => 2,
                _ => 0,
            };
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sact1healer", ref healer, HealerLabels, HealerLabels.Length))
                Overrides.StackHealer = healer switch
                {
                    1 => PartyRole.RegenHealer,
                    2 => PartyRole.ShieldHealer,
                    _ => null,
                };
            SettingsGrid.End();
        }
    }
}
