using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Tea.I2TemporalStasis;

// What the Ai reads off the arena; a peer's copies are filled in as they arrive.
public sealed class TeaI2TemporalStasisScenarioObjects
{
    public SimEnemy? AlexanderPrime { get; set; }
    public SimEnemy? BruteJustice { get; set; }
    public SimEnemy? CruiseChaser { get; set; }
}
