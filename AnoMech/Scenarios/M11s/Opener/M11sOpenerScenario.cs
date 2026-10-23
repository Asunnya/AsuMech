using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M11s.M11sConstants;
using Actions = AnoMech.Scenarios.M11s.M11sActions;

namespace AnoMech.Scenarios.M11s.Opener;

// The Tyrant's opener, from the pull to the second Crown of Arcadia: Raw Steel Trophy (axe or
// scythe), two Trophy Weapons into Assault Evolved, and Void Stardust's chasing comets ending in
// a stack or spreads, twice.
public sealed class M11sOpenerScenario : IScenario
{
    public string Name => "Raw Steel Trophy to Void Stardust";
    public IPhase Phase => M11sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M11sOpenerAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M11sOpenerSettingsWindow settingsWindow = new();

    public static readonly float[] CrownCastAt = [5.17f, 117.58f];
    public const float TrophyCastAt = 15.34f;
    public const float RawSteelSwingAt = 23.45f;
    public const float RawSteelFollowAt = 23.63f;
    public const float RawSteelHitAt = 24.79f;
    public static readonly float[] TrophyWeaponsCastAt = [35.06f, 69.65f];
    public static readonly float[] WeaponsAppearAt = [39.36f, 73.79f];
    public static readonly float[] AssaultCastAt = [41.92f, 86.92f];
    public static readonly float[][] StrikeAt = [[48.17f, 53.29f, 58.42f], [93.16f, 98.29f, 103.41f]];
    public const float FollowUpDelay = M11sTrophyWeapons.FollowUpDelay;
    public const float StrikeResolves = M11sTrophyWeapons.StrikeResolves;
    public const float VoidStardustCastAt = 75.83f;
    public static readonly float[][] CometiteAt = [[80.95f, 82.95f, 84.96f], [107.56f, 109.56f, 111.56f]];
    public const float CometiteResolves = 3f;
    public static readonly float[] CometCastAt = [84.96f, 111.56f];
    public const float CometResolves = 5f;
    private static readonly float[] AutoAttackAt = [0.5f, 3.04f, 11.11f, 14.14f, 27.69f, 30.73f, 63.46f, 66.49f];

    public const float RepositionAt = 67.55f;
    private const float WalkBackSpeed = 40f;

    private SimWorld world = null!;
    private M11sOpenerState state = null!;
    private SimEnemy? tyrant;
    private M11sTrophyWeapons weapons = null!;
    private M11sRawSteel rawSteel = null!;
    private readonly SimEnemy?[] cometHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] cometiteHelpers = new SimEnemy?[24];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sOpenerState(world.Rng, settingsWindow.Overrides);
        weapons = new M11sTrophyWeapons(world, () => tyrant);
        rawSteel = new M11sRawSteel(world, () => tyrant);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sOpenerState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, TankTheBoss);
        foreach (var at in AutoAttackAt)
            world.Events.Add(at, AutoAttack);
        world.Events.Add(CrownCastAt[0], () => tyrant?.Cast(Actions.CrownOfArcadia));
        world.Events.Add(TrophyCastAt, () => rawSteel.Trophy(state.AxeTrophyFirst));
        world.Events.Add(RawSteelSwingAt, () => rawSteel.Swing(state.AxeTrophyFirst));
        world.Events.Add(RawSteelFollowAt, () => rawSteel.Follow(state.AxeTrophyFirst));
        world.Events.Add(RawSteelHitAt, () => rawSteel.Hit(state.AxeTrophyFirst, state.HeavyHitterTarget));
        for (var s = 0; s < 2; s++)
        {
            var set = s;
            world.Events.Add(TrophyWeaponsCastAt[set], () => tyrant?.Cast(ActionId.TrophyWeapons, animationLock: 0.2f));
            world.Events.Add(WeaponsAppearAt[set], () =>
            {
                for (var i = 0; i < 3; i++) weapons.Show(state.Weapons[set][i], state.WeaponSpot(set, i));
            });
            if (set == 1)
            {
                world.Events.Add(RepositionAt, ReturnToTheMiddle);
                world.Events.Add(VoidStardustCastAt, () => tyrant?.Cast(ActionId.VoidStardust, animationLock: 0.2f));
            }
            world.Events.Add(AssaultCastAt[set], () => Assault(set));
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                world.Events.Add(StrikeAt[set][index], () => weapons.Strike(state.Weapons[set][index], state.WeaponSpot(set, index), state.ArrivalBearing(set, index)));
                world.Events.Add(StrikeAt[set][index] + FollowUpDelay, () => weapons.FollowUp(state.Weapons[set][index], state.WeaponSpot(set, index), state.HeavyWeightTarget));
                world.Events.Add(StrikeAt[set][index] + StrikeResolves + 0.8f, () => weapons.Hide(state.WeaponSpot(set, index)));
            }
            if (set == 0) world.Events.Add(StrikeAt[set][2] + StrikeResolves, TankTheBoss);
        }
        for (var s = 0; s < 2; s++)
        {
            var set = s;
            for (var w = 0; w < 3; w++)
            {
                var wave = w;
                world.Events.Add(CometiteAt[set][wave], () => DropCometites(wave));
            }
            world.Events.Add(CometCastAt[set], () => Comet(set));
        }
        world.Events.Add(CrownCastAt[1], () => tyrant?.Cast(Actions.CrownOfArcadia));
    }

    public void Tick(float delta, float elapsed)
    {
    }

    private void SpawnActors()
    {
        tyrant = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Tyrant,
            NameId: BNpcNameId.Tyrant,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        weapons.Spawn();
        rawSteel.Spawn();
        for (var i = 0; i < cometHelpers.Length; i++) cometHelpers[i] = SpawnHelper();
        for (var i = 0; i < cometiteHelpers.Length; i++) cometiteHelpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Tyrant,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void TankTheBoss() => tyrant?.SetTarget(Alive(PartyRole.MainTank));

    private void HoldTheBoss() => tyrant?.Follow();

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private void AutoAttack()
    {
        if (Alive(PartyRole.MainTank) is { } tank)
            tyrant?.Cast(ActionId.Attack, tank, animationLock: 0.2f);
    }

    private void ReturnToTheMiddle()
    {
        tyrant?.Cast(ActionId.Reposition, Vector3.Zero, animationLock: 0.2f);
        tyrant?.MoveTo(Vector3.Zero, WalkBackSpeed);
    }

    private void Assault(int set)
    {
        HoldTheBoss();
        tyrant?.Face(state.WeaponSpot(set, 0));
        tyrant?.Cast(ActionId.AssaultEvolved, animationLock: 0.2f);
    }

    private void DropCometites(int wave)
    {
        var i = 0;
        foreach (var member in AliveMembers())
        {
            var helper = cometiteHelpers[wave * 8 + i++];
            helper?.SetPosition(new Placement(member.Position, 0f));
            helper?.Cast(Actions.Cometite, member.Position);
        }
    }

    private void Comet(int set)
    {
        if (state.StackOnSet(set))
        {
            var target = Alive(state.CrushingCometTarget) ?? AliveMembers().FirstOrDefault();
            if (target == null) return;
            target.AttachLockonVfx(LockonId.Stack);
            cometHelpers[0]?.SetPosition(new Placement(target.Position, 0f));
            cometHelpers[0]?.Cast(Actions.CrushingComet, target);
            return;
        }
        var i = 0;
        foreach (var member in AliveMembers())
        {
            member.AttachLockonVfx(LockonId.Spread);
            cometHelpers[i]?.SetPosition(new Placement(member.Position, 0f));
            cometHelpers[i++]?.Cast(Actions.Comet, member);
        }
    }
}
