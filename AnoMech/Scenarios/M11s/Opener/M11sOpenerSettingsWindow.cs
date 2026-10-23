using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.Opener;

public sealed class M11sOpenerSettingsWindow
{
    private static readonly string[] TrophyLabels = ["Random", "Axe", "Scythe"];
    private static readonly string[] CometLabels = ["Random", "Stack first", "Spreads first"];
    private static readonly string[] CometTargetLabels = ["Random", "H1", "H2"];
    private static readonly Weapon[][] Orders =
    [
        [Weapon.Axe, Weapon.Scythe, Weapon.Sword],
        [Weapon.Axe, Weapon.Sword, Weapon.Scythe],
        [Weapon.Scythe, Weapon.Axe, Weapon.Sword],
        [Weapon.Scythe, Weapon.Sword, Weapon.Axe],
        [Weapon.Sword, Weapon.Axe, Weapon.Scythe],
        [Weapon.Sword, Weapon.Scythe, Weapon.Axe],
    ];
    private static readonly string[] OrderLabels =
        ["Random", "Axe, Scythe, Sword", "Axe, Sword, Scythe", "Scythe, Axe, Sword", "Scythe, Sword, Axe", "Sword, Axe, Scythe", "Sword, Scythe, Axe"];

    public M11sOpenerStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.AxeTrophyFirst = null;
            Overrides.FirstWeapons = null;
            Overrides.SecondWeapons = null;
            Overrides.StackFirst = null;
            Overrides.CrushingCometTarget = null;
        }

        if (SettingsGrid.Begin("##m11sopener"))
        {
            SettingsGrid.Row("Raw Steel Trophy:");
            var trophy = Overrides.AxeTrophyFirst is { } axe ? (axe ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11strophy", ref trophy, TrophyLabels, TrophyLabels.Length))
                Overrides.AxeTrophyFirst = trophy == 0 ? null : trophy == 1;

            SettingsGrid.Row("First weapons:");
            Overrides.FirstWeapons = OrderCombo("##m11sfirstweapons", Overrides.FirstWeapons);

            SettingsGrid.Row("Second weapons:");
            Overrides.SecondWeapons = OrderCombo("##m11ssecondweapons", Overrides.SecondWeapons);

            SettingsGrid.Row("Void Stardust:");
            var comet = Overrides.StackFirst is { } stack ? (stack ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11scomet", ref comet, CometLabels, CometLabels.Length))
                Overrides.StackFirst = comet == 0 ? null : comet == 1;

            SettingsGrid.Row("Crushing Comet on:");
            var target = Overrides.CrushingCometTarget switch
            {
                PartyRole.RegenHealer => 1,
                PartyRole.ShieldHealer => 2,
                _ => 0,
            };
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11scomettarget", ref target, CometTargetLabels, CometTargetLabels.Length))
                Overrides.CrushingCometTarget = target switch
                {
                    1 => PartyRole.RegenHealer,
                    2 => PartyRole.ShieldHealer,
                    _ => null,
                };
            SettingsGrid.End();
        }
    }

    private static IReadOnlyList<Weapon>? OrderCombo(string id, IReadOnlyList<Weapon>? current)
    {
        var selected = 0;
        for (var i = 0; i < Orders.Length && current != null; i++)
            if (Orders[i][0] == current[0] && Orders[i][1] == current[1]) selected = i + 1;
        SettingsGrid.ItemWidth(160);
        ImGui.Combo(id, ref selected, OrderLabels, OrderLabels.Length);
        return selected == 0 ? null : Orders[selected - 1];
    }
}
