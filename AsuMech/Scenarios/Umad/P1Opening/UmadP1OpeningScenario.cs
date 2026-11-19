using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Umad.P1TeleTrouncing;
using static AsuMech.Scenarios.Umad.UmadConstants;

namespace AsuMech.Scenarios.Umad.P1Opening;

// Dancing Mad P1 from the pull to Tele-trouncing: both Graven Images, Light of Judgment and Hyperdrive.
// t=0 = pull +0:08 (Revolting Ruin III starts at 2.14).
public sealed class UmadP1OpeningScenario : IScenario
{
    public string Name => "Opening";
    public IPhase Phase => UmadZone.P1;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP1OpeningAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly UmadP1OpeningSettingsWindow settingsWindow = new();

    public const float RuinCastAt = 2.14f;
    public const float TankSwapAt = 11.0f;
    public const float FirstTethersAt = 21.90f;
    public const float FirstMysteryMagicAt = 24.30f;
    public const float PulseWaveAt = 26.96f;
    public const float FireAt = 30.09f;
    public const float LaserAt = 34.33f;
    public const float TowersAt = 34.99f;
    public const float FirstConfettiAt = 41.59f;
    public const float SecondMysteryMagicAt = 40.57f;
    public const float SecondMysteryResolveAt = 45.57f;
    public const float LightOfJudgmentAt = 49.66f;
    public static readonly float[] FirstHyperdriveAt = [57.81f, 59.90f, 62.00f];
    public const float SecondTethersAt = 72.95f;
    public const float ThirdIceAt = 74.38f;
    public static readonly float[] GravitasAt = [79.46f, 97.92f];
    public static readonly float[] VitrophyreAt = [83.50f, 101.93f];
    public const float SecondRuinCastAt = 84.24f;
    public static readonly float[] WillWindUpAt = [88.23f, 101.31f];
    public static readonly float[] WillAt = [93.30f, 106.43f];
    public const float RetetherAt = 89.41f;
    public const float TankSwapBackAt = 93.0f;
    public const float PuddlesArmAt = 109.90f;
    public const float SecondConfettiAt = 110.22f;
    public const float UnpoppedPuddlesAt = 113.6f;
    public const float SecondLightOfJudgmentAt = 119.46f;
    public static readonly float[] SecondHyperdriveAt = [127.60f, 129.70f, 131.80f];
    public const float EndAt = 136f;

    public static readonly Vector3 PulseWaveSource = new(0f, 0f, -44f);
    public static readonly Vector3 WaveCannonSource = new(0f, 0f, -35f);
    private static readonly Vector3 FirstTetherSource = new(0f, 18.5f, -44f);
    private static readonly Vector3 GravityTetherSource = new(2.5f, 27f, -77.5f);
    private static readonly Vector3 RockTetherSource = new(26f, 41.5f, -93f);
    private static readonly Vector3 GravitasSource = new(2.5f, 0f, -73f);
    private static readonly Vector3 VitrophyreSource = new(26f, 0f, -58.5f);

    // UNVERIFIED: how close a player must pass to pop an armed puddle, and how long it takes to go off.
    public const float PuddlePopRadius = 3f;
    private const float PuddleDetonateDelay = 1.4f;
    private const float PuddleAppearDelay = 0.8f;

    private static readonly (uint EObjId, uint LayoutId, Vector3 Position, uint Arg2)[] Statues =
    [
        (EObjId.EarlierStatueA, EObjId.EarlierStatueA1LayoutId, new(0f, 18.5f, -44f), 0x00420003U),
        (EObjId.EarlierStatueA, EObjId.EarlierStatueA2LayoutId, new(0f, 0f, -35f), 0x00410003U),
        (EObjId.EarlierStatueB, EObjId.EarlierStatueB1LayoutId, new(2.5f, 22.5f, -73f), 0x00450003U),
        (EObjId.EarlierStatueC, EObjId.EarlierStatueC1LayoutId, new(26f, 7f, -58.5f), 0x00460003U),
        (EObjId.EarlierStatueB, EObjId.EarlierStatueB2LayoutId, new(-8f, 15f, -73f), 0x00430003U),
        (EObjId.EarlierStatueC, EObjId.EarlierStatueC2LayoutId, new(16f, 6.5f, -57f), 0x00440003U),
    ];
    private const int TetherStatue = 0, LaserStatue = 1, GravityStatue = 2, RockStatue = 3, WestWillStatue = 4, EastWillStatue = 5;

