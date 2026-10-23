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
    public const float FollowUpDelay = 1.82f;
    public const float StrikeResolves = 2f;
    public const float VoidStardustCastAt = 75.83f;
    public static readonly float[][] CometiteAt = [[80.95f, 82.95f, 84.96f], [107.56f, 109.56f, 111.56f]];
    public const float CometiteResolves = 3f;
    public static readonly float[] CometCastAt = [84.96f, 111.56f];
    public const float CometResolves = 5f;
    private static readonly float[] AutoAttackAt = [0.5f, 3.04f, 11.11f, 14.14f, 27.69f, 30.73f, 63.46f, 66.49f];

    // The sword's cross is turned 30° back from its bearing.
    public const float SwordCrossTurn = -MathF.PI / 6f;

    private SimWorld world = null!;
    private M11sOpenerState state = null!;
    private SimEnemy? tyrant;
    private readonly Dictionary<Weapon, SimEnemy?> weapons = [];
    private readonly SimEnemy?[] coneHelpers = new SimEnemy?[3];
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] cometHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] cometiteHelpers = new SimEnemy?[24];
    private readonly SimEnemy?[] lineHelpers = new SimEnemy?[2];
    private SimEnemy? strikeHelper;
    private SimEnemy? stackHelper;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sOpenerState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sOpenerState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        foreach (var at in AutoAttackAt)
            world.Events.Add(at, AutoAttack);
        world.Events.Add(CrownCastAt[0], () => tyrant?.Cast(Actions.CrownOfArcadia));
        world.Events.Add(TrophyCastAt, () => tyrant?.Cast(state.AxeTrophyFirst ? ActionId.RawSteelTrophyAxe : ActionId.RawSteelTrophyScythe, animationLock: 0.2f));
        world.Events.Add(RawSteelSwingAt, () => tyrant?.Cast(state.AxeTrophyFirst ? ActionId.RawSteelAxe : ActionId.RawSteelScythe, animationLock: 0.2f));
        world.Events.Add(RawSteelFollowAt, RawSteelFollow);
        world.Events.Add(RawSteelHitAt, RawSteelHit);
        for (var s = 0; s < 2; s++)
        {
            var set = s;
            world.Events.Add(TrophyWeaponsCastAt[set], () => tyrant?.Cast(ActionId.TrophyWeapons, animationLock: 0.2f));
            world.Events.Add(WeaponsAppearAt[set], () => ShowWeapons(set));
            if (set == 1) world.Events.Add(VoidStardustCastAt, () => tyrant?.Cast(ActionId.VoidStardust, animationLock: 0.2f));
            world.Events.Add(AssaultCastAt[set], () => Assault(set));
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                world.Events.Add(StrikeAt[set][index], () => Strike(set, index));
                world.Events.Add(StrikeAt[set][index] + FollowUpDelay, () => FollowUp(set, index));
                world.Events.Add(StrikeAt[set][index] + StrikeResolves + 0.8f, () => HideWeapon(state.Weapons[set][index]));
            }
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
        weapons[Weapon.Axe] = SpawnWeapon(BNpcBaseId.Axe);
        weapons[Weapon.Scythe] = SpawnWeapon(BNpcBaseId.Scythe);
        weapons[Weapon.Sword] = SpawnWeapon(BNpcBaseId.Sword);
        for (var i = 0; i < coneHelpers.Length; i++) coneHelpers[i] = SpawnHelper();
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper();
        for (var i = 0; i < cometHelpers.Length; i++) cometHelpers[i] = SpawnHelper();
        for (var i = 0; i < cometiteHelpers.Length; i++) cometiteHelpers[i] = SpawnHelper();
        for (var i = 0; i < lineHelpers.Length; i++) lineHelpers[i] = SpawnHelper();
        strikeHelper = SpawnHelper();
        stackHelper = SpawnHelper();
    }

    private SimEnemy? SpawnWeapon(uint baseId)
    {
        var weapon = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: BNpcNameId.Tyrant,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(Vector3.Zero, 0f)));
        weapon?.SetVisible(false);
        return weapon;
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Tyrant,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private List<SimCharacter> AliveNonTanks() => Enum.GetValues<PartyRole>().Where(r => !r.IsTank()).Select(Alive).OfType<SimCharacter>().ToList();

    private void AutoAttack()
    {
        if (Alive(PartyRole.MainTank) is { } tank)
            tyrant?.Cast(ActionId.Attack, tank, animationLock: 0.2f);
    }

    private void RawSteelFollow()
    {
        if (state.AxeTrophyFirst)
            tyrant?.Cast(Actions.RawSteelAxeStack, Alive(PartyRole.MainTank) ?? Alive(PartyRole.OffTank));
        else
            tyrant?.Cast(ActionId.RawSteelScytheFollow, animationLock: 0.2f);
    }

    private void RawSteelHit()
    {
        if (tyrant is not { } boss) return;
        if (state.AxeTrophyFirst)
        {
            var i = 0;
            foreach (var member in AliveNonTanks())
            {
                spreadHelpers[i]?.SetPosition(boss.Placement());
                spreadHelpers[i++]?.Cast(Actions.Impact, member);
            }
            return;
        }
        PartyRole[] targets = [PartyRole.MainTank, PartyRole.OffTank];
        for (var t = 0; t < targets.Length; t++)
        {
            if (Alive(targets[t]) is not { } tank) continue;
            coneHelpers[t]?.SetPosition(boss.Placement());
            coneHelpers[t]?.Cast(Actions.RawSteelScytheCone, tank);
        }
        var hitter = Alive(state.HeavyHitterTarget) ?? AliveNonTanks().FirstOrDefault();
        coneHelpers[2]?.SetPosition(boss.Placement());
        if (hitter != null) coneHelpers[2]?.Cast(Actions.HeavyHitter, hitter);
    }

    private void ShowWeapons(int set)
    {
        for (var i = 0; i < 3; i++)
        {
            var kind = state.Weapons[set][i];
            if (weapons[kind] is not { } weapon) continue;
            var bearing = state.WeaponBearing(set, i);
            weapon.SetPosition(new Placement(state.WeaponSpot(set, i), bearing));
            weapon.SetVisible(true);
            weapon.PlayActionTimeline(kind switch
            {
                Weapon.Axe => TimelineId.AxeAppears,
                Weapon.Scythe => TimelineId.ScytheAppears,
                _ => TimelineId.SwordAppears,
            });
        }
    }

    private void HideWeapon(Weapon kind) => weapons[kind]?.SetVisible(false);

    private void Assault(int set)
    {
        tyrant?.Face(state.WeaponSpot(set, 0));
        tyrant?.Cast(ActionId.AssaultEvolved, animationLock: 0.2f);
    }

    private void Strike(int set, int index)
    {
        var kind = state.Weapons[set][index];
        var spot = state.WeaponSpot(set, index);
        var bearing = state.WeaponBearing(set, index);
        tyrant?.Face(spot);
        tyrant?.Cast(kind switch
        {
            Weapon.Axe => ActionId.TurnToAxe,
            Weapon.Scythe => ActionId.TurnToScythe,
            _ => ActionId.TurnToSword,
        }, animationLock: 0.2f);
        weapons[kind]?.PlayActionTimeline(kind switch
        {
            Weapon.Axe => TimelineId.AxeStrikes,
            Weapon.Scythe => TimelineId.ScytheStrikes,
            _ => TimelineId.SwordStrikes,
        });
        strikeHelper?.SetPosition(new Placement(spot, kind == Weapon.Sword ? bearing + SwordCrossTurn : bearing));
        strikeHelper?.Cast(kind switch
        {
            Weapon.Axe => Actions.AxeStrike,
            Weapon.Scythe => Actions.ScytheStrike,
            _ => Actions.SwordStrike,
        });
    }

    private void FollowUp(int set, int index)
    {
        var spot = state.WeaponSpot(set, index);
        switch (state.Weapons[set][index])
        {
            case Weapon.Axe:
                stackHelper?.SetPosition(new Placement(tyrant?.Position ?? Vector3.Zero, 0f));
                stackHelper?.Cast(Actions.HeavyWeight);
                break;
            case Weapon.Scythe:
                var i = 0;
                foreach (var member in AliveMembers())
                {
                    spreadHelpers[i]?.SetPosition(new Placement(spot, 0f));
                    spreadHelpers[i++]?.Cast(Actions.SweepingVictory, member);
                }
                break;
            default:
                PartyRole[][] parties = [M11sOpenerAi.LightPartyOne, M11sOpenerAi.LightPartyTwo];
                for (var p = 0; p < parties.Length; p++)
                {
                    var bait = Alive(parties[p][1]) ?? parties[p].Select(Alive).FirstOrDefault(m => m != null);
                    if (bait == null) continue;
                    lineHelpers[p]?.SetPosition(new Placement(spot, 0f));
                    lineHelpers[p]?.Cast(Actions.SharpTaste, bait);
                }
                break;
        }
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
