using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.Act1;

// Grotesquerie: Act 1 to the second Fixer: spread or stack debuffs and an arrow on everyone,
// four chasing Phagocyte Spotlights, Ravenous Reach's cone from the dragon head, the debuffs going
// off, then the blood vessels' Bursts with tank busters and a healer stack.
// t=0 = P1 0:54 (Grotesquerie: Act 1 starts at 0:56.8).
public sealed class M12sAct1Scenario : IScenario
{
    public string Name => "Grotesquerie: Act 1";
    public IPhase Phase => M12sZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sAct1Ai()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sAct1SettingsWindow settingsWindow = new();

    public static readonly Vector3 BossSpot = new(0f, 0f, -15f);
    private static readonly Vector3 HeadSpot = new(10f, 0f, -15f);
    private const float ReachOriginX = 10f;

    public const float Act1CastAt = 2.77f;
    public const float DebuffsAt = 6.91f;
    public const float DebuffSeconds = 17f;
    public const float VesselsSpawnAt = 11.88f;
    public const float RavenousReachAt = 11.99f;
    public const float ConeCastAt = 13.05f;
    public static readonly float[] SpotlightAt = [11.99f, 13.99f, 15.95f, 17.95f];
    public const float SpotlightResolves = 2.98f;
    public const float ConeHitsAt = 23.65f;
    public const float GrotesquerieGoesOffAt = 23.9f;
    public const float VesselsSwellAt = 26.05f;
    public const float MarkersAt = 28.06f;
    public const float BurstCastAt = 30.15f;
    public const float BurstHitsAt = 32.65f;
    public const float BustersAt = 33.17f;
    public const float FixerCastAt = 38.25f;

