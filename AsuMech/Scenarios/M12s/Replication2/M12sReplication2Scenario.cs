using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.Replication2;

// Staging to Reenactment (t=0 = P2 1:22, 5.8s before Staging): eight player clones, each tied to one
// player; six boss clones whose cone, defamation and stack tethers (and the boss's jump tether) the
// players pick up; Firefall Splash with four thin cleaves, the defamations, light party stacks and the
// cones; Snaking Kick; then Netherwrath's partner stacks while each player clone, in the order they
// appeared, replays what its player took.
public sealed class M12sReplication2Scenario : IScenario
{
    public string Name => "Replication 2";
    public IPhase Phase => M12sZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sReplication2Ai()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sReplication2SettingsWindow settingsWindow = new();

    public const float ArenaRadius = 20f;
    public const float StagingCastAt = 5.81f;
    public static readonly float[] CloneRevealAt = [10.07f, 11.58f, 13.1f, 14.61f];
    public const float OwnerTethersAt = 16.66f;
    public const float ReplicationCastAt = 19.95f;
    public const float PlayerClonesHideAt = 20.22f;
    public const float BossClonesShowAt = 29.27f;
    public const float TethersAt = 31.36f;
    public const float TethersLockAt = 39.39f;
    public const float FirefallCastAt = 39.52f;
    public const float JumpAt = 45.19f;
    public const float ScaldingAt = 46.03f;
    public const float ManaBurstVisualAt = 46.22f;
    public const float ManaBurstAt = 47.5f;
    public const float HeavySlamAt = 52.98f;
    public const float SnakingKickCastAt = 53.52f;
    public const float SnakingKickResolves = 5f;
    public const float GrotesquerieAt = 54.28f;
    public const float ProjectionAt = 54.81f;
    public const float ReenactmentCastAt = 65.74f;
    public const float PlayerClonesReturnAt = 69.98f;
    public const float NetherwrathCastAt = 71.89f;
    public const float TimelessSpiteAt = 78.17f;
    public static readonly float[] ReplayAt = [76.83f, 80.8f, 84.81f, 88.81f];
    public const float ReplayDefamationAfter = 1.29f;
    public const float ReplayStackAfter = 1.51f;
    public const float ReplayScaldingAfter = 1.51f;
    public const float ReplayMarkAfter = 1.3f;
    public const float ReplayConeAfter = 1.83f;
    public const float DashAt = 96.07f;
    // UNVERIFIED for the replays' stacks.
    private const float UnsoakedStackPunishedAfter = 4.05f;
    private const float ReplayCloneLingers = 3f;
    // UNVERIFIED: how close to a clone, or to the boss, a player must stand to take its tether.
    public const float TetherReach = 3f;