    private static readonly (uint State, uint Mask) Appear = (0x1, 0x2);
    private static readonly (uint State, uint Mask) Fire = (0x10, 0x20);
    private static readonly (uint State, uint Mask) WindUp = (0x40, 0x80);
    private static readonly (uint State, uint Mask) Release = (0x100, 0x200);
    private static readonly (uint State, uint Mask) Gone = (0x4, 0x8);

    private SimWorld world = null!;
    private SimParty party = null!;
    private UmadP1OpeningState state = null!;
    private SimEnemy? kefka;
    private PartyRole tank = PartyRole.MainTank;
    private readonly SimEventObject?[] statues = new SimEventObject?[6];
    private SimEventObject? colossus;
    private readonly List<SimEnemy> tetherHelpers = [];
    private readonly List<GravityPuddle> puddles = [];
    private bool puddlesFailed;

    public UmadP1OpeningState? LastState { get; private set; }

    private sealed class GravityPuddle(Vector3 position, SimEventObject? prop)
    {
        public Vector3 Position { get; } = position;
        public SimEventObject? Prop { get; } = prop;
        public bool Armed { get; set; }
        public bool Spent { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new UmadP1OpeningState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        tank = PartyRole.MainTank;
        tetherHelpers.Clear();
        puddles.Clear();
        puddlesFailed = false;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP1OpeningState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnKefka);
        world.Events.Add(0f, SpawnStatues);
        world.Events.Add(2.06f, () => Beat(colossus, Appear));
        world.Events.Add(RuinCastAt, () => kefka?.Cast(UmadActions.RevoltingRuinIII, Tank()));
        world.Events.Add(RuinCastAt, () => Tank()?.AttachLockonVfx(LockonId.RevoltingRuin));
        world.Events.Add(10.34f, () => kefka?.CastFromHelper(UmadActions.RevoltingRuinIIISecond, Tank()));
        world.Events.Add(TankSwapAt, () => HandBossTo(PartyRole.OffTank));
        world.Events.Add(16.53f, () => kefka?.Cast(ActionId.KefkaPoof, animationLock: 1.1f));

        world.Events.Add(18.14f, () => kefka?.Cast(ActionId.GravenImage, animationLock: 2.1f));
        world.Events.Add(FirstTethersAt, TetherFirstGroup);
        world.Events.Add(24.23f, () => ShowOrbs(state.FireIsLie ? LockonId.FireFalse : LockonId.FireTrue, state.FirstIce));
        world.Events.Add(24.23f, AttachFireMarkers);
        world.Events.Add(FirstMysteryMagicAt, () => kefka?.Cast(ActionId.MysteryMagic, animationLock: 3.1f));
        world.Events.Add(FirstMysteryMagicAt, () => CastBlizzard(state.FirstIce));
        world.Events.Add(26.92f, () => Beat(statues[TetherStatue], Fire));
        world.Events.Add(PulseWaveAt, KnockTetheredBack);
        world.Events.Add(29.20f, () => Beat(statues[LaserStatue], WindUp));
        world.Events.Add(FireAt, ResolveFire);
        world.Events.Add(33.47f, () => kefka?.Cast(ActionId.DoubleTroubleTrap_Cast, animationLock: 1.1f));
        world.Events.Add(34.20f, () => Beat(statues[LaserStatue], Release));
        world.Events.Add(LaserAt, FireLasers);
        world.Events.Add(TowersAt, DropTowersUnderLasered);
        world.Events.Add(36.47f, () => GiveConfetti(0, 4.99f));
        world.Events.Add(40.51f, () => ShowOrbs(state.ThunderIsLie ? LockonId.LightningFalse : LockonId.LightningTrue, state.SecondIce));
        world.Events.Add(SecondMysteryMagicAt, () => kefka?.Cast(ActionId.MysteryMagic, animationLock: 3.1f));
        world.Events.Add(SecondMysteryMagicAt, () => CastBlizzard(state.SecondIce));
        world.Events.Add(SecondMysteryMagicAt, CastThunder);
        world.Events.Add(FirstConfettiAt, () => ResolveConfetti(0));
        world.Events.Add(42.18f, () => GiveConfetti(1, 67.99f));

