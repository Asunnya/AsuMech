using System.Collections.Generic;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.P4Materialization.FruP4MaterializationState;

namespace AsuMech.Scenarios.Fru.P4Materialization;

// t=0 is the Usurper spawning after Memory's End; it runs up to Darklit Dragonsong's start.
public sealed class FruP4MaterializationScenario : IScenario
{
    public string Name => "Materialization";
    public IPhase Phase => FruZone.P4;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP4MaterializationAi()];

    private SimWorld world = null!;
    private SimEnemy? usurper, gaia, ryne;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        var state = new FruP4MaterializationState();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP4MaterializationState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1.6f, () => BattleTalk(0x1C));
        world.Events.Add(TargetableAt, Appear);
        world.Events.Add(9.7f, () => BattleTalk(0x1D));
        world.Events.Add(MaterializationAt, () => usurper?.Cast(ActionId.Materialization));
        world.Events.Add(16f, () => world.Map.AddEffect(MapEffect.FragmentOfFateShow, MapEffect.FragmentOfFate));
        world.Events.Add(FragmentAt, () => world.SpawnEnemy(FruP4Actors.Fragment()));
        // UNVERIFIED that this timeline is what reveals them.
        world.Events.Add(17f, () => gaia?.PlayActionTimeline(TimelineId.VisionAppear));
        world.Events.Add(17f, () => ryne?.PlayActionTimeline(TimelineId.VisionAppear));
    }

    private void SpawnActors()
    {
        usurper = world.SpawnEnemy(FruP4Actors.Usurper(shown: false));
        gaia = world.SpawnEnemy(FruP4Actors.VisionOfGaia(shown: false));
        ryne = world.SpawnEnemy(FruP4Actors.VisionOfRyne(shown: false));
    }

    private void Appear()
    {
        if (usurper is not { } boss) return;
        boss.SetTargetable(true);
        boss.PlayActionTimeline(ActionTimelineId.WarpEnd);
        boss.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false);
    }

    private void BattleTalk(uint line)
    {
        if (usurper is { } speaker) world.Map.DirectorUpdate(0x80000027U, line, 0x2U, UsurperBattleTalk, speaker.EntityId);
    }
}
