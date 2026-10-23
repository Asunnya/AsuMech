using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M11s.M11sConstants;
using Actions = AnoMech.Scenarios.M11s.M11sActions;

namespace AnoMech.Scenarios.M11s;

public enum Weapon
{
    Axe,
    Scythe,
    Sword,
}

// The Tyrant's trophy weapons for Assault Evolved and Assault Apex: each strike, The Tyrant dashes
// to the weapon, its own area goes off there and the follow-up lands on the party.
public sealed class M11sTrophyWeapons(SimWorld world, Func<SimEnemy?> tyrant)
{
    public const float FollowUpDelay = 1.82f;
    public const float StrikeResolves = 2f;
    private const float DashSpeed = 40f;
    private const int PerKind = 2;

    private readonly Dictionary<Weapon, List<SimEnemy?>> actors = [];
    private readonly Dictionary<Vector3, SimEnemy> shown = [];
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] lineHelpers = new SimEnemy?[2];
    private SimEnemy? strikeHelper;
    private SimEnemy? stackHelper;

    public void Spawn()
    {
        actors.Clear();
        shown.Clear();
        actors[Weapon.Axe] = Enumerable.Range(0, PerKind).Select(_ => SpawnWeapon(BNpcBaseId.Axe)).ToList();
        actors[Weapon.Scythe] = Enumerable.Range(0, PerKind).Select(_ => SpawnWeapon(BNpcBaseId.Scythe)).ToList();
        actors[Weapon.Sword] = Enumerable.Range(0, PerKind).Select(_ => SpawnWeapon(BNpcBaseId.Sword)).ToList();
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper();
        for (var i = 0; i < lineHelpers.Length; i++) lineHelpers[i] = SpawnHelper();
        strikeHelper = SpawnHelper();
        stackHelper = SpawnHelper();
    }

    public static float Bearing(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    public void Show(Weapon kind, Vector3 spot)
    {
        if (actors[kind].FirstOrDefault(a => a != null && !shown.ContainsValue(a)) is not { } weapon) return;
        shown[spot] = weapon;
        weapon.SetPosition(new Placement(spot, Bearing(Vector3.Zero, spot)));
        weapon.SetVisible(true);
        weapon.PlayActionTimeline(kind switch
        {
            Weapon.Axe => TimelineId.AxeAppears,
            Weapon.Scythe => TimelineId.ScytheAppears,
            _ => TimelineId.SwordAppears,
        });
    }

    public void Hide(Vector3 spot)
    {
        if (!shown.Remove(spot, out var weapon)) return;
        weapon.SetVisible(false);
    }

    // `arrival` is the bearing The Tyrant dashes in on; the weapon's area turns with it.
    public void Strike(Weapon kind, Vector3 spot, float arrival)
    {
        tyrant()?.Face(spot);
        tyrant()?.Cast(kind switch
        {
            Weapon.Axe => ActionId.DashToAxe,
            Weapon.Scythe => ActionId.DashToScythe,
            _ => ActionId.DashToSword,
        }, spot, animationLock: 0.2f);
        tyrant()?.MoveTo(spot, DashSpeed);
        if (shown.TryGetValue(spot, out var weapon))
            weapon.PlayActionTimeline(kind switch
            {
                Weapon.Axe => TimelineId.AxeStrikes,
                Weapon.Scythe => TimelineId.ScytheStrikes,
                _ => TimelineId.SwordStrikes,
            });
        strikeHelper?.SetPosition(new Placement(spot, arrival));
        strikeHelper?.Cast(kind switch
        {
            Weapon.Axe => Actions.AxeStrike,
            Weapon.Scythe => Actions.ScytheStrike,
            _ => Actions.SwordStrike,
        });
    }

    public void FollowUp(Weapon kind, Vector3 spot, PartyRole heavyWeightTarget)
    {
        switch (kind)
        {
            case Weapon.Axe:
                var target = Alive(heavyWeightTarget) ?? AliveMembers().FirstOrDefault();
                if (target == null) break;
                stackHelper?.SetPosition(new Placement(spot, 0f));
                stackHelper?.Cast(Actions.HeavyWeight, target);
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
                for (var p = 0; p < LightParties.Length; p++)
                {
                    var bait = Alive(LightParties[p][1]) ?? LightParties[p].Select(Alive).FirstOrDefault(m => m != null);
                    if (bait == null) continue;
                    lineHelpers[p]?.SetPosition(new Placement(spot, 0f));
                    lineHelpers[p]?.Cast(Actions.SharpTaste, bait);
                }
                break;
        }
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

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
}
