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

namespace AnoMech.Scenarios.M11s.UltimateWeapons;

// After the second Crown of Arcadia: Dance of Domination's raidwides into the pair stacks between
// six lines, the second Raw Steel Trophy, Charybdistopia, Ultimate Trophy Weapons' six strikes with
// Maelstroms left behind, the tornadoes' gusts and One and Only.
public sealed class M11sUltimateWeaponsScenario : IScenario
{
    public string Name => "Dance of Domination to One and Only";
    public IPhase Phase => M11sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M11sUltimateWeaponsAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M11sUltimateWeaponsSettingsWindow settingsWindow = new();

    public const float DanceCastAt = 8.23f;
    public const float DanceAt = 16.33f;
    public static readonly float[] DanceRepeatAt = [17.44f, 18.02f, 18.6f, 19.18f, 19.75f];
    public const float DanceLastAt = 20.91f;
    public const float EyeCastAt = 20.87f;
    public const float ExplosionCastAt = 21.09f;
    public const float ExplosionResolves = 5f;
    public const float TrophyCastAt = 29.95f;
    public const float RawSteelSwingAt = 38.06f;
    public const float RawSteelFollowAt = 38.24f;
    public const float RawSteelHitAt = 39.4f;
    public const float CharybdistopiaCastAt = 49.65f;
    public const float UltimateCastAt = 57.32f;
    public static readonly float[] WeaponAppearsAt = [61.47f, 65.48f, 69.44f, 73.45f, 77.47f, 81.48f];
    public const float ApexCastAt = 63.11f;
    public static readonly float[] StrikeAt = [68.37f, 73.49f, 78.62f, 83.74f, 88.87f, 94f];
    public const float MaelstromRisesAfter = 1.55f;
    public const float RepositionAt = 97.12f;
    public const float ImmortalReignAt = 100.24f;
    public const float GustAt = 102.25f;
    public const float MaelstromsFadeAt = 104.27f;
    public const float OneAndOnlyAt = 106.95f;
    public const float FollowUpDelay = M11sTrophyWeapons.FollowUpDelay;
    public const float StrikeResolves = M11sTrophyWeapons.StrikeResolves;
    private static readonly float[] AutoAttackAt = [1f, 4f, 42f, 45f];

    public const float MaelstromRadius = 4f;
    private const float CharybdisEvery = 1f;
    private const float WalkBackSpeed = 40f;

    // The six lines with the open side south: three north-south, one closing the north, two diagonals.
    private static readonly (Vector3 From, float Heading)[] SouthOpenLines =
    [
        (new(-15f, 0f, -30f), 0f), (new(0f, 0f, -30f), 0f), (new(15f, 0f, -30f), 0f),
        (new(-30f, 0f, -15f), MathF.PI / 2f),
        (new(-21.21f, 0f, -21.21f), MathF.PI / 4f), (new(21.21f, 0f, -21.21f), -MathF.PI / 4f),
    ];