        world.Events.Add(LightOfJudgmentAt, () => kefka?.Cast(UmadActions.LightOfJudgmentP1));
        world.Events.Add(51.58f, () => Beat(colossus, Fire));
        foreach (var at in FirstHyperdriveAt)
            world.Events.Add(at, () => kefka?.Cast(UmadActions.Hyperdrive, Tank()));
        world.Events.Add(67.79f, () => kefka?.Cast(ActionId.KefkaPoof, animationLock: 1.1f));

        world.Events.Add(69.23f, () => kefka?.Cast(ActionId.GravenImage, animationLock: 2.1f));
        world.Events.Add(SecondTethersAt, () => Beat(statues[GravityStatue], Appear));
        world.Events.Add(SecondTethersAt, () => Beat(statues[RockStatue], Appear));
        world.Events.Add(SecondTethersAt, () => TetherGravityAndRock(0));
        world.Events.Add(74.27f, () => ShowOrbs(null, state.ThirdIce));
        world.Events.Add(ThirdIceAt, () => kefka?.Cast(ActionId.BlizzardIIIBlowout_Cast, animationLock: 3.1f));
        world.Events.Add(ThirdIceAt, () => CastBlizzard(state.ThirdIce));
        for (var r = 0; r < UmadP1OpeningState.GravenRounds; r++)
        {
            var round = r;
            world.Events.Add(GravitasAt[round], () => DropGravity(round));
            world.Events.Add(VitrophyreAt[round], () => DropRocks(round));
            world.Events.Add(WillWindUpAt[round], () => Beat(WillStatue(round), WindUp));
            world.Events.Add(WillAt[round], () => ResolveWill(round));
        }
        world.Events.Add(SecondRuinCastAt, () => kefka?.Cast(UmadActions.RevoltingRuinIII, Tank()));
        world.Events.Add(SecondRuinCastAt, () => Tank()?.AttachLockonVfx(LockonId.RevoltingRuin));
        world.Events.Add(RetetherAt, () => TetherGravityAndRock(1));
        world.Events.Add(92.43f, () => kefka?.CastFromHelper(UmadActions.RevoltingRuinIIISecond, Tank()));
        world.Events.Add(TankSwapBackAt, () => HandBossTo(PartyRole.MainTank));
        world.Events.Add(105.85f, () => Beat(statues[GravityStatue], Fire));
        world.Events.Add(105.85f, () => Beat(statues[RockStatue], Fire));
        world.Events.Add(PuddlesArmAt, ArmPuddles);
        world.Events.Add(SecondConfettiAt, () => ResolveConfetti(1));
        world.Events.Add(110.85f, () => GiveConfetti(2, 48.99f));
        world.Events.Add(UnpoppedPuddlesAt, PunishUnpoppedPuddles);

        world.Events.Add(SecondLightOfJudgmentAt, () => kefka?.Cast(UmadActions.LightOfJudgmentP1));
        world.Events.Add(119.25f, () => Beat(colossus, WindUp));
        foreach (var at in SecondHyperdriveAt)
            world.Events.Add(at, () => kefka?.Cast(UmadActions.Hyperdrive, Tank()));
        world.Events.Add(EndAt, DespawnHelpers);
    }

