using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.CurtainCall;

// Grotesquerie: Curtain Call, t=0 = P1 3:56. Five waves of puddles chase everyone while one role
// carries Rotting Flesh (cured by standing in the dragon head's cleave, then spreads) and the other
// Bursting Grotesquerie (spreads, outside the cleave); then each support is chained to a DPS and
// both break the chain apart, before the blood vessels burst and Splattershed's raidwide.
public sealed class M12sCurtainCallScenario : IScenario
{
    public string Name => "Curtain Call";
    public IPhase Phase => M12sZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sCurtainCallAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sCurtainCallSettingsWindow settingsWindow = new();

    public const float CurtainCallCastAt = 3.64f;
    public const float BondsAt = 7.57f;
    public const float DebuffsAt = 7.8f;
    public static readonly float[] SpotlightAt = [7.66f, 9.63f, 11.59f, 13.6f, 15.56f];
    public const float BlobsSpawnAt = 9.76f;
    public const float RavenousReachCastAt = 9.8f;
    public const float ConeCastAt = 10.88f;
    public const float HeadRisesAt = 12.79f;
    public const float ConeHitsAt = 21.48f;
    public const float ChainMarkersAt = 21.59f;
    public const float BurstingSpreadsAt = 21.86f;
    public const float BlobsShowAt = 23.87f;
    public const float HeadSinksAt = 24.74f;
    public const float ChainsAt = 26.59f;
    public const float BurstCastAt = 31.99f;
    // Retail starts hurting whoever is still chained here, every second until they die.
    public const float ChainsHeldAt = 32.03f;
    public const float BurstHitsAt = 34.48f;
    public const float SplattershedCastAt = 38.14f;
    public const float SplattershedHitsAt = 43.63f;
    private const float EndsAt = 46f;

    private const float RottingSeconds = 16f;
    private const float BurstingSeconds = 14f;
    private const float BondsSeconds = 19f;
    private const float ChainSeconds = 18f;
    private const float CellSheddingDelay = 0.04f;
    // UNVERIFIED: how far apart a chain snaps; retail's broke 26-28y apart at the hit.
    public const float ChainBreaksAt = 25f;

    private const ushort BlobDormant = 1;
    private const ushort BlobShows = 0x10;
    private const ushort BlobShown = 0x20;
    private const ushort BlobPrimes = 1;
    private const ushort BlobPrimed = 2;
    private const ushort BlobBursts = 4;
    private const ushort BlobBurst = 8;
    private const float BlobPrimesBefore = 0.09f;
    private const float BlobLingers = 1.05f;

    // The party zones in at the entrance, south of the stage; the edge kills once they have walked on.
    private const float BoundaryAt = 3.5f;