    private SimWorld world = null!;
    private M11sUltimateWeaponsState state = null!;
    private SimEnemy? tyrant;
    private M11sTrophyWeapons weapons = null!;
    private M11sRawSteel rawSteel = null!;
    private readonly SimEnemy?[] danceHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] eyeHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] lineHelpers = new SimEnemy?[12];
    private readonly SimEnemy?[] gustHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] maelstroms = new SimEnemy?[4];
    private SimEnemy? tornadoHelper;
    private SimEnemy? raidwideHelper;
    private readonly Dictionary<SimCharacter, float> lastCharybdis = [];
    private bool maelstromsUp;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M11sUltimateWeaponsState(world.Rng, settingsWindow.Overrides);
        weapons = new M11sTrophyWeapons(world, () => tyrant);
        rawSteel = new M11sRawSteel(world, () => tyrant);
        lastCharybdis.Clear();
        maelstromsUp = false;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M11sUltimateWeaponsState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, TankTheBoss);
        foreach (var at in AutoAttackAt)
            world.Events.Add(at, AutoAttack);
        world.Events.Add(DanceCastAt, HoldTheBoss);
        world.Events.Add(DanceCastAt, () => tyrant?.Cast(ActionId.DanceOfDominationTrophy, animationLock: 0.2f));
        world.Events.Add(DanceAt, () =>
        {
            tyrant?.Cast(ActionId.DanceOfDomination, animationLock: 0.2f);
            danceHelpers[0]?.Cast(Actions.DanceOfDominationFirst);
        });
        foreach (var at in DanceRepeatAt)
            world.Events.Add(at, () => danceHelpers[1]?.Cast(Actions.DanceOfDominationRepeat));
        world.Events.Add(EyeCastAt, EyeOfTheHurricane);
        world.Events.Add(DanceLastAt, () => danceHelpers[1]?.Cast(Actions.DanceOfDominationLast));
        world.Events.Add(ExplosionCastAt, Explosions);
        world.Events.Add(TrophyCastAt, TankTheBoss);
        world.Events.Add(TrophyCastAt, () => rawSteel.Trophy(state.AxeTrophy));
        world.Events.Add(RawSteelSwingAt, () => rawSteel.Swing(state.AxeTrophy));
        world.Events.Add(RawSteelFollowAt, () => rawSteel.Follow(state.AxeTrophy));
        world.Events.Add(RawSteelHitAt, () => rawSteel.Hit(state.AxeTrophy, state.HeavyHitterTarget));
        world.Events.Add(CharybdistopiaCastAt, () => tyrant?.Cast(Actions.Charybdistopia));
        world.Events.Add(UltimateCastAt, () => tyrant?.Cast(ActionId.UltimateTrophyWeapons, animationLock: 0.2f));
        world.Events.Add(ApexCastAt, HoldTheBoss);
        world.Events.Add(ApexCastAt, () =>
        {
            tyrant?.Face(state.WeaponSpot(0));
            tyrant?.Cast(ActionId.AssaultApex, animationLock: 0.2f);
        });
        for (var i = 0; i < M11sUltimateWeaponsState.Strikes; i++)
        {
            var index = i;
            world.Events.Add(WeaponAppearsAt[index], () => weapons.Show(state.Weapons[index], state.WeaponSpot(index)));
            world.Events.Add(StrikeAt[index], () => weapons.Strike(state.Weapons[index], state.WeaponSpot(index), state.ArrivalBearing(index)));
            world.Events.Add(StrikeAt[index] + FollowUpDelay, () => weapons.FollowUp(state.Weapons[index], state.WeaponSpot(index), state.HeavyWeightTarget));
            world.Events.Add(StrikeAt[index] + StrikeResolves + 0.8f, () => weapons.Hide(state.WeaponSpot(index)));
            if (index >= M11sUltimateWeaponsState.FirstTornadoWeapon)
                world.Events.Add(StrikeAt[index] + MaelstromRisesAfter, () => RaiseMaelstrom(index));
        }
        world.Events.Add(RepositionAt, ReturnToTheMiddle);
        world.Events.Add(ImmortalReignAt, () => tyrant?.Cast(ActionId.ImmortalReign, animationLock: 0.2f));
        world.Events.Add(GustAt, PowerfulGusts);
        world.Events.Add(MaelstromsFadeAt, FadeMaelstroms);
        world.Events.Add(OneAndOnlyAt, () =>
        {
            tyrant?.Cast(ActionId.OneAndOnly, animationLock: 0.2f);
            raidwideHelper?.Cast(Actions.OneAndOnlyRaidwide);
        });
    }

    public void Tick(float delta, float elapsed)
    {
        if (!maelstromsUp) return;
        foreach (var member in AliveMembers())
        {
            if (!maelstroms.Any(m => m is { Visible: true } && Vector3.Distance(m.Position, member.Position) < MaelstromRadius)) continue;
            if (lastCharybdis.TryGetValue(member, out var last) && elapsed - last < CharybdisEvery) continue;
            lastCharybdis[member] = elapsed;
            tornadoHelper?.SetPosition(new Placement(member.Position, 0f));
            tornadoHelper?.Cast(Actions.Charybdis, member);
        }
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
        for (var i = 0; i < danceHelpers.Length; i++) danceHelpers[i] = SpawnHelper();
        for (var i = 0; i < eyeHelpers.Length; i++) eyeHelpers[i] = SpawnHelper();
        for (var i = 0; i < lineHelpers.Length; i++) lineHelpers[i] = SpawnHelper();
        for (var i = 0; i < gustHelpers.Length; i++) gustHelpers[i] = SpawnHelper();
        tornadoHelper = SpawnHelper();
        raidwideHelper = SpawnHelper();
        for (var i = 0; i < maelstroms.Length; i++)
        {
            maelstroms[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Maelstrom,
                NameId: BNpcNameId.Maelstrom,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Vector3.Zero, 0f)));
            maelstroms[i]?.SetVisible(false);
        }
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

    private void EyeOfTheHurricane()
    {
        for (var i = 0; i < M11sUltimateWeaponsState.EyeTargets.Length; i++)
        {
            if (Alive(M11sUltimateWeaponsState.EyeTargets[i]) is not { } target) continue;
            target.AttachLockonVfx(LockonId.Stack);
            eyeHelpers[i]?.Cast(Actions.EyeOfTheHurricane, target);
        }
    }

    private void Explosions()
    {
        for (var i = 0; i < SouthOpenLines.Length; i++)
        {
            var (from, heading) = SouthOpenLines[i];
            var placement = new Placement(state.FromSouthOpen(from), heading + state.OpenBearing);
            lineHelpers[i]?.SetPosition(placement);
            lineHelpers[i]?.Cast(Actions.HurricaneExplosion);
            lineHelpers[i + SouthOpenLines.Length]?.SetPosition(placement);
            lineHelpers[i + SouthOpenLines.Length]?.Cast(ActionId.HurricaneExplosionVisual, animationLock: 0.2f);
        }
    }

    private void ReturnToTheMiddle()
    {
        tyrant?.Cast(ActionId.Reposition, Vector3.Zero, animationLock: 0.2f);
        tyrant?.MoveTo(Vector3.Zero, WalkBackSpeed);
    }

    private void RaiseMaelstrom(int index)
    {
        if (maelstroms[index - M11sUltimateWeaponsState.FirstTornadoWeapon] is not { } maelstrom) return;
        maelstrom.SetPosition(new Placement(state.WeaponSpot(index), state.ArrivalBearing(index)));
        maelstrom.SetVisible(true);
        maelstromsUp = true;
    }

    private void PowerfulGusts()
    {
        var helper = 0;
        foreach (var maelstrom in maelstroms.Where(m => m is { Visible: true }))
        {
            foreach (var target in AliveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, maelstrom!.Position)).Take(2))
            {
                if (helper >= gustHelpers.Length) return;
                gustHelpers[helper]?.SetPosition(maelstrom!.Placement());
                gustHelpers[helper++]?.Cast(Actions.PowerfulGust, target);
            }
        }
    }

    private void FadeMaelstroms()
    {
        maelstromsUp = false;
        foreach (var maelstrom in maelstroms) maelstrom?.SetVisible(false);
    }
}
