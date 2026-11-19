using System.Linq;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P1FallOfFaith;

public enum FallOfFaithTether { None, First, Second, Third, Fourth }

// User-forced rolls for FruP1FallOfFaithState; null leaves a field randomized.
public sealed class FruP1FallOfFaithStateOverrides
{
    // Per tether, in the order they go off.
    public bool?[] FaithFire { get; set; } = new bool?[4];
    public bool? TowersWest { get; set; }
    public bool? TowersFire { get; set; }
    // An index into FruP1FallOfFaithState.TowerPatterns.
    public int? TowerPattern { get; set; }

    public PerRoleSetting<FallOfFaithTether> Tether { get; set; } = new();

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        foreach (var slot in new[] { FallOfFaithTether.First, FallOfFaithTether.Second, FallOfFaithTether.Third, FallOfFaithTether.Fourth })
            conflicts.AtMost(1, PerRole.All.Where(r => Tether[r] == slot).ToList(), $"the {slot.ToString().ToLowerInvariant()} Fall of Faith tether");
        conflicts.AtLeast(4, PerRole.All.Where(r => Tether[r] == FallOfFaithTether.None).ToList(), 8, "a Fall of Faith tether");
        return conflicts;
    }
}
