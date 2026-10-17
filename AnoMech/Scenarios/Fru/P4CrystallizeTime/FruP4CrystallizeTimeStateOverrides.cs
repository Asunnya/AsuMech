using System.Collections.Generic;

namespace AnoMech.Scenarios.Fru.P4CrystallizeTime;

// User-forced rolls for FruP4CrystallizeTimeState; null leaves a field randomized.
public sealed class FruP4CrystallizeTimeStateOverrides
{
    // The player gets one of these.
    public IReadOnlyList<CrystallizeDebuff>? PlayerDebuffs { get; set; }

    public bool? SlowNorthwest { get; set; }
    public CrystallizeCorner? Corner { get; set; }
}