    private SimWorld world = null!;
    private M12sAct1State state = null!;
    private SimEnemy? lindwurm;
    private SimEnemy? head;
    private SimEnemy? coneHelper;
    private readonly SimEnemy?[] helpers = new SimEnemy?[16];
    private readonly List<SimEventObject> vessels = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sAct1State(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sAct1State>)AiStrats[idx]).Run(state, world);

        // The party walks in from the entrance, south of the arena.
        world.Events.Add(3f, () => world.EnforceArenaBoundary(new RectArena(Geometry.P1HalfWidth, Geometry.P1HalfDepth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, TankTheBoss);
        world.Events.Add(2.68f, () => lindwurm?.SetModeAttributeFlags(0xF9));
        world.Events.Add(Act1CastAt, () => lindwurm?.Cast(ActionId.GrotesquerieAct1, animationLock: 0.2f));
        world.Events.Add(DebuffsAt, HandOutGrotesquerie);
        world.Events.Add(VesselsSpawnAt, SpawnVessels);
        world.Events.Add(RavenousReachAt, () => lindwurm?.Cast(ActionId.RavenousReach, animationLock: 0.2f));
        for (var w = 0; w < SpotlightAt.Length; w++)
        {
            var wave = w;
            world.Events.Add(SpotlightAt[wave], () => Spotlight(wave));
        }
        world.Events.Add(ConeCastAt, ReachFromTheHead);
        world.Events.Add(15.46f, () => lindwurm?.SetModelState(4));
        world.Events.Add(23.12f, () => head?.Cast(ActionId.RavenousReachHeadStrike, animationLock: 0.2f));
        world.Events.Add(GrotesquerieGoesOffAt, GrotesquerieGoesOff);
        world.Events.Add(23.92f, () => lindwurm?.SetModelState(0));
        world.Events.Add(26f, () => head?.Despawn());
        world.Events.Add(VesselsSwellAt, () => Beat(0x10, 0x20));
        world.Events.Add(MarkersAt, MarkBustersAndStack);
        world.Events.Add(30.06f, () => Beat(0x1, 0x2));
        world.Events.Add(BurstCastAt, () => lindwurm?.SetModeAttributeFlags(0xFC));
        world.Events.Add(BurstCastAt, VesselsBurst);
        world.Events.Add(32.64f, () => Beat(0x4, 0x8));
        world.Events.Add(BustersAt, BustersAndStack);
        world.Events.Add(35f, ClearVessels);
        world.Events.Add(FixerCastAt, () => lindwurm?.Cast(Actions.TheFixer));
    }

    public void Tick(float delta, float elapsed)
    {
    }

    private void SpawnActors()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP1,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(BossSpot, 0f)));
        head = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.SnakeHead,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(HeadSpot, 0f)));
        coneHelper = SpawnHelper();
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.LindwurmP1,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private IEnumerable<(PartyRole Role, SimCharacter Member)> AliveMembers()
        => Enum.GetValues<PartyRole>().Select(r => (r, Alive(r))).Where(p => p.Item2 != null).Select(p => (p.r, p.Item2!));

    private void TankTheBoss() => lindwurm?.SetTarget(Alive(PartyRole.MainTank), follow: false);

    private void HandOutGrotesquerie()
    {
        foreach (var (role, member) in AliveMembers())
        {
            if (state.Spreads(role)) member.AddStatus(StatusId.BurstingGrotesquerie, DebuffSeconds);
            else if (role == state.SharedHolder) member.AddStatus(StatusId.SharedGrotesquerie, DebuffSeconds);
            member.AddStatus(StatusId.DirectedGrotesquerie, DebuffSeconds);
            member.AddStatusParam(StatusId.DirectedArrow, M12sAct1State.ArrowParam(state.Arrows[role]), DebuffSeconds);
        }
    }

    private void SpawnVessels()
    {
        foreach (var at in M12sAct1State.VesselLayouts[state.VesselLayout])
            if (world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.BloodVessel, Placement = new Placement(at, 0f), TimelineState = 1 }) is { } vessel)
                vessels.Add(vessel);
    }

    private void Beat(uint beat, uint bitmask)
    {
        foreach (var vessel in vessels) vessel.PlayBeat(beat, bitmask);
    }

    private void ClearVessels()
    {
        foreach (var vessel in vessels) vessel.Despawn();
        vessels.Clear();
    }

    private void Spotlight(int wave)
    {
        var i = (wave % 2) * 8;
        foreach (var (_, member) in AliveMembers())
        {
            var helper = helpers[i++];
            helper?.SetPosition(new Placement(member.Position, 0f));
            helper?.Cast(Actions.PhagocyteSpotlight, member.Position);
        }
    }

    private void ReachFromTheHead()
    {
        var fromWest = !state.EastSafe;
        head?.Cast(fromWest ? ActionId.RavenousReachFromWest : ActionId.RavenousReachFromEast, animationLock: 0.2f);
        var origin = new Vector3(fromWest ? -ReachOriginX : ReachOriginX, 0f, 0f);
        coneHelper?.SetPosition(new Placement(origin, fromWest ? MathF.PI / 2f : -MathF.PI / 2f));
        coneHelper?.Cast(Actions.RavenousReachCone);
    }

    private void GrotesquerieGoesOff()
    {
        var i = 0;
        foreach (var (role, member) in AliveMembers())
        {
            var helper = helpers[i++];
            helper?.SetPosition(new Placement(member.Position, member.Rotation + M12sAct1State.ArrowTurn(state.Arrows[role])));
            helper?.Cast(Actions.HemorrhagicProjection);
        }
        foreach (var (role, member) in AliveMembers().Where(m => state.Spreads(m.Role) || m.Role == state.SharedHolder))
        {
            var helper = helpers[i++];
            helper?.SetPosition(new Placement(member.Position, 0f));
            helper?.Cast(state.Spreads(role) ? Actions.DramaticLysis : Actions.FourthWallFusion, member.Position);
        }
    }

    private void MarkBustersAndStack()
    {
        Alive(state.StackHealer)?.AttachLockonVfx(LockonId.Stack);
        Alive(PartyRole.MainTank)?.AttachLockonVfx(LockonId.TankBuster);
        Alive(PartyRole.OffTank)?.AttachLockonVfx(LockonId.TankBuster);
    }

    private void VesselsBurst()
    {
        var i = 0;
        foreach (var at in M12sAct1State.VesselLayouts[state.VesselLayout])
        {
            var helper = helpers[i++];
            helper?.SetPosition(new Placement(at, 0f));
            helper?.Cast(Actions.Burst, at);
        }
    }

    private void BustersAndStack()
    {
        var i = 8;
        foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank }.Select(Alive).OfType<SimCharacter>())
        {
            var helper = helpers[i++];
            helper?.SetPosition(new Placement(tank.Position, 0f));
            helper?.Cast(Actions.VisceralBurst, tank);
        }
        if (Alive(state.StackHealer) is not { } healer) return;
        helpers[i]?.SetPosition(new Placement(healer.Position, 0f));
        helpers[i]?.Cast(Actions.Act1FourthWallFusion, healer);
    }
}
