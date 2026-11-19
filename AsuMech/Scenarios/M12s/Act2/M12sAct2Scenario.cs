using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.Act2;

// Grotesquerie: Act 2, t=0 = P1 1:44. Four blobs drop in, Cruel Coil wraps the party in the
// middle, and pair by pair a chain between an α (out) and a β (in) is stretched until it snaps;
// each snap and each blob leaves a one-player tower. Constrictor then kills inside the coil, and
// Splattershed ends it.
public sealed class M12sAct2Scenario : IScenario
{
    public string Name => "Grotesquerie: Act 2";
    public IPhase Phase => M12sZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sAct2Ai()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sAct2SettingsWindow settingsWindow = new();

    public const float ActCastAt = 2.49f;
    public const float BondsAt = 6.64f;
    public static readonly float[] SpotlightAt = [11.72f, 13.68f, 15.69f, 17.70f];
    public static readonly float[] BlobAppearsAt = [15.33f, 17.30f, 19.31f, 21.32f];
    public const float CoilCastAt = 17.66f;
    public const float CoilLandsAt = 20.66f;
    public const float CoilBindsAt = 21.67f;
    public const float CoilClosesAt = 21.76f;
    public static readonly float[] SpinAt = [26.68f, 31.63f, 36.59f, 41.54f, 46.49f, 51.45f, 56.40f];
    public static readonly float[] PulseAfterSpin = [0.40f, 0.98f];
    public static readonly float[] ChainMarkedAt = [27.61f, 32.61f, 37.62f, 42.61f];
    public static readonly float[] ChainAt = [32.61f, 37.62f, 42.61f, 47.61f];
    public static readonly float[] BlobTowerCastAt = [35.47f, 40.42f, 45.47f, 50.46f];
    public const float TowerResolves = 3f;
    public const float ChainTowerCastDelay = 1.11f;
    // UNVERIFIED: how long an unbroken chain lasts before it bursts.
    public const float ChainFailsAfter = 5f;
    public const float ConstrictorAt = 61.35f;
    public const float CoilOpensAt = 62.86f;
    public const float BossReturnsAt = 66.9f;
    public const float VesselsGoneAt = 68.31f;
    public const float SplattershedCastAt = 68.52f;
    public const float SplattershedHitsAt = 74.0f;
    public const float EndsAt = 76f;
    public const float ChainSnapsAt = 20f;

    public static readonly Vector3 BossHome = new(0f, 0f, -15f);

    private SimWorld world = null!;
    private M12sAct2State state = null!;
    private SimEnemy? boss;
    private readonly SimEnemy?[] blobs = new SimEnemy?[4];
    private readonly List<SimEnemy> chainVessels = [];
    private readonly SimTether?[] chains = new SimTether?[4];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sAct2State(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        chainVessels.Clear();
        Array.Clear(chains);
        Array.Clear(blobs);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sAct2State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new RectArena(Geometry.P1HalfWidth, Geometry.P1HalfDepth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnBoss);
        world.Events.Add(1f, () => boss?.SetTarget(Alive(PartyRole.MainTank)));
        world.Events.Add(ActCastAt, () => boss?.Cast(ActionId.GrotesquerieAct2, animationLock: 0.2f));
        world.Events.Add(BondsAt, GiveBonds);
        for (var b = 0; b < 4; b++)
        {
            var blob = b + 1;
            world.Events.Add(SpotlightAt[b], () => boss?.CastFromHelper(Actions.Act2PhagocyteSpotlight, ToWorld(state.BlobSpot(blob)), at: new Placement(ToWorld(state.BlobSpot(blob)), 0f)));
            world.Events.Add(BlobAppearsAt[b] - 0.3f, () => blobs[blob - 1] = SpawnVessel(state.BlobSpot(blob)));
            world.Events.Add(BlobAppearsAt[b], () => blobs[blob - 1]?.PlayActionTimeline(ActionTimeline.BloodVesselAppears));
        }
        world.Events.Add(CoilCastAt, CruelCoil);
        world.Events.Add(CoilLandsAt, () => TurnBoss(state.Facing(1)));
        world.Events.Add(CoilBindsAt, () => boss?.CastFromHelper(Actions.CruelCoilBind, at: new Placement(Vector3.Zero, 0f)));
        world.Events.Add(CoilClosesAt, () => boss?.SetModelState(ModelState.Coiled));
        for (var s = 0; s < M12sAct2State.Spins; s++)
        {
            var spin = s + 1;
            world.Events.Add(SpinAt[s], () => Skinsplitter(spin));
            foreach (var after in PulseAfterSpin)
                world.Events.Add(SpinAt[s] + after, () => boss?.CastFromHelper(Actions.SkinsplitterDonut, at: new Placement(Vector3.Zero, state.Facing(spin))));
        }
        for (var p = 0; p < M12sAct2State.Pairs; p++)
        {
            var order = p + 1;
            world.Events.Add(ChainMarkedAt[p], () => MarkPair(order));
            world.Events.Add(ChainAt[p], () => Chain(order));
            world.Events.Add(ChainAt[p] + ChainFailsAfter, () => FailChain(order));
            world.Events.Add(BlobTowerCastAt[p], () => blobs[order - 1]?.Cast(Actions.RoilingMassBlob));
        }
        world.Events.Add(ConstrictorAt, Constrictor);
        world.Events.Add(CoilOpensAt, () => boss?.SetModelState(ModelState.Normal));
        world.Events.Add(BossReturnsAt, () =>
        {
            boss?.SetPosition(new Placement(BossHome, 0f));
            boss?.SetTargetable(true);
        });
        world.Events.Add(VesselsGoneAt, RemoveVessels);
        world.Events.Add(SplattershedCastAt, () => boss?.Cast(ActionId.Act2Splattershed, animationLock: 0.2f));
        world.Events.Add(SplattershedHitsAt, () => boss?.CastFromHelper(Actions.SplattershedRaidwide, at: new Placement(Vector3.Zero, 0f)));
        world.Events.Add(EndsAt, () => { });
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        Towers.ResolveTower(world, resolved, ActionId.RoilingMassBlob, Actions.UnmitigatedExplosion, 1);
        Towers.ResolveTower(world, resolved, ActionId.RoilingMassChain, Actions.UnmitigatedExplosion, 1);
    }

