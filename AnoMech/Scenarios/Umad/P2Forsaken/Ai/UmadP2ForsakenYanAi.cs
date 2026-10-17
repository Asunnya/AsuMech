using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad.P2Forsaken.Ai;

// JP strat: forked from the NA "Kroxy-Rinon 341 (melee flex)" strat. Runs over its own
// helper (UmadP2ForsakenYanAiHelper) with Yan's tower spots and the last Past's/Future's
// End baited at the A marker, diverging from the NA version without affecting it.
public sealed class UmadP2ForsakenYanAi : IScenarioAi<UmadP2ForsakenState>
{
    public string Name => "Yan";
    public string? Group => "JP";

    public void Run(UmadP2ForsakenState state, SimWorld world)
        => new UmadP2ForsakenYanAiHelper(
               UmadP2ForsakenYanAiHelper.YanOdd,
               UmadP2ForsakenYanAiHelper.YanEven,
               UmadP2ForsakenYanAiHelper.KroxyReorder).Run(state, world);
}