    public void Tick(float delta, float elapsed)
    {
        foreach (var puddle in puddles)
        {
            if (!puddle.Armed || puddle.Spent) continue;
            if (party.Find.InsideCircle(puddle.Position, PuddlePopRadius).Count == 0) continue;
            puddle.Spent = true;
            world.Events.Add(PuddleDetonateDelay, () => Detonate(puddle, UmadActions.GravityIII));
        }
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        if (resolved.Action.ActionId == ActionId.Explosion && resolved.Hits.Count == 0)
            kefka?.CastFromHelper(UmadActions.UnmitigatedExplosion);
        if (resolved.Action.ActionId == ActionId.Gravitas)
        {
            var at = resolved.Origin.Position;
            world.Events.Add(PuddleAppearDelay, () => SpawnPuddle(at));
        }
        if (resolved.Action.ActionId == ActionId.Vitrophyre)
            foreach (var puddle in puddles.Where(p => !p.Spent && Distance(p.Position, resolved.Origin.Position) <= 5f).ToList())
            {
                puddle.Spent = true;
                Detonate(puddle, UmadActions.GravitationalExplosion);
            }
    }

    public static readonly float[] IceHeadings = [-3f * MathF.PI / 4f, -MathF.PI / 4f, MathF.PI / 4f, 3f * MathF.PI / 4f];
    public static bool IsNwSe(float heading) => MathF.Abs(heading - (-3f * MathF.PI / 4f)) < 0.01f || MathF.Abs(heading - MathF.PI / 4f) < 0.01f;

    public static bool InIce(IceSet ice, Vector2 point, float marginRadians = 0f)
    {
        if (point.LengthSquared() < 0.01f) return true;
        var heading = MathF.Atan2(point.X, point.Y);
        foreach (var cone in IceHeadings)
        {
            if (IsNwSe(cone) != ice.RealNwSe) continue;
            var diff = MathF.Abs(MathF.IEEERemainder(heading - cone, 2f * MathF.PI));
            if (diff <= MathF.PI / 4f + marginRadians) return true;
        }
        return false;
    }

    public static IEnumerable<Placement> RealThunderLanes(UmadP1OpeningState state)
        => UmadP1TeleTrouncingScenario.ThunderAnchors
            .Where((_, i) => (i + state.ThunderRealOffset) % 2 == 0)
            .Select(a => a.MulX(state.ThunderOrientation).MulRot(state.ThunderOrientation));

    public static bool InThunder(UmadP1OpeningState state, Vector2 point, float margin = 0f)
        => RealThunderLanes(state).Any(lane =>
        {
            var dir = new Vector2(MathF.Sin(lane.Rotation), MathF.Cos(lane.Rotation));
            var local = point - lane.Position2;
            var along = Vector2.Dot(local, dir);
            var across = MathF.Abs(local.X * dir.Y - local.Y * dir.X);
            return along >= -margin && along <= 40f + margin && across <= 5f + margin;
        });

    private SimCharacter? Tank() => party.Get(tank);

    private void HandBossTo(PartyRole role)
    {
        tank = role;
        kefka?.KeepFacing(Tank());
        kefka?.SetTarget(Tank(), follow: false);
    }

