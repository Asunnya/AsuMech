using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;

namespace AsuMech.Scenarios.Ucob.P3SeventhUmbralEra;

// Bahamut Prime lands after Nael leaves: Seventh Umbral Era, the Calamitous raidwides, then the main
// tank picks him up for Flare Breath and Flatten, up to the Quickmarch Trio. t=0 is 2.7s before
// Seventh Umbral Era lands, where P2 ends. The raidwides' HP cost is not simulated.
public sealed class UcobP3SeventhUmbralEraScenario : IScenario
{
    public string Name => "Seventh Umbral Era";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3SeventhUmbralEraAi(), new UcobP3SeventhUmbralEraAi(tuufless: true)];

    private const float BahamutAppears = 1.30f;
    private const float BahamutLands = 1.67f;
    public const float SeventhUmbralEra = 2.70f;
    public static readonly float[] CalamitousFlames = [5.69f, 6.76f, 7.83f];
    public const float CalamitousBlaze = 10.69f;
    public const float BahamutTargetable = 13.73f;
    private static readonly float[] AutoAttacks = [13.73f, 16.76f, 19.79f, 22.82f];
    public const float FlareBreath = 19.88f;
    public const float Flatten = 23.98f;
    public const float End = 28f;

    private SimWorld world = null!;
    private SimEnemy? bahamut;
    private readonly UcobNeurolinks neurolinks = new();

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        bahamut = null;
        neurolinks.Clear();
        var markers = UcobMarkers.Of(AiStrats, selectedAi);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UcobMarkers>)AiStrats[idx]).Run(markers, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(markers.Waymarks));
        world.Events.Add(0f, () => { foreach (var at in markers.Place(Geometry.P3Neurolinks)) neurolinks.Drop(world, at); });
        world.Events.Add(BahamutAppears, SpawnBahamut);
        world.Events.Add(BahamutLands, () => bahamut?.PlayActionTimeline(TimelineId.BahamutPrimeLands));

        world.Events.Add(SeventhUmbralEra, () => RaidwideFromTheCentre(Actions.SeventhUmbralEra));
        foreach (var at in CalamitousFlames) world.Events.Add(at, () => RaidwideFromTheCentre(Actions.CalamitousFlame));
        world.Events.Add(CalamitousBlaze, () => RaidwideFromTheCentre(Actions.CalamitousBlaze));

        world.Events.Add(BahamutTargetable, () =>
        {
            bahamut?.SetTargetable(true);
            TankBahamut();
        });
        foreach (var at in AutoAttacks) world.Events.Add(at, () => OnMainTank(Actions.AutoAttack));
        world.Events.Add(FlareBreath, () => OnMainTank(Actions.FlareBreath));
        world.Events.Add(Flatten, () => OnMainTank(Actions.Flatten));
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed) => neurolinks.Tick(world.Party);

    // He lands facing south until the main tank picks him up.
    private void SpawnBahamut()
    {
        bahamut = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.BahamutPrime,
            NameId: BNpcNameId.BahamutPrime,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, 0f)));
    }

    private void RaidwideFromTheCentre(EnemyAction action)
        => bahamut?.CastFromHelper(action, at: new Placement(Vector3.Zero, 0f));

    private void TankBahamut()
    {
        var tank = world.Party.Get(PartyRole.MainTank);
        bahamut?.SetTarget(tank, follow: false);
        bahamut?.KeepFacing(tank);
    }

    private void OnMainTank(EnemyAction action)
    {
        if (world.Party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive()) bahamut?.Cast(action, tank);
    }

    private void DespawnAll()
    {
        neurolinks.Clear();
        bahamut?.Despawn();
        bahamut = null;
    }
}
