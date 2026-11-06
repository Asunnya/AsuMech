using System.Collections.Generic;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Dsr.P6DoubleDragons;

// The second half of the phase on its own, from the second Akh Afah to Revenge of the Horde.
public sealed class DsrP6CauterizeScenario : IScenario
{
    private readonly DsrP6DoubleDragonsScenario dragons = new(secondHalf: true);

    public string Name => dragons.Name;
    public IPhase Phase => dragons.Phase;
    public IReadOnlyList<IScenarioAi> AiStrats => dragons.AiStrats;
    public float BgmSecondsAtStart => dragons.BgmSecondsAtStart;
    public object SettingsOverrides => dragons.SettingsOverrides;

    public void Run(SimWorld world, int? selectedAi) => dragons.Run(world, selectedAi);
    public void Tick(float delta, float elapsed) => dragons.Tick(delta, elapsed);
}
