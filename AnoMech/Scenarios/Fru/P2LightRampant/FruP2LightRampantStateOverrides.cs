using System.Linq;

namespace AnoMech.Scenarios.Fru.P2LightRampant;

public enum LightRampantDuty { Tether, Bait }

// User-forced rolls for FruP2LightRampantState; null leaves a field randomized.
public sealed class FruP2LightRampantStateOverrides
{
    public bool? NorthOrbsFirst { get; set; }
    public bool? Pairs { get; set; }

    // Bait = one of the two Luminous Hammer baiters; Tether = one of the six chained.
    public PerRoleSetting<LightRampantDuty> Duty { get; set; } = new();

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;
        conflicts.AtMost(2, PerRole.All.Where(r => Duty[r] == LightRampantDuty.Bait).ToList(), "a Luminous Hammer bait");
        conflicts.AtMost(6, PerRole.All.Where(r => Duty[r] == LightRampantDuty.Tether).ToList(), "a tether");
        return conflicts;
    }
}
