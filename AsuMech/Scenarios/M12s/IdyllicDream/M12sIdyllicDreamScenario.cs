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
using State = AsuMech.Scenarios.M12s.IdyllicDream.M12sIdyllicDreamState;

namespace AsuMech.Scenarios.M12s.IdyllicDream;

// t=0 = P2 4:22, three seconds before Idyllic Dream. Player clones tether the party, Replication's three
// clones show the cleaves they will repeat, eight boss clones hand out defamation and stack tethers,
// the floor splits for towers and light spreads, the tethers resolve on the whole floor, the towers
// resolve on the platforms (wind, doom, fire, earth, then the portents), and the Reenactment replays
// the tethers from the player clones while the three clones repeat their cleaves from the platforms
// and the tear; until the second Idyllic Dream.
public sealed class M12sIdyllicDreamScenario : IScenario
{
    public string Name => "Idyllic Dream";
    public IPhase Phase => M12sZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sIdyllicDreamAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sIdyllicDreamSettingsWindow settingsWindow = new();

    public const float IdyllicDreamCastAt = 3.05f;
    public const float StagingCastAt = 11.25f;
    public static readonly float[] PlayerClonesShowAt = [15.53f, 17.54f];
    public const float PlayerClonesTetherAt = 19.58f;
    public const float PlayerClonesUntetherAt = 23.6f;
    public static readonly float[] TwistedVisionCastAt = [25.41f, 69.7f, 98.55f, 129.08f, 165.15f, 180.44f, 187.97f];
    public static readonly float[] ReplicationCastAt = [32.56f, 51.14f];
    public const float MemoryClonesShowAt = 38.7f;
    public const float RehearsalAt = 44.01f;
    public static readonly float[] BossClonesShowAt = [57.3f, 58.28f, 59.26f, 60.29f];
    public const float BossTethersAt = 65.55f;
    public const float TethersLockAt = 73.62f;
    public const float ReplayAt = 77.14f;
    public const float MeteorCastAt = 77.19f;
    public const float ReplayKickAt = 78.08f;
    public const float ReplayConesAt = 78.3f;
    public const float DownfallCastAt = 85.35f;
    public static readonly float[] SplitAt = [86.2f, 134.34f, 185.7f];
    public static readonly float[] RestoreAt = [103.77f, 170.36f, 193.19f];
    public const float TowersShowAt = 89.59f;
    public const float ArcanumCastAt = 91.51f;
    public const float ArcanumSpreadAt = 95.79f;
    public static readonly float[] SlotAt = [109.03f, 114.03f, 119.03f, 124.03f];
    public const float StackHitDelay = 0.03f;
    public const float DefaHitDelay = 1.29f;
    public const float CloneLeavesDelay = 4.06f;
    public const float LightDownAgainSeconds = 16f;
    public const float TowersResolveAt = 137.46f;
    public const float TowerStatusesAt = 138.04f;
    public const float DarkLinesAt = 138.08f;
    public const float WindLandsAt = 139.4f;
    public const float PortentsResolveAt = 148.03f;
    public const float TemporalCurtainCastAt = 153.51f;
    public const float IntoTheTearAt = 159.66f;
    public const float CloneJumpAt = 162.78f;
    public const float ReenactmentCastAt = 172.28f;
    public static readonly float[] ReenactAt = [177.36f, 198.31f];
    public const float ReenactedManaBurstDelay = 1.3f;
    public const float ReenactedHeavySlamDelay = 1.52f;
    public const float PlatformReplayAt = 187.93f;
    public const float PlatformKickAt = 188.87f;
    public const float PlatformConesAt = 189.09f;
    public const float TearOpensAt = 196.06f;
    public const float TearCloneCastAt = 200.45f;
    public const float TearConesAt = 204.59f;
    private const float WindAirborne = 1.6f;
    // UNVERIFIED: how far a Hot-blooded player may drift before Pyretic Wurm.
    private const float HotBloodedDrift = 1f;
    private const float TearZ = -15f;