    // A snapped chain bursts on both ends and leaves a tower where the β stood.
    public void OnTetherEnded(TetherEnded ended)
    {
        if (ended.Tether.TetherId != TetherId.Cell || ended.Reason != TetherEndReason.Stretched) return;
        if (ended.Tether.A is not { } alpha || ended.Tether.B is not { } beta) return;
        alpha.RemoveStatus(StatusId.UnbreakableFleshAlpha);
        beta.RemoveStatus(StatusId.UnbreakableFleshBeta);
        boss?.CastFromHelper(Actions.DramaticLysisChainBreak, at: new Placement(alpha.Position, 0f));
        boss?.CastFromHelper(Actions.DramaticLysisChainBreak, at: new Placement(beta.Position, 0f));
        var spot = new Vector2(beta.Position.X, beta.Position.Z);
        if (SpawnVessel(spot) is not { } vessel) return;
        chainVessels.Add(vessel);
        world.Events.Add(0.2f, () => vessel.PlayActionTimeline(ActionTimeline.BloodVesselAppears));
        world.Events.Add(ChainTowerCastDelay, () => vessel.Cast(Actions.RoilingMassChain));
    }

    private void SpawnBoss()
    {
        boss = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP1,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(BossHome, 0f)));
    }

    private SimEnemy? SpawnVessel(Vector2 at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.BloodVessel,
        NameId: BNpcNameId.BloodVessel,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Placement: new Placement(ToWorld(at), 0f)));

    private static Vector3 ToWorld(Vector2 p) => new(p.X, 0f, p.Y);

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void GiveBonds()
    {
        ushort[] inLine = [StatusId.FirstInLine, StatusId.SecondInLine, StatusId.ThirdInLine, StatusId.FourthInLine];
        foreach (var (role, order) in state.Order)
        {
            if (Alive(role) is not { } member) continue;
            var bonds = role.IsDps() ? StatusId.BondsOfFleshAlpha : StatusId.BondsOfFleshBeta;
            member.AddStatus(bonds, ChainAt[order - 1] - BondsAt);
            member.AddStatus(inLine[order - 1], EndsAt - BondsAt);
        }
    }

    private void CruelCoil()
    {
        var facing = state.FirstFacing switch { 1 => 0, 3 => 1, 0 => 2, _ => 3 };
        boss?.SetTargetable(false);
        boss?.Cast(ActionId.CruelCoilEast + (uint)facing, Vector3.Zero, animationLock: 0.2f);
    }

    private void TurnBoss(float facing) => boss?.SetPosition(new Placement(Vector3.Zero, facing));

    private void Skinsplitter(int spin)
    {
        TurnBoss(state.Facing(spin));
        boss?.Cast(ActionId.SkinsplitterBoss, animationLock: 0.2f);
    }

    private void MarkPair(int order)
    {
        Alive(state.Alpha(order))?.AttachLockonVfx(LockonId.CellChainSpawn);
        Alive(state.Beta(order))?.AttachLockonVfx(LockonId.CellChainSpawn);
    }

    private void Chain(int order)
    {
        if (Alive(state.Alpha(order)) is not { } alpha || Alive(state.Beta(order)) is not { } beta) return;
        alpha.AddStatus(StatusId.UnbreakableFleshAlpha, 18f);
        beta.AddStatus(StatusId.UnbreakableFleshBeta, 18f);
        chains[order - 1] = world.Tether(alpha, beta, TetherId.Cell).BreaksWhen(t => t.StretchGt(ChainSnapsAt));
    }

    private void FailChain(int order)
    {
        if (chains[order - 1] is not { IsActive: true } chain) return;
        chain.Despawn();
        foreach (var end in new[] { chain.A, chain.B })
            if (end is { } member && member.IsAlive())
                boss?.CastFromHelper(Actions.DramaticLysisChainFail, member, at: new Placement(member.Position, 0f));
    }

    private void Constrictor()
    {
        var facing = state.FirstFacing switch { 1 => 0, 3 => 1, 0 => 2, _ => 3 };
        boss?.Cast(ActionId.ConstrictorEast + (uint)facing, animationLock: 0.2f);
        boss?.CastFromHelper(Actions.ConstrictorKill, at: new Placement(Vector3.Zero, 0f));
    }

    private void RemoveVessels()
    {
        foreach (var vessel in blobs) vessel?.Despawn();
        foreach (var vessel in chainVessels) vessel.Despawn();
        chainVessels.Clear();
    }
}
