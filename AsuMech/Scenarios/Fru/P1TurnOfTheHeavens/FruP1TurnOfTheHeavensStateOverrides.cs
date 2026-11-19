using System.Linq;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;

public enum BoundOfFaithDuty { Tethered, Untethered }

// User-forced rolls for FruP1TurnOfTheHeavensState; null leaves a field randomized.
public sealed class FruP1TurnOfTheHeavensStateOverrides
{
    public bool? CyclonicFire { get; set; }
    public bool? HeavensFire { get; set; }
    // The halos of flame on the west half (N, NW, W, SW), the halos of levin on the east.
    public bool? FlameWest { get; set; }

    public PerRoleSetting<BoundOfFaithDuty> BoundOfFaith { get; set; } = new();

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        conflicts.AtMost(2, PerRole.All.Where(r => BoundOfFaith[r] == BoundOfFaithDuty.Tethered).ToList(), "a Bound of Faith tether");
        conflicts.AtLeast(2, PerRole.All.Where(r => BoundOfFaith[r] == BoundOfFaithDuty.Untethered).ToList(), 8, "a Bound of Faith tether");
        return conflicts;
    }
}