    private void SpawnKefka()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.KefkaP3, NameId: BNpcNameId.Kefka, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            Placement: new Placement(Vector3.Zero, MathF.PI), LayoutId: BNpcLayoutId.KefkaP3));
        HandBossTo(PartyRole.MainTank);
    }

    // The statue props bind to their pre-placed layout instances, hidden until they appear.
    private void SpawnStatues()
    {
        for (var i = 0; i < Statues.Length; i++)
        {
            var (eobjId, layoutId, position, arg2) = Statues[i];
            statues[i] = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = eobjId, LayoutId = layoutId, EventId = EObjId.PropEventId,
                EntityId = Spawn.EarlierStatueEntityIdBase + (uint)i, TargetableStatus = 5, Arg2 = arg2,
                Placement = new Placement(position, 0f), TimelineState = EObjId.GazeStatueSpawnState,
            });
        }
        colossus = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.GravenStatue, LayoutId = EObjId.GravenStatueLayoutId, EventId = EObjId.PropEventId,
            EntityId = 0x4000EB8AU, TargetableStatus = 5, Arg2 = EObjId.GravenStatueArg2,
            Placement = new Placement(Vector3.Zero, 0f), TimelineState = EObjId.GazeStatueSpawnState,
        });
    }

    private static void Beat(SimEventObject? prop, (uint State, uint Mask) beat) => prop?.PlayBeat(beat.State, beat.Mask);

    private SimEventObject? WillStatue(int round) => statues[state.WillEast[round] ? EastWillStatue : WestWillStatue];

    private SimEnemy? TetherHelper(Vector3 at)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GravenImage, NameId: BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new Placement(at, 0f), LayoutId: BNpcLayoutId.GravenImage));
        if (helper != null) tetherHelpers.Add(helper);
        return helper;
    }

    private IEnumerable<SimCharacter> Members(Func<PartyRole, bool> which)
        => Enum.GetValues<PartyRole>().Where(which).Select(r => party.Get(r)).OfType<SimCharacter>().Where(m => m.IsAlive());

    private void TetherFirstGroup()
    {
        Beat(statues[TetherStatue], Appear);
        foreach (var member in Members(state.IsTethered))
            world.Tether(TetherHelper(FirstTetherSource), member, TetherId.GravenImage, duration: PulseWaveAt - FirstTethersAt);
    }

    private void KnockTetheredBack()
    {
        foreach (var member in Members(state.IsTethered))
            kefka?.CastFromHelper(UmadActions.PulseWave, member, at: new Placement(PulseWaveSource, 0f));
    }

    private void TetherGravityAndRock(int round)
    {
        var lasts = new[] { GravitasAt[round] - (round == 0 ? SecondTethersAt : RetetherAt), VitrophyreAt[round] - (round == 0 ? SecondTethersAt : RetetherAt) };
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            var gravity = state.TakesGravitas(role, round);
            world.Tether(TetherHelper(gravity ? GravityTetherSource : RockTetherSource), member, TetherId.GravenImage, duration: lasts[gravity ? 0 : 1]);
        }
    }

    private void ShowOrbs(uint? second, IceSet ice)
    {
        kefka?.AttachLockonVfx(ice.IsLie ? LockonId.ColdFalse : LockonId.ColdTrue);
        if (second is { } orb) kefka?.AttachLockonVfx(orb);
    }

    // A truthful set telegraphs its real cones; a lie telegraphs the safe pair and hits the other unseen.
    private void CastBlizzard(IceSet ice)
    {
        foreach (var heading in IceHeadings)
        {
            var real = IsNwSe(heading) == ice.RealNwSe;
            var at = new Placement(Vector3.Zero, heading);
            if (real) kefka?.CastFromHelper(ice.IsLie ? UmadActions.BlizzardIIIBlowoutLie : UmadActions.BlizzardIIIBlowout, at: at);
            else if (ice.IsLie) kefka?.CastFromHelper(ActionId.BlizzardIIIBlowout_FakeOmen, at: at, animationLock: 1.1f);
        }
    }

    private void CastThunder()
    {
        var realLine = state.ThunderIsLie ? UmadActions.ThrummingThunderReal2 : UmadActions.ThrummingThunderReal1;
        var anchors = UmadP1TeleTrouncingScenario.ThunderAnchors;
        for (var i = 0; i < anchors.Length; i++)
        {
            var real = (i + state.ThunderRealOffset) % 2 == 0;
            if (!real && !state.ThunderIsLie) continue;
            var at = anchors[i].MulX(state.ThunderOrientation).MulRot(state.ThunderOrientation);
            if (real) kefka?.CastFromHelper(realLine, at: at);
            else kefka?.CastFromHelper(ActionId.ThrummingThunderIII_FakeOmen, at: at, animationLock: 1.1f);
        }
    }

    private void AttachFireMarkers()
    {
        foreach (var member in Members(_ => true))
        {
            var role = ((ISimPartyMember)member).Role;
            if (!state.FireShowsStackIcon) member.AttachLockonVfx(LockonId.FireSpread);
            else if (role == state.FireStackSupport || role == state.FireStackDps) member.AttachLockonVfx(LockonId.FireStack);
        }
    }

    private void ResolveFire()
    {
        if (state.FireIsStack)
        {
            foreach (var holder in Members(r => r == state.FireStackSupport || r == state.FireStackDps))
                kefka?.CastFromHelper(UmadActions.FlagrantFireStack, holder);
            return;
        }
        foreach (var member in Members(_ => true))
            kefka?.CastFromHelper(UmadActions.FlagrantFireSpread, member);
    }

    private void FireLasers()
    {
        foreach (var member in Members(state.IsLasered))
            kefka?.CastFromHelper(UmadActions.WaveCannon, member, at: new Placement(WaveCannonSource, 0f));
    }

    private void DropTowersUnderLasered()
    {
        foreach (var member in Members(state.IsLasered))
        {
            var at = member.Position with { Y = 0f };
            kefka?.CastFromHelper(UmadActions.Explosion, at, at: new Placement(at, 0f));
        }
    }

    private void GiveConfetti(int wave, float duration)
    {
        var (support, dps) = state.Confetti[wave];
        foreach (var holder in Members(r => r == support || r == dps))
            holder.AddStatus(StatusId.DoubleTroubleTrap, duration);
    }

    private void ResolveConfetti(int wave)
    {
        var (support, dps) = state.Confetti[wave];
        foreach (var holder in Members(r => r == support || r == dps))
        {
            holder.RemoveStatus(StatusId.DoubleTroubleTrap);
            kefka?.CastFromHelper(UmadActions.DoubleTroubleTrapStack, holder);
        }
    }

    private void DropGravity(int round)
    {
        foreach (var member in Members(r => state.TakesGravitas(r, round)))
            kefka?.CastFromHelper(UmadActions.Gravitas, member, at: new Placement(GravitasSource, 0f));
    }

    private void DropRocks(int round)
    {
        foreach (var member in Members(r => !state.TakesGravitas(r, round)))
            kefka?.CastFromHelper(UmadActions.Vitrophyre, member, at: new Placement(VitrophyreSource, 0f));
    }

    private void ResolveWill(int round)
    {
        Beat(WillStatue(round), Release);
        var east = state.WillEast[round];
        kefka?.CastFromHelper(east ? UmadActions.IntemperateWill : UmadActions.GravitationalWave,
            at: new Placement(Vector3.Zero, east ? MathF.PI / 2f : -MathF.PI / 2f));
    }

    private void SpawnPuddle(Vector3 at)
    {
        var prop = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.GravityPuddle, EventId = EObjId.PropEventId, TargetableStatus = 5,
            Arg2 = ((uint)(0x50 + puddles.Count) << 16) | 0x3U,
            Placement = new Placement(at, 0f), TimelineState = 0x1,
        });
        puddles.Add(new GravityPuddle(at, prop));
    }

    private void ArmPuddles()
    {
        foreach (var puddle in puddles.Where(p => !p.Spent))
        {
            puddle.Armed = true;
            Beat(puddle.Prop, Fire);
        }
    }

    private void Detonate(GravityPuddle puddle, EnemyAction action)
    {
        Beat(puddle.Prop, Gone);
        puddle.Prop?.Despawn();
        if (action.ActionId == ActionId.GravitationalExplosion)
        {
            if (puddlesFailed) return;
            puddlesFailed = true;
            kefka?.CastFromHelper(action);
            return;
        }
        kefka?.CastFromHelper(action, puddle.Position, at: new Placement(puddle.Position, 0f));
    }

    private void PunishUnpoppedPuddles()
    {
        foreach (var puddle in puddles.Where(p => !p.Spent).ToList())
        {
            puddle.Spent = true;
            Detonate(puddle, UmadActions.GravitationalExplosion);
        }
    }

    private void DespawnHelpers()
    {
        foreach (var helper in tetherHelpers) helper.Despawn();
        tetherHelpers.Clear();
    }

    private static float Distance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
}