    private SimWorld world = null!;
    private State state = null!;
    private DreamArena arena = null!;
    private SimEnemy? lindwurm;
    private readonly SimEnemy?[] playerClones = new SimEnemy?[State.Directions];
    private readonly SimEnemy?[] bossClones = new SimEnemy?[State.Directions];
    private SimEnemy? kickClone;
    private SimEnemy? eastWestClone;
    private SimEnemy? northSouthClone;
    private SimEnemy? tear;
    private readonly List<SimEnemy?> helpers = [];
    private int nextHelper;
    private readonly List<SimTether> cloneTethers = [];
    private readonly SimTether?[] bossTethers = new SimTether?[State.Directions];
    // Who held each boss clone's tether when it locked: the Reenactment replays it from their clone.
    private readonly SimCharacter?[] lockedHolders = new SimCharacter?[State.Directions];
    private readonly Dictionary<SimCharacter, Vector3> hotBlooded = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new State(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        arena = new DreamArena(() => world.Events.Elapsed);
        cloneTethers.Clear();
        Array.Clear(bossTethers);
        Array.Clear(lockedHolders);
        hotBlooded.Clear();
        helpers.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(arena, "Fell off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(DnP2Waymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(0.5f, TankTheBoss);
        world.Events.Add(IdyllicDreamCastAt, () => lindwurm?.Cast(Actions.IdyllicDream));
        world.Events.Add(9.29f, () => world.Map.AddEffect(MapEffect.DreamBegins, MapEffect.DreamSlot));
        world.Events.Add(StagingCastAt, () => lindwurm?.Cast(ActionId.Staging, animationLock: 2.1f));
        world.Events.Add(PlayerClonesShowAt[0], () => ShowPlayerClones(state.CardinalClonesFirst));
        world.Events.Add(PlayerClonesShowAt[1], () => ShowPlayerClones(!state.CardinalClonesFirst));
        world.Events.Add(PlayerClonesTetherAt, TetherPlayersToClones);
        world.Events.Add(PlayerClonesUntetherAt, UntetherPlayerClones);
        world.Events.Add(25.32f, () => world.Map.AddEffect(MapEffect.VisionStarts, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[0], TwistedVision);
        world.Events.Add(30.65f, () => world.Map.AddEffect(MapEffect.VisionShows, MapEffect.DreamSlot));
        world.Events.Add(ReplicationCastAt[0], () => lindwurm?.Cast(ActionId.Replication, animationLock: 2.1f));
        world.Events.Add(MemoryClonesShowAt, ShowMemoryClones);
        world.Events.Add(43.92f, () => world.Map.AddEffect(MapEffect.VisionEnds, MapEffect.DreamSlot));
        world.Events.Add(RehearsalAt, Rehearse);
        world.Events.Add(49.23f, () => world.Map.AddEffect(MapEffect.VisionCleared, MapEffect.DreamSlot));
        world.Events.Add(ReplicationCastAt[1], () => lindwurm?.Cast(ActionId.Replication, animationLock: 2.1f));
        for (var p = 0; p < BossClonesShowAt.Length; p++)
        {
            var pair = p;
            world.Events.Add(BossClonesShowAt[pair], () => ShowBossClones(pair));
        }
        world.Events.Add(BossTethersAt, TetherBossClones);
        world.Events.Add(69.61f, () => world.Map.AddEffect(MapEffect.VisionStarts, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[1], TwistedVision);
        world.Events.Add(TethersLockAt, LockTethers);
        world.Events.Add(74.92f, () => world.Map.AddEffect(MapEffect.VisionShows, MapEffect.DreamSlot));
        world.Events.Add(77.1f, () => world.Map.AddEffect(MapEffect.MeteorFalls, MapEffect.FloorSlot));
        world.Events.Add(ReplayAt, ReplayVisuals);
        world.Events.Add(MeteorCastAt, () => lindwurm?.Cast(Actions.LindwurmsMeteor));
        world.Events.Add(ReplayKickAt, () => Fire(Actions.SnakingKickReplay, state.KickClone, 0f));
        world.Events.Add(ReplayConesAt, () =>
        {
            FireCones(state.EastWestConesClone, true);
            FireCones(state.NorthSouthConesClone, false);
        });
        world.Events.Add(DownfallCastAt, () => lindwurm?.Cast(ActionId.Downfall, animationLock: 2.1f));
        world.Events.Add(SplitAt[0], () => SplitFloor(MapEffect.FloorSplits));
        world.Events.Add(TowersShowAt, ShowTowers);
        world.Events.Add(ArcanumCastAt, () => lindwurm?.Cast(ActionId.ArcadianArcanum, animationLock: 2.1f));
        world.Events.Add(ArcanumSpreadAt, LightSpreads);
        world.Events.Add(98.46f, () => world.Map.AddEffect(MapEffect.VisionEnds, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[2], TwistedVision);
        world.Events.Add(RestoreAt[0], RestoreFloor);
        world.Events.Add(RestoreAt[0], HideTowers);
        for (var s = 0; s < SlotAt.Length; s++)
        {
            var slot = s;
            world.Events.Add(SlotAt[slot], () => MarkSlot(slot));
            world.Events.Add(SlotAt[slot] + StackHitDelay, () => ResolveSlot(slot, defa: false));
            world.Events.Add(SlotAt[slot] + DefaHitDelay, () => ResolveSlot(slot, defa: true));
            world.Events.Add(SlotAt[slot] + CloneLeavesDelay, () => HideSlotClones(slot));
        }
        world.Events.Add(128.99f, () => world.Map.AddEffect(MapEffect.VisionStarts, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[3], TwistedVision);
        world.Events.Add(SplitAt[1], () => SplitFloor(MapEffect.FloorSplitsAgain));
        world.Events.Add(SplitAt[1], ShowTowers);
        world.Events.Add(SplitAt[1], LightDownAgain);
        world.Events.Add(TowersResolveAt, SoakTowers);
        world.Events.Add(TowersResolveAt, HideTowers);
        world.Events.Add(TemporalCurtainCastAt, () => lindwurm?.Cast(ActionId.TemporalCurtain, animationLock: 2.1f));
        world.Events.Add(IntoTheTearAt, CloneIntoTheTear);
        world.Events.Add(CloneJumpAt, ClonesJumpToPlatforms);
        world.Events.Add(165.06f, () => world.Map.AddEffect(MapEffect.VisionEnds, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[4], TwistedVision);
        world.Events.Add(TwistedVisionCastAt[4], PlatformRehearsal);
        world.Events.Add(RestoreAt[1], RestoreFloor);
        world.Events.Add(ReenactmentCastAt, () => lindwurm?.Cast(ActionId.Reenactment, animationLock: 2.1f));
        for (var s = 0; s < ReenactAt.Length; s++)
        {
            var set = s;
            world.Events.Add(ReenactAt[set] - 0.09f, () => BringBackBossClones(set));
            world.Events.Add(ReenactAt[set], () => MarkReenactment(set));
            world.Events.Add(ReenactAt[set] + ReenactedManaBurstDelay, () => Reenact(set, defa: true));
            world.Events.Add(ReenactAt[set] + ReenactedHeavySlamDelay, () => Reenact(set, defa: false));
        }
        world.Events.Add(180.35f, () => world.Map.AddEffect(MapEffect.VisionStarts, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[5], TwistedVision);
        world.Events.Add(SplitAt[2], () => SplitFloor(MapEffect.FloorSplitsAgain));
        world.Events.Add(187.88f, () => world.Map.AddEffect(MapEffect.VisionEnds, MapEffect.DreamSlot));
        world.Events.Add(TwistedVisionCastAt[6], TwistedVision);
        world.Events.Add(PlatformReplayAt, PlatformReplayVisuals);
        world.Events.Add(PlatformKickAt, () => Fire(Actions.SnakingKickReplay, state.PlatformKick, 0f));
        world.Events.Add(PlatformConesAt, () => FireCones(state.PlatformCone, state.PlatformConesEastWest));
        world.Events.Add(RestoreAt[2], RestoreFloor);
        world.Events.Add(TearOpensAt, OpenTheTear);
        world.Events.Add(TearCloneCastAt, TearCloneCasts);
        world.Events.Add(TearConesAt, () => FireCones(TearClonePosition, state.TearTakesEastWestClone));
    }

    public void Tick(float delta, float elapsed) => BurnTheHotBlooded();

    public void OnStatusAdded(StatusAdded added)
    {
        if (added.StatusId == StatusId.HotBlooded) hotBlooded[added.Holder] = added.Holder.Position;
    }

    // Doom runs out unless an Esuna takes it off first.
    public void OnStatusLost(StatusLost lost)
    {
        if (lost.StatusId == StatusId.HotBlooded) hotBlooded.Remove(lost.Holder);
        if (lost is { StatusId: StatusId.Doom, Reason: StatusLossReason.Expired }) lost.Holder.Die(ActionId.LindwurmsDarkII, "Doom ran out: needs an Esuna");
        if (lost is { StatusId: StatusId.NearbyPortent or StatusId.FarawayPortent, Reason: StatusLossReason.Expired } && lost.Holder.IsAlive())
            Portent(lost.Holder, faraway: lost.StatusId == StatusId.FarawayPortent);
    }

    private static Vector3 TearClonePosition => new(0f, 0f, State.TearCloneZ);

    private void SpawnActors()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP2,
            NameId: BNpcNameId.LindwurmP2,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var d = 0; d < State.Directions; d++)
        {
            var spot = State.PlayerCloneSpot(d);
            playerClones[d] = SpawnHidden(BNpcBaseId.Understudy, BNpcNameId.Understudy, new Placement(spot, Bearing(spot, Vector3.Zero)));
            var bossSpot = State.BossCloneSpot(d);
            bossClones[d] = SpawnHidden(BNpcBaseId.Lindschrat, BNpcNameId.Lindschrat, new Placement(bossSpot, Bearing(bossSpot, Vector3.Zero)));
        }
        kickClone = SpawnHidden(BNpcBaseId.Lindschrat, BNpcNameId.Lindschrat, new Placement(state.KickClone, 0f));
        eastWestClone = SpawnHidden(BNpcBaseId.Lindschrat, BNpcNameId.Lindschrat, new Placement(state.EastWestConesClone, 0f));
        northSouthClone = SpawnHidden(BNpcBaseId.Lindschrat, BNpcNameId.Lindschrat, new Placement(state.NorthSouthConesClone, 0f));
        tear = SpawnHidden(BNpcBaseId.TemporalTear, BNpcNameId.TemporalTear, new Placement(new Vector3(0f, 0f, TearZ), 0f));
        for (var i = 0; i < 24; i++)
            helpers.Add(world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Helper,
                NameId: BNpcNameId.LindwurmP2,
                Level: 1,
                EnemyList: EnemyListMode.Never,
                Visibility: SpawnVisibility.InvisibleHelper)));
    }

    private SimEnemy? SpawnHidden(uint baseId, uint nameId, Placement at)
    {
        var enemy = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: at));
        enemy?.SetVisible(false);
        return enemy;
    }

    private static float Bearing(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private void TankTheBoss() => lindwurm?.SetTarget(Alive(PartyRole.MainTank), follow: false);

    private void TwistedVision() => lindwurm?.Cast(ActionId.TwistedVision, animationLock: 2.1f);

    private void Fire(EnemyAction action, Vector3 at, float heading, CastTarget target = default)
    {
        if (helpers.Count == 0) return;
        var helper = helpers[nextHelper];
        nextHelper = (nextHelper + 1) % helpers.Count;
        helper?.SetPosition(new Placement(at, heading));
        helper?.Cast(action, target);
    }

    private void FireCones(Vector3 clone, bool eastWest)
    {
        foreach (var heading in State.ConeHeadings(eastWest)) Fire(Actions.PowerGusherReplay, clone, heading);
    }

    private void ShowPlayerClones(bool cardinals)
    {
        for (var d = 0; d < State.Directions; d++)
            if (State.IsCardinal(d) == cardinals) playerClones[d]?.SetVisible(true);
    }

    private void TetherPlayersToClones()
    {
        foreach (var (role, direction) in state.CloneDirection)
            if (Alive(role) is { } member && playerClones[direction] is { } clone)
                cloneTethers.Add(world.Tether(clone, member, TetherId.Bound));
    }

    private void UntetherPlayerClones()
    {
        foreach (var tether in cloneTethers) tether.Despawn();
        cloneTethers.Clear();
    }

    private void ShowMemoryClones()
    {
        kickClone?.SetVisible(true);
        eastWestClone?.SetVisible(true);
        northSouthClone?.SetVisible(true);
    }

    private SimEnemy? NorthMemoryClone => state.EastWestConesZ < 0f ? eastWestClone : northSouthClone;

    private SimEnemy? SouthMemoryClone => state.EastWestConesZ < 0f ? northSouthClone : eastWestClone;

    private void Rehearse()
    {
        kickClone?.Cast(Actions.SnakingKickRehearsal);
        NorthMemoryClone?.Cast(ActionId.PowerGusherNorthClone, animationLock: 0.6f);
        SouthMemoryClone?.Cast(ActionId.PowerGusherSouthClone, animationLock: 0.6f);
        foreach (var heading in State.ConeHeadings(true)) Fire(Actions.PowerGusherRehearsal, state.EastWestConesClone, heading);
        foreach (var heading in State.ConeHeadings(false)) Fire(Actions.PowerGusherRehearsal, state.NorthSouthConesClone, heading);
    }

    private void ShowBossClones(int pair)
    {
        bossClones[pair]?.SetVisible(true);
        bossClones[pair + 4]?.SetVisible(true);
    }

    private void TetherBossClones()
    {
        for (var d = 0; d < State.Directions; d++)
        {
            if (bossClones[d] is not { } clone) continue;
            var tetherId = state.IsDefaClone(d) ? TetherId.ManaBurst : TetherId.HeavySlam;
            bossTethers[d] = world.Tether(clone, new BeamGrab(Alive(state.FirstHolders[d])), tetherId);
        }
    }

    // Twisted Vision's end snapshots each tether on whoever holds it.
    private void LockTethers()
    {
        for (var d = 0; d < State.Directions; d++)
        {
            if (bossTethers[d] is not { } grab) continue;
            var holder = grab.B;
            grab.Despawn();
            bossTethers[d] = null;
            if (!holder.IsAlive() || bossClones[d] is not { } clone) continue;
            lockedHolders[d] = holder;
            bossTethers[d] = world.Tether(clone, holder, TetherId.Bound);
        }
    }

    private void ReplayVisuals()
    {
        kickClone?.Cast(ActionId.SnakingKickReplayVisual, animationLock: 0.6f);
        NorthMemoryClone?.Cast(ActionId.PowerGusherNorthReplay, animationLock: 0.6f);
        SouthMemoryClone?.Cast(ActionId.PowerGusherSouthReplay, animationLock: 0.6f);
    }

    private void SplitFloor(uint effect)
    {
        world.Map.AddEffect(effect, MapEffect.FloorSlot);
        world.Map.AddEffect(MapEffect.MiddleGone, MapEffect.MiddleFloorSlot);
        world.Map.AddEffect(MapEffect.VisionShows, MapEffect.DreamSlot);
        arena.Split();
    }

    private void RestoreFloor()
    {
        world.Map.AddEffect(MapEffect.FloorRestored, MapEffect.FloorSlot);
        world.Map.AddEffect(MapEffect.MiddleShows, MapEffect.MiddleFloorSlot);
        world.Map.AddEffect(MapEffect.VisionCleared, MapEffect.DreamSlot);
        arena.Restore();
    }

    // UNVERIFIED: which floor slot is which tower, and how retail shows a tower's kind; the kinds go to chat.
    private void ShowTowers()
    {
        for (byte i = 0; i < 8; i++)
        {
            world.Map.AddEffect(MapEffect.TowerShows, (byte)(MapEffect.FirstTowerSlot + i));
            world.Map.AddEffect(MapEffect.TowerLit, (byte)(MapEffect.FirstTowerSlot + i));
        }
        world.Announce(TowerCallout(-1) + " | " + TowerCallout(1));
    }

    private string TowerCallout(int side)
    {
        var name = side < 0 ? "West" : "East";
        var towers = state.Towers.Where(t => t.Side == side)
            .OrderBy(t => t.ZSign).ThenBy(t => t.Centre.X)
            .Select(t => $"{(t.ZSign < 0 ? "N" : "S")}{(t.Inner == side > 0 ? "W" : "E")} {t.Kind}");
        return $"{name}: {string.Join(", ", towers)}";
    }

    private void HideTowers()
    {
        for (byte i = 0; i < 8; i++) world.Map.AddEffect(MapEffect.TowerOff, (byte)(MapEffect.FirstTowerSlot + i));
    }

    private void LightSpreads()
    {
        foreach (var role in Enum.GetValues<PartyRole>().Where(state.IsSpreadRole))
            if (Alive(role) is { } member)
                Fire(Actions.ArcanumSpread, member.Position, 0f, member);
    }

    private void LightDownAgain()
    {
        foreach (var role in Enum.GetValues<PartyRole>().Where(state.IsSpreadRole))
            Alive(role)?.AddStatus(StatusId.LightResistanceDownII, LightDownAgainSeconds);
    }

    private IEnumerable<int> SlotClones(int slot) => [slot, slot + 4];

    private bool SlotIsDefa(int slot) => state.IsDefaClone(slot);

    private void MarkSlot(int slot)
    {
        if (!SlotIsDefa(slot)) return;
        foreach (var d in SlotClones(slot)) bossClones[d]?.Cast(ActionId.ManaBurstMarker, animationLock: 0.6f);
    }

    private void ResolveSlot(int slot, bool defa)
    {
        if (SlotIsDefa(slot) != defa) return;
        foreach (var d in SlotClones(slot))
        {
            bossTethers[d]?.Despawn();
            bossTethers[d] = null;
            if (lockedHolders[d] is not { } holder || !holder.IsAlive()) continue;
            Fire(defa ? Actions.IdyllicManaBurst : Actions.IdyllicHeavySlam, holder.Position, 0f, holder);
        }
    }

    private void HideSlotClones(int slot)
    {
        foreach (var d in SlotClones(slot)) bossClones[d]?.SetVisible(false);
    }

    // Whoever soaked each tower carries its mark once the towers go off.
    private void SoakTowers()
    {
        var members = AliveMembers();
        var soakers = new List<(DreamTower Tower, SimCharacter Soaker)>();
        foreach (var tower in state.Towers)
        {
            var inside = members.Where(m => Vector3.Distance(m.Position, tower.Centre) <= State.TowerRadius).ToList();
            if (inside.Count == 0)
            {
                Fire(Actions.IdyllicUnmitigatedExplosion, tower.Centre, 0f);
                continue;
            }
            Fire(tower.Kind switch
            {
                TowerKind.Wind => Actions.CosmicKissWind,
                TowerKind.Doom => Actions.CosmicKissTriple,
                _ => Actions.CosmicKiss,
            }, tower.Centre, 0f, tower.Centre);
            if (inside.Count == 1) soakers.Add((tower, inside[0]));
        }
        arena.AirborneUntil(TowersResolveAt + Actions.CosmicKissWind.Timing.DamageDelay + WindAirborne);
        world.Events.Add(TowerStatusesAt - TowersResolveAt, () => TowerAftermath(soakers));
        world.Events.Add(DarkLinesAt - TowersResolveAt, () => DarkLines(soakers));
    }

    private void TowerAftermath(List<(DreamTower Tower, SimCharacter Soaker)> soakers)
    {
        foreach (var tower in state.Towers.Where(t => t.Kind == TowerKind.Earth))
            Fire(Actions.StoneIII, tower.Centre, 0f, tower.Centre);
        foreach (var (tower, soaker) in soakers.Where(s => s.Soaker.IsAlive()))
        {
            switch (tower.Kind)
            {
                case TowerKind.Fire:
                    soaker.AddStatus(StatusId.HotBlooded, Actions.HotBloodedSeconds);
                    break;
                case TowerKind.Doom:
                    soaker.AddStatus(StatusId.LindwurmsPortent, Actions.PortentSeconds);
                    soaker.AddStatus(StatusId.NearbyPortent, Actions.PortentSeconds);
                    break;
                case TowerKind.Wind:
                    soaker.AddStatus(StatusId.LindwurmsPortent, Actions.PortentSeconds);
                    soaker.AddStatus(StatusId.FarawayPortent, Actions.PortentSeconds);
                    break;
            }
        }
    }

    private void DarkLines(List<(DreamTower Tower, SimCharacter Soaker)> soakers)
    {
        foreach (var (tower, soaker) in soakers.Where(s => s.Tower.Kind == TowerKind.Doom && s.Soaker.IsAlive()))
            Fire(Actions.DarkII, tower.Centre, Bearing(tower.Centre, soaker.Position), soaker);
    }

    private void BurnTheHotBlooded()
    {
        foreach (var (member, from) in hotBlooded.ToList())
        {
            if (!member.IsAlive()) { hotBlooded.Remove(member); continue; }
            if (Vector2.Distance(new Vector2(member.Position.X, member.Position.Z), new Vector2(from.X, from.Z)) <= HotBloodedDrift) continue;
            hotBlooded.Remove(member);
            member.Die(ActionId.PyreticWurm, "moved while Hot-blooded");
        }
    }

    // A portent runs out into a Glare on its holder and a Thunder II at the nearest or farthest other player.
    private void Portent(SimCharacter holder, bool faraway)
    {
        Fire(Actions.Glare, holder.Position, 0f, holder);
        var others = AliveMembers().Where(m => !ReferenceEquals(m, holder)).OrderBy(m => Vector3.Distance(m.Position, holder.Position)).ToList();
        if (others.Count == 0) return;
        var aim = faraway ? others[^1] : others[0];
        Fire(Actions.ThunderII, holder.Position, Bearing(holder.Position, aim.Position), aim);
    }

    private SimEnemy? TearClone => state.TearTakesEastWestClone ? eastWestClone : northSouthClone;

    private SimEnemy? PlatformConeClone => state.TearTakesEastWestClone ? northSouthClone : eastWestClone;

    private void CloneIntoTheTear()
    {
        TearClone?.Cast(ActionId.IntoTheTear, animationLock: 0.6f);
        world.Events.Add(1f, () => TearClone?.SetVisible(false));
    }

    private void ClonesJumpToPlatforms()
    {
        kickClone?.Cast(ActionId.CloneJump, animationLock: 0.6f);
        PlatformConeClone?.Cast(ActionId.CloneJump, animationLock: 0.6f);
        kickClone?.SetPosition(new Placement(state.PlatformKick, 0f));
        PlatformConeClone?.SetPosition(new Placement(state.PlatformCone, 0f));
    }

    // Only cast bars this time: the omens were Replication's.
    private void PlatformRehearsal()
    {
        kickClone?.Cast(ActionId.SnakingKickPlatform, animationLock: 0.6f);
        PlatformConeClone?.Cast(ActionId.PowerGusherSouthClone, animationLock: 0.6f);
    }

    private void PlatformReplayVisuals()
    {
        kickClone?.Cast(ActionId.SnakingKickReplayVisual, animationLock: 0.6f);
        PlatformConeClone?.Cast(ActionId.PowerGusherSouthReplay, animationLock: 0.6f);
    }

    private IEnumerable<int> ReenactedSet(int set) => set == 0 ? state.ReenactedFirst : Enumerable.Range(0, State.Directions).Except(state.ReenactedFirst);

    // What each player clone replays: the tethers its player held when they locked.
    private IEnumerable<bool> HeldDefas(int direction)
    {
        var owner = world.Party.Get(state.CloneOwner(direction));
        for (var d = 0; d < State.Directions; d++)
            if (owner != null && ReferenceEquals(lockedHolders[d], owner)) yield return state.IsDefaClone(d);
    }

    private void BringBackBossClones(int set)
    {
        foreach (var d in ReenactedSet(set)) bossClones[d]?.SetVisible(true);
    }

    private void MarkReenactment(int set)
    {
        foreach (var d in ReenactedSet(set))
            foreach (var defa in HeldDefas(d).Distinct())
            {
                if (defa) bossClones[d]?.Cast(ActionId.ReenactedManaBurstMarker, animationLock: 0.6f);
                else bossClones[d]?.Cast(ActionId.ReenactedHeavySlamMarker, playerClones[d], animationLock: 0.6f);
            }
        if (set == 1) TearClone?.Cast(ActionId.OutOfTheTear, animationLock: 0.6f);
    }

    private void Reenact(int set, bool defa)
    {
        foreach (var d in ReenactedSet(set))
        {
            foreach (var held in HeldDefas(d).Where(h => h == defa))
                Fire(defa ? Actions.ReenactedManaBurst : Actions.ReenactedHeavySlam, State.PlayerCloneSpot(d), 0f, State.PlayerCloneSpot(d));
            if (!defa) world.Events.Add(1f, () => bossClones[d]?.SetVisible(false));
        }
    }

    private void OpenTheTear()
    {
        tear?.SetVisible(true);
        TearClone?.SetPosition(new Placement(new Vector3(0f, 0f, TearZ), 0f));
        TearClone?.SetVisible(true);
    }

    private void TearCloneCasts()
    {
        TearClone?.SetPosition(new Placement(TearClonePosition, 0f));
        TearClone?.Cast(ActionId.PowerGusherFromTheTear, animationLock: 0.6f);
    }

    // Runs like UMAD's passable tethers, but a player may hold more than one: whoever steps into the
    // beam ahead of its holder takes it over.
    private sealed class BeamGrab(SimCharacter? first) : DynamicEnd
    {
        private const float HalfWidth = 0.8f;
        private const float MinForward = 0.1f;

        public override SimCharacter? Resolve(SimCharacter? self, SimCharacter? other, TetherContext ctx)
        {
            if (self is null) return first ?? ctx.Party.RandomMember(ctx.Rng);
            if (!self.IsAlive() || other is null) return self;
            var from = self.Position;
            var dx = other.Position.X - from.X;
            var dz = other.Position.Z - from.Z;
            var length = MathF.Sqrt(dx * dx + dz * dz);
            if (length < 0.01f) return self;
            SimCharacter? best = null;
            var bestDistance = float.MaxValue;
            foreach (var c in ctx.Party.InsideRect(new Placement(from, MathF.Atan2(dx, dz)), HalfWidth, length))
            {
                if (ReferenceEquals(c, self) || !c.IsAlive()) continue;
                var ox = c.Position.X - from.X;
                var oz = c.Position.Z - from.Z;
                if ((ox * dx + oz * dz) / length < MinForward) continue;
                var distance = ox * ox + oz * oz;
                if (distance < bestDistance) { bestDistance = distance; best = c; }
            }
            return best ?? self;
        }
    }

    // The round floor, or the two platforms while the vision splits it. While a wind tower throws its
    // soaker across the gap only the outer wall counts.
    private sealed class DreamArena(Func<float> now) : IArenaShape
    {
        private bool split;
        private float airborneUntil;

        public void Split() => split = true;
        public void Restore() => split = false;
        public void AirborneUntil(float t) => airborneUntil = t;

        public bool IsOutside(Vector3 p)
        {
            var outsideCircle = p.X * p.X + p.Z * p.Z > State.ArenaRadius * State.ArenaRadius;
            if (!split) return outsideCircle;
            if (now() < airborneUntil) return outsideCircle && !State.OnPlatform(p);
            return !State.OnPlatform(p);
        }
    }
}
