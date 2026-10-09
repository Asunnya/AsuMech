using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P2Thordan;

// Sanctity of the Ward's meteors on their own, without playing the phase up to them.
public sealed class DsrP2MeteorsScenario : IScenario
{
    private readonly DsrP2ThordanScenario thordan = new(meteorsOnly: true);

    public string Name => thordan.Name;
    public IPhase Phase => thordan.Phase;
    public IReadOnlyList<IScenarioAi> AiStrats => thordan.AiStrats;
    public float BgmSecondsAtStart => thordan.BgmSecondsAtStart;
    public object SettingsOverrides => thordan.SettingsOverrides;

    public void Run(SimWorld world, int? selectedAi) => thordan.Run(world, selectedAi);
    public void Tick(float delta, float elapsed) => thordan.Tick(delta, elapsed);
}