    private SimWorld world = null!;
    private M12sCurtainCallState state = null!;
    private SimEnemy? lindwurm;
    private SimEnemy? snakeHead;
    private SimEnemy? coneHelper;
    private SimEnemy? raidwideHelper;
    private readonly SimEnemy?[] spotlightHelpers = new SimEnemy?[24];
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] chainHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] burstHelpers = new SimEnemy?[5];
    private readonly List<SimEventObject> blobs = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sCurtainCallState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        blobs.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sCurtainCallState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(BoundaryAt, () => world.EnforceArenaBoundary(M12sZone.Instance.Arena, "Fell off the stage"));
        world.Events.Add(CurtainCallCastAt, () => lindwurm?.Cast(ActionId.GrotesquerieCurtainCall, animationLock: 0.2f));
        world.Events.Add(BondsAt, () => { foreach (var m in AliveMembers()) m.AddStatus(StatusId.BondsOfFleshAlpha, BondsSeconds); });
        world.Events.Add(DebuffsAt, HandOutGrotesquerie);
        for (var w = 0; w < SpotlightAt.Length; w++)
        {
            var wave = w;
            world.Events.Add(SpotlightAt[wave], () => DropSpotlights(wave));
        }
        world.Events.Add(BlobsSpawnAt, SpawnBlobs);
        world.Events.Add(RavenousReachCastAt, () => lindwurm?.Cast(ActionId.RavenousReach, animationLock: 0.2f));
        world.Events.Add(ConeCastAt, () => coneHelper?.Cast(Actions.CurtainCallRavenousReach));
        world.Events.Add(HeadRisesAt, RaiseTheHead);
        world.Events.Add(13.29f, () => lindwurm?.SetModelState(ModelState.CurtainCall));
        world.Events.Add(ChainMarkersAt, () => { foreach (var m in AliveMembers()) m.AttachLockonVfx(LockonId.CellChain); });
        world.Events.Add(21.77f, () => lindwurm?.SetModelState(ModelState.Idle));
        world.Events.Add(BurstingSpreadsAt, BurstingSpreads);
        world.Events.Add(BlobsShowAt, () => AnimateBlobs(BlobShows, BlobShown));
        world.Events.Add(HeadSinksAt, () => snakeHead?.SetVisible(false));
        world.Events.Add(ChainsAt, ChainThePairs);
        world.Events.Add(BurstCastAt - BlobPrimesBefore, () => AnimateBlobs(BlobPrimes, BlobPrimed));
        world.Events.Add(BurstCastAt, Burst);
        world.Events.Add(ChainsHeldAt, KillTheStillChained);
        world.Events.Add(BurstHitsAt, () => AnimateBlobs(BlobBursts, BlobBurst));
        world.Events.Add(BurstHitsAt + BlobLingers, ClearBlobs);
        world.Events.Add(SplattershedCastAt, () => lindwurm?.Cast(ActionId.Splattershed, animationLock: 0.2f));
        world.Events.Add(SplattershedHitsAt, () => raidwideHelper?.Cast(Actions.SplattershedRaidwide));
        world.Events.Add(EndsAt, () => { });
    }

    public void OnStatusLost(StatusLost lost)
    {
        if (lost.StatusId != StatusId.RottingFlesh) return;
        var holder = lost.Holder;
        if (lost.Reason == StatusLossReason.Removed)
            world.Events.Add(CellSheddingDelay, () => CastAt(spreadHelpers, Actions.CellShedding, holder));
        else if (lost.Reason == StatusLossReason.Expired)
            CastAt(spreadHelpers, Actions.CellDeath, holder);
    }

    public void OnTetherEnded(TetherEnded ended)
    {
        if (ended.Tether.TetherId != TetherId.CellChain || ended.Reason != TetherEndReason.Stretched) return;
        foreach (var end in new[] { ended.Tether.A, ended.Tether.B }.OfType<SimCharacter>().Where(e => e.IsAlive()))
        {
            end.RemoveStatus(StatusId.UnbreakableFleshAlpha);
            CastAt(chainHelpers, Actions.ChainBreak, end);
        }
    }

    private void SpawnActors()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP1,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(new Vector3(0f, 0f, -15f), 0f)));
        snakeHead = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.SnakeHead,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(state.HeadSpot, 0f)));
        snakeHead?.SetVisible(false);
        coneHelper = SpawnHelper(new Placement(state.ConeOrigin, -state.DragonSide * MathF.PI / 2f));
        raidwideHelper = SpawnHelper(new Placement(Vector3.Zero, 0f));
        for (var i = 0; i < spotlightHelpers.Length; i++) spotlightHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < chainHelpers.Length; i++) chainHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < burstHelpers.Length; i++) burstHelpers[i] = SpawnHelper(new Placement(state.Blobs[i], 0f));
    }

    private SimEnemy? SpawnHelper(Placement at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.LindwurmP1,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: at));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    // Each member has its own helper in `helpers`.
    private static void CastAt(SimEnemy?[] helpers, Core.EnemyActions.EnemyAction action, SimCharacter target)
    {
        var helper = target is ISimPartyMember member ? helpers[(int)member.Role] : null;
        helper?.SetPosition(new Placement(target.Position, 0f));
        helper?.Cast(action, target);
    }

    private void HandOutGrotesquerie()
    {
        foreach (var role in state.Rotting) Alive(role)?.AddStatus(StatusId.RottingFlesh, RottingSeconds);
        foreach (var role in state.Bursting) Alive(role)?.AddStatus(StatusId.BurstingGrotesquerie, BurstingSeconds);
    }

    private void DropSpotlights(int wave)
    {
        var i = 0;
        foreach (var member in AliveMembers())
        {
            var helper = spotlightHelpers[wave % 3 * 8 + i++];
            helper?.SetPosition(new Placement(member.Position, 0f));
            helper?.Cast(Actions.PhagocyteSpotlight, member.Position);
        }
    }

    private void SpawnBlobs()
    {
        foreach (var at in state.Blobs)
            if (world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.BloodVessel, Placement = new Placement(at, 0f), TimelineState = BlobDormant }) is { } blob)
                blobs.Add(blob);
    }

    private void AnimateBlobs(ushort from, ushort to)
    {
        foreach (var blob in blobs) blob.UpdateSharedTimelineState(from, to);
    }

    private void ClearBlobs()
    {
        foreach (var blob in blobs) blob.Despawn();
        blobs.Clear();
    }

    private void RaiseTheHead()
    {
        snakeHead?.SetVisible(true);
        world.Events.Add(0.59f, () => snakeHead?.SetModelState(ModelState.SnakeHeadOut));
    }

    private void BurstingSpreads()
    {
        foreach (var member in state.Bursting.Select(Alive).OfType<SimCharacter>())
            CastAt(spreadHelpers, Actions.DramaticLysis, member);
    }

    private void ChainThePairs()
    {
        foreach (var support in M12sCurtainCallState.Supports)
        {
            if (Alive(support) is not { } a || Alive(state.ChainPartner[support]) is not { } b) continue;
            foreach (var end in new[] { a, b })
            {
                end.RemoveStatus(StatusId.BondsOfFleshAlpha);
                end.AddStatus(StatusId.UnbreakableFleshAlpha, ChainSeconds);
            }
            world.Tether(a, b, TetherId.CellChain, ChainSeconds).BreaksWhen(t => t.StretchGt(ChainBreaksAt));
        }
    }

    // UNVERIFIED: dead at once; retail ticks hard every second until the chain breaks or runs out.
    private void KillTheStillChained()
    {
        foreach (var member in AliveMembers().Where(m => m.HasStatus(StatusId.UnbreakableFleshAlpha)))
            member.Die(ActionId.ChainHeld, "pull your chain apart until it breaks");
    }

    private void Burst()
    {
        foreach (var helper in burstHelpers) helper?.Cast(Actions.Burst);
    }
}