    private SimWorld world = null!;
    private M12sReplication2State state = null!;
    private SimEnemy? boss;
    private readonly SimEnemy?[] bossClones = new SimEnemy?[M12sReplication2State.BossClones];
    private readonly SimEnemy?[] playerClones = new SimEnemy?[M12sReplication2State.PlayerClones];
    private readonly List<SimEnemy> helpers = [];
    private int nextHelper;
    private readonly List<SimTether> ownerTethers = [];
    private readonly List<(SimEnemy Source, CloneMechanic? Mechanic, SimTether Tether)> cloneTethers = [];
    // What each player took when the tethers locked, kept for the replay 40s later.
    private readonly Dictionary<SimCharacter, (SimEnemy Source, CloneMechanic? Mechanic)> taken = new();
    private readonly List<SimCharacter> scaldingTargets = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sReplication2State(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        Array.Clear(bossClones);
        Array.Clear(playerClones);
        helpers.Clear();
        nextHelper = 0;
        ownerTethers.Clear();
        cloneTethers.Clear();
        taken.Clear();
        scaldingTargets.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sReplication2State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new CircleArena(ArenaRadius), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(DnP2Waymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(StagingCastAt, () => boss?.Cast(ActionId.Staging, animationLock: 0.2f));
        for (var p = 0; p < M12sReplication2State.ClonePairs.Length; p++)
        {
            var pair = M12sReplication2State.ClonePairs[p];
            world.Events.Add(CloneRevealAt[p], () => { ShowPlayerClone(pair.A); ShowPlayerClone(pair.B); });
        }
        world.Events.Add(13.72f, () => Talk(0x18));
        world.Events.Add(OwnerTethersAt, TetherOwners);
        world.Events.Add(ReplicationCastAt, () => boss?.Cast(ActionId.Replication, animationLock: 0.2f));
        world.Events.Add(PlayerClonesHideAt, HidePlayerClones);
        world.Events.Add(24.9f, () => Talk(0x19));
        world.Events.Add(BossClonesShowAt, SpawnBossClones);
        world.Events.Add(TethersAt, TetherBossClones);
        world.Events.Add(TethersLockAt, LockTethers);
        world.Events.Add(FirefallCastAt, () => boss?.Cast(ActionId.FirefallSplashCast, animationLock: 0.2f));
        world.Events.Add(JumpAt, FirefallSplash);
        world.Events.Add(ScaldingAt, ScaldingWaves);
        world.Events.Add(ManaBurstVisualAt, ManaBurstVisuals);
        world.Events.Add(ManaBurstAt, ManaBurst);
        world.Events.Add(49.28f, () => HideBossClones(CloneMechanic.Defamation));
        world.Events.Add(HeavySlamAt, HeavySlam);
        world.Events.Add(HeavySlamAt, () => ForEachClone(CloneMechanic.Cone, clone => clone.Cast(ActionId.GrotesquerieVisual, animationLock: 0.2f)));
        world.Events.Add(SnakingKickCastAt, SnakingKick);
        world.Events.Add(GrotesquerieAt, () => ForEachHolder(CloneMechanic.Cone, holder => Helper(holder.Placement()).Cast(ActionId.Grotesquerie, holder)));
        world.Events.Add(ProjectionAt, HemorrhagicProjection);
        world.Events.Add(56.06f, () => HideBossClones(CloneMechanic.Cone));
        world.Events.Add(57.04f, () => HideBossClones(CloneMechanic.Stack));
        world.Events.Add(ReenactmentCastAt, () => boss?.Cast(ActionId.Reenactment, animationLock: 0.2f));
        world.Events.Add(PlayerClonesReturnAt, () => { for (var spot = 0; spot < playerClones.Length; spot++) ShowPlayerClone(spot); });
        world.Events.Add(NetherwrathCastAt, () => boss?.Cast(state.NetherwrathFar ? ActionId.NetherwrathFar : ActionId.NetherwrathNear, animationLock: 0.2f));
        for (var p = 0; p < M12sReplication2State.ClonePairs.Length; p++)
        {
            var pair = M12sReplication2State.ClonePairs[p];
            var at = ReplayAt[p];
            foreach (var spot in new[] { pair.A, pair.B })
            {
                world.Events.Add(at, () => ReplayFirefall(spot));
                world.Events.Add(at + ReplayDefamationAfter, () => ReplayDefamation(spot));
                world.Events.Add(at + ReplayMarkAfter, () => ReplayConeMark(spot));
                world.Events.Add(at + ReplayStackAfter, () => ReplayStack(spot));
                world.Events.Add(at + ReplayScaldingAfter, () => ReplayScalding(spot));
                world.Events.Add(at + ReplayConeAfter, () => ReplayCone(spot));
                world.Events.Add(at + ReplayConeAfter + ReplayCloneLingers, () => HidePlayerClone(spot));
            }
        }
        world.Events.Add(TimelessSpiteAt, TimelessSpite);
        world.Events.Add(82.98f, () => Talk(0x1A));
        world.Events.Add(DashAt, () =>
        {
            boss?.Cast(ActionId.Dash, Vector3.Zero, animationLock: 0.2f);
            boss?.SetPosition(new Placement(Vector3.Zero, MathF.PI));
        });
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        Towers.ResolveTower(world, resolved, ActionId.HeavySlam, Actions.UnmitigatedImpact, minSoakers: 1, delay: UnsoakedStackPunishedAfter);
        Towers.ResolveTower(world, resolved, ActionId.HeavySlamReplay, Actions.UnmitigatedImpact, minSoakers: 1, delay: UnsoakedStackPunishedAfter);
    }

    private void SpawnActors()
    {
        boss = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP2,
            NameId: BNpcNameId.LindwurmP2,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var i = 0; i < 16; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(
                    BNpcBaseId: BNpcBaseId.Helper,
                    NameId: BNpcNameId.LindwurmP2,
                    Level: 1,
                    EnemyList: EnemyListMode.Never,
                    Visibility: SpawnVisibility.InvisibleHelper)) is { } helper)
                helpers.Add(helper);
    }

    private SimEnemy Helper(Placement at)
    {
        var helper = helpers[nextHelper++ % helpers.Count];
        helper.SetPosition(at);
        return helper;
    }

    private void Talk(uint line) => world.Map.DirectorUpdate(0x80000027U, line, 0x2U, BattleTalk.Speaker, BattleTalk.NoActor);

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void ShowPlayerClone(int spot)
    {
        if (playerClones[spot] != null) return;
        var at = M12sReplication2State.PlayerClonePosition(spot);
        playerClones[spot] = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Understudy,
            NameId: BNpcNameId.Understudy,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Appearance: world.Party.Get(state.RoleAt(spot))?.Appearance,
            Placement: new Placement(at, 0f).Face(Vector3.Zero)));
    }

    private void HidePlayerClone(int spot)
    {
        playerClones[spot]?.Despawn();
        playerClones[spot] = null;
    }

    private void TetherOwners()
    {
        for (var spot = 0; spot < playerClones.Length; spot++)
            if (playerClones[spot] is { } clone && Alive(state.RoleAt(spot)) is { } owner)
                ownerTethers.Add(world.Tether(clone, owner, TetherId.Locked));
    }

    private void HidePlayerClones()
    {
        foreach (var tether in ownerTethers) tether.Despawn();
        ownerTethers.Clear();
        for (var spot = 0; spot < playerClones.Length; spot++) HidePlayerClone(spot);
    }

    private void SpawnBossClones()
    {
        for (var i = 0; i < bossClones.Length; i++)
        {
            var at = M12sReplication2State.BossClonePosition(i);
            bossClones[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Lindschrat,
                NameId: BNpcNameId.Lindschrat,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(at, 0f).Face(Vector3.Zero)));
        }
    }

    private static ushort TetherOf(CloneMechanic mechanic) => mechanic switch
    {
        CloneMechanic.Cone => TetherId.CloneCone,
        CloneMechanic.Defamation => TetherId.CloneDefamation,
        _ => TetherId.CloneStack,
    };

    private void TetherBossClones()
    {
        for (var i = 0; i < bossClones.Length; i++)
        {
            if (bossClones[i] is not { } clone) continue;
            var mechanic = state.CloneMechanics[i];
            var tether = world.Tether(clone, new TakenByWhoeverStandsOnIt(world.Party.Get(state.FirstHolders[i]), TetherReach), TetherOf(mechanic));
            cloneTethers.Add((clone, mechanic, tether));
        }
        if (boss is { } lindwurm)
            cloneTethers.Add((lindwurm, null,
                world.Tether(lindwurm, new TakenByWhoeverStandsOnIt(world.Party.Get(state.FirstHolders[^1]), TetherReach), TetherId.BossJump)));
    }

    private void LockTethers()
    {
        var locked = new List<(SimEnemy, CloneMechanic?, SimTether)>();
        foreach (var (source, mechanic, tether) in cloneTethers)
        {
            var holder = tether.B;
            tether.Despawn();
            if (holder is null || !holder.IsAlive()) continue;
            taken[holder] = (source, mechanic);
            locked.Add((source, mechanic, world.Tether(source, holder, TetherId.Locked)));
        }
        cloneTethers.Clear();
        cloneTethers.AddRange(locked);
    }

    private void ReleaseTethers(CloneMechanic? mechanic)
    {
        foreach (var (_, m, tether) in cloneTethers)
            if (m == mechanic) tether.Despawn();
    }

    private IEnumerable<SimCharacter> Holders(CloneMechanic? mechanic)
        => taken.Where(kv => kv.Value.Mechanic == mechanic && kv.Key.IsAlive()).Select(kv => kv.Key);

    private void ForEachHolder(CloneMechanic mechanic, Action<SimCharacter> act)
    {
        foreach (var holder in Holders(mechanic).ToList()) act(holder);
    }

    private void ForEachClone(CloneMechanic mechanic, Action<SimEnemy> act)
    {
        for (var i = 0; i < bossClones.Length; i++)
            if (state.CloneMechanics[i] == mechanic && bossClones[i] is { } clone) act(clone);
    }

    private void HideBossClones(CloneMechanic mechanic) => ForEachClone(mechanic, clone => clone.SetVisible(false));

    private void FirefallSplash()
    {
        ReleaseTethers(null);
        if (boss is not { } lindwurm || Holders(null).FirstOrDefault() is not { } holder) return;
        lindwurm.SetPosition(new Placement(holder.Position, lindwurm.Rotation));
        lindwurm.Cast(Actions.FirefallSplashJump, holder.Position);
    }

    private void ScaldingWaves()
    {
        if (boss is not { } lindwurm) return;
        var jumpedOn = Holders(null).FirstOrDefault();
        var targets = world.Party.ActiveMembers().Where(m => m != jumpedOn)
            .OrderBy(m => Vector3.DistanceSquared(m.Position, lindwurm.Position)).Take(4).ToList();
        scaldingTargets.AddRange(targets);
        foreach (var target in targets)
            Helper(lindwurm.Placement()).Cast(Actions.ScaldingWaves, target);
    }

    private void ManaBurstVisuals()
    {
        boss?.Cast(ActionId.ManaBurstVisual, animationLock: 0.2f);
        ForEachClone(CloneMechanic.Defamation, clone => clone.Cast(ActionId.ManaBurstVisual, animationLock: 0.2f));
    }

    // The defamation tethers' holders, and whoever took no tether at all.
    private void ManaBurst()
    {
        ReleaseTethers(CloneMechanic.Defamation);
        var targets = Holders(CloneMechanic.Defamation)
            .Concat(world.Party.ActiveMembers().Where(m => !taken.ContainsKey(m))).ToList();
        foreach (var target in targets)
            Helper(target.Placement()).Cast(Actions.ManaBurst, target);
    }

    private void HeavySlam()
    {
        ReleaseTethers(CloneMechanic.Stack);
        foreach (var (holder, (source, _)) in taken.Where(kv => kv.Value.Mechanic == CloneMechanic.Stack && kv.Key.IsAlive()).ToList())
            source.Cast(Actions.HeavySlam, holder.Position);
    }

    private void SnakingKick()
    {
        if (boss is not { } lindwurm) return;
        if (state.KickOrder.Select(Alive).FirstOrDefault(m => m != null) is { } target) lindwurm.Face(target);
        lindwurm.Cast(Actions.SnakingKick);
    }

    private void HemorrhagicProjection()
    {
        ReleaseTethers(CloneMechanic.Cone);
        ForEachHolder(CloneMechanic.Cone, holder => Helper(holder.Placement()).Cast(Actions.CloneHemorrhagicProjection));
    }

    private void TimelessSpite()
    {
        if (boss is not { } lindwurm) return;
        var byDistance = world.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, lindwurm.Position)).ToList();
        if (state.NetherwrathFar) byDistance.Reverse();
        foreach (var target in byDistance.Take(2))
            Helper(target.Placement()).Cast(Actions.TimelessSpite, target);
    }

    // A clone replays what its player took when the tethers locked; no tether replays a defamation.
    private (SimEnemy? Source, CloneMechanic? Mechanic, bool Boss) Replayed(int spot)
    {
        if (world.Party.Get(state.RoleAt(spot)) is { } owner && taken.TryGetValue(owner, out var took))
            return (took.Source, took.Mechanic, took.Mechanic == null);
        return (null, CloneMechanic.Defamation, false);
    }

    private static Placement FacingCenter(int spot) => new Placement(M12sReplication2State.PlayerClonePosition(spot), 0f).Face(Vector3.Zero);

    private void ReplayFirefall(int spot)
    {
        if (!Replayed(spot).Boss) return;
        var at = FacingCenter(spot);
        Helper(at).Cast(Actions.FirefallSplashReplay);
    }

    private void ReplayScalding(int spot)
    {
        if (!Replayed(spot).Boss) return;
        var at = FacingCenter(spot);
        foreach (var target in scaldingTargets.Where(t => t.IsAlive()))
            Helper(at).Cast(Actions.ScaldingWavesReplay, target);
    }

    private void ReplayDefamation(int spot)
    {
        if (Replayed(spot) is not { Boss: false, Mechanic: CloneMechanic.Defamation }) return;
        Helper(FacingCenter(spot)).Cast(Actions.ManaBurstReplay);
    }

    private void ReplayConeMark(int spot)
    {
        if (Replayed(spot).Mechanic != CloneMechanic.Cone || playerClones[spot] is not { } clone) return;
        Helper(FacingCenter(spot)).Cast(ActionId.Grotesquerie, clone, animationLock: 0.2f);
    }

    private void ReplayCone(int spot)
    {
        if (Replayed(spot).Mechanic != CloneMechanic.Cone) return;
        Helper(FacingCenter(spot)).Cast(Actions.HemorrhagicProjectionReplay);
    }

    private void ReplayStack(int spot)
    {
        if (Replayed(spot).Mechanic != CloneMechanic.Stack) return;
        Helper(FacingCenter(spot)).Cast(Actions.HeavySlamReplay);
    }

    // UNVERIFIED: retail passes these tethers by touch; here the one standing closest to the clone
    // (or the boss), within reach of it, takes its tether.
    private sealed class TakenByWhoeverStandsOnIt(SimCharacter? first, float reach) : DynamicEnd
    {
        public override SimCharacter? Resolve(SimCharacter? self, SimCharacter? other, TetherContext ctx)
        {
            if (self is null) return first ?? ctx.Party.RandomMember(ctx.Rng);
            if (other is null) return self;
            var closest = ctx.Party.Closest(other.Position);
            if (closest is null || closest == self) return self;
            var dx = closest.Position.X - other.Position.X;
            var dz = closest.Position.Z - other.Position.Z;
            return dx * dx + dz * dz <= reach * reach ? closest : self;
        }
    }
}
