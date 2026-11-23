namespace AsuMech.Scenarios.Uwu.P3Titan;

// A drill of Titan's three gaols, not a step of the fight.
public sealed class UwuP3TitanGaolsScenario : UwuP3TitanScenario, IScenario
{
    public override string Name => "Titan Gaols";
    protected override bool GaolsOnly => true;
    public bool InWholeRun => false;
    public float BgmSecondsAtStart => GaolsFrom;
}
