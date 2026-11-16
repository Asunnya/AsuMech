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

namespace AsuMech.Scenarios.M12s.ReplicationOne;

// t=0 = P2 0:07.5, 3s before Arcadia Aflame. Replication's four clones on the cardinals: two cleave,
// one slams fire on the nearest player, one casts dark on the two nearest. Snaking Kick, then the
// clones split into a fire-dark-fire-dark line on one diagonal and do it again; Double Sobat ends it.
public sealed class M12sReplicationOneScenario : IScenario
{
    public string Name => "Replication 1";
    public IPhase Phase => M12sZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sReplicationOneAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sReplicationOneSettingsWindow settingsWindow = new();

    public const float ArcadiaCastAt = 3f;
    public const float BossDashAt = 15.25f;
    public const float ReplicationCastAt = 17.38f;
    public const float FirstClonesAppearAt = 26.7f;
    public const float FirstCastAt = 28.85f;
    public const float FirstConesCastAt = 28.95f;
    public const float FirstFireAt = 32.11f;
    public const float SnakingKickCastAt = 32.73f;
    public const float FirstDarkAt = 33.08f;
    public const float SnakingKickAt = 37.73f;
    public const float SplitAt = 39.13f;
    public const float ElementClonesDashAt = 46.24f;
    public const float CleaveClonesDashAt = 47.26f;
    public const float SecondCastAt = 50.37f;
    public const float SecondConesCastAt = 50.41f;
    public const float SecondFireAt = 53.62f;
    public const float SecondDarkAt = 54.64f;
    public const float SobatMarkedAt = 56.81f;
    public const float SobatCastAt = 56.9f;
    public const float ClonesVanishAt = 57.5f;
    public const float SobatKickAt = 62.45f;
    public const float SobatRepeatAt = 67.05f;
    public const float EsotericAt = 69.48f;
    private const float DashSeconds = 0.9f;
    private const float SplitOffset = 5f;

    private SimWorld world = null!;
    private M12sReplicationOneState state = null!;
    private SimEnemy? lindwurm;
    private SimEnemy? fireClone;
    private SimEnemy? darkClone;
    private readonly List<SimEnemy?> cleaveClones = [];
    private readonly List<SimEnemy?> secondFireClones = [];
    private readonly List<SimEnemy?> secondDarkClones = [];
    private readonly List<SimEnemy?> secondCleaveClones = [];
    private readonly List<SimEnemy?> coneHelpers = [];
    private readonly List<SimEnemy?> spreadHelpers = [];
    private SimEnemy? repeatHelper;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sReplicationOneState(world.Rng, settingsWindow.Overrides);
        fireClone = darkClone = null;
        cleaveClones.Clear();
        secondFireClones.Clear();
        secondDarkClones.Clear();
        secondCleaveClones.Clear();
        coneHelpers.Clear();
        spreadHelpers.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sReplicationOneState>)AiStrats[idx]).Run(state, world);

        // UNVERIFIED: whether retail's edge kills or walls.
        world.Events.Add(0f, () => world.EnforceArenaBoundary(new CircleArena(Geometry.P2ArenaRadius), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(DnP2Waymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(ArcadiaCastAt, () => lindwurm?.Cast(Actions.ArcadiaAflame));
        world.Events.Add(BossDashAt, DashToTheCentre);
        world.Events.Add(ReplicationCastAt, () => lindwurm?.Cast(ActionId.Replication, animationLock: 1.1f));
        world.Events.Add(FirstClonesAppearAt, SpawnFirstClones);
        world.Events.Add(FirstCastAt, FirstClonesCast);
        world.Events.Add(FirstConesCastAt, () => Cleave(state.CleaveClones));
        world.Events.Add(FirstFireAt, () => SlamFire(fireClone));
        world.Events.Add(SnakingKickCastAt, SnakingKick);
        world.Events.Add(FirstDarkAt, () => CastDark(darkClone, 0));
        world.Events.Add(SplitAt, Split);
        world.Events.Add(ElementClonesDashAt, DashElementClones);
        world.Events.Add(CleaveClonesDashAt, DashCleaveClones);
        world.Events.Add(SecondCastAt, SecondClonesCast);
        world.Events.Add(SecondConesCastAt, () => Cleave(state.SecondCleaveClones));
        world.Events.Add(SecondFireAt, () => secondFireClones.ForEach(SlamFire));
        world.Events.Add(SecondDarkAt, () => { CastDark(secondDarkClones[0], 0); CastDark(secondDarkClones[1], 2); });
        world.Events.Add(SobatMarkedAt, () => Alive(PartyRole.MainTank)?.AttachLockonVfx(LockonId.DoubleSobat));
        world.Events.Add(SobatCastAt, () => lindwurm?.Cast(Actions.DoubleSobat));
        world.Events.Add(ClonesVanishAt, DespawnSecondClones);
        world.Events.Add(SobatKickAt, DoubleSobatKick);
        world.Events.Add(EsotericAt, EsotericFinisher);
    }

    public void Tick(float delta, float elapsed)
    {
    }

    private void SpawnActors()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP2,
            NameId: BNpcNameId.LindwurmP2,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(new Vector3(0f, 0f, 3f), MathF.PI)));
        for (var i = 0; i < 8; i++) coneHelpers.Add(SpawnHelper());
        for (var i = 0; i < 4; i++) spreadHelpers.Add(SpawnHelper());
        repeatHelper = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.LindwurmP2,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private SimEnemy? SpawnClone(Vector3 at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Lindschrat,
        NameId: BNpcNameId.Lindschrat,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Placement: new Placement(at, 0f)));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private IEnumerable<SimCharacter> NearestTo(Vector3 point)
        => world.Party.Find.InsideCircle(point, 100f).OrderBy(m => Vector3.DistanceSquared(m.Position, point));

    private static float Bearing(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    private void DashToTheCentre()
    {
        lindwurm?.Cast(ActionId.Dash, Vector3.Zero, animationLock: 0.2f);
        lindwurm?.MoveTo(Vector3.Zero, speed: 20f, finalRotation: MathF.PI);
    }

    private void SpawnFirstClones()
    {
        fireClone = SpawnClone(state.FireClone);
        darkClone = SpawnClone(state.DarkClone);
        foreach (var at in state.CleaveClones) cleaveClones.Add(SpawnClone(at));
    }

    private void FirstClonesCast()
    {
        fireClone?.Cast(ActionId.TopTierSlamCast, animationLock: 0.2f);
        darkClone?.Cast(ActionId.MightyMagicCast, animationLock: 0.2f);
        foreach (var clone in cleaveClones) clone?.Cast(CleaveCast, animationLock: 0.2f);
    }

    private uint CleaveCast => state.VerticalCleaves ? ActionId.WingedScourgeVertical : ActionId.WingedScourgeHorizontal;

    private void Cleave(IReadOnlyList<Vector3> clones)
    {
        var helper = 0;
        foreach (var at in clones)
            foreach (var heading in state.CleaveHeadings)
            {
                coneHelpers[helper]?.SetPosition(new Placement(at, heading));
                coneHelpers[helper++]?.Cast(Actions.WingedScourge);
            }
    }

    private void SlamFire(SimEnemy? clone)
    {
        if (clone is null || NearestTo(clone.Position).FirstOrDefault() is not { } target) return;
        clone.Cast(Actions.TopTierSlam, target);
        clone.MoveTo(target.Position, speed: 20f);
    }

    private void CastDark(SimEnemy? clone, int firstHelper)
    {
        if (clone is null) return;
        var helper = firstHelper;
        foreach (var target in NearestTo(clone.Position).Take(2))
        {
            spreadHelpers[helper]?.SetPosition(clone.Placement());
            spreadHelpers[helper++]?.Cast(Actions.MightyMagic, target);
        }
    }

    private void SnakingKick()
    {
        lindwurm?.SetRotation(state.SnakingKickHeading);
        lindwurm?.Cast(Actions.SnakingKick);
    }

    private void Split()
    {
        SplitInTwo(fireClone, secondFireClones);
        SplitInTwo(darkClone, secondDarkClones);
        foreach (var clone in cleaveClones) SplitInTwo(clone, secondCleaveClones);
        fireClone = darkClone = null;
        cleaveClones.Clear();
    }

    private void SplitInTwo(SimEnemy? clone, List<SimEnemy?> into)
    {
        if (clone is null) return;
        into.Add(SpawnClone(clone.Position - new Vector3(SplitOffset, 0f, 0f)));
        into.Add(SpawnClone(clone.Position + new Vector3(SplitOffset, 0f, 0f)));
        clone.Despawn();
    }

    private void DashElementClones()
    {
        DashPair(secondFireClones, state.OuterFire, state.InnerFire);
        DashPair(secondDarkClones, state.OuterDark, state.InnerDark);
    }

    // Each split clone takes the nearer of its element's two spots.
    private static void DashPair(List<SimEnemy?> pair, Vector3 a, Vector3 b)
    {
        if (pair is not [{ } first, { } second]) return;
        var straight = Vector3.Distance(first.Position, a) + Vector3.Distance(second.Position, b);
        var crossed = Vector3.Distance(first.Position, b) + Vector3.Distance(second.Position, a);
        if (crossed < straight)
        {
            pair[0] = second;
            pair[1] = first;
        }
        DashTo(pair[0], a);
        DashTo(pair[1], b);
    }

    private void DashCleaveClones()
    {
        var spots = state.SecondCleaveClones.ToList();
        foreach (var clone in secondCleaveClones)
        {
            if (clone is null) continue;
            var spot = spots.MinBy(s => Vector3.Distance(clone.Position, s));
            spots.Remove(spot);
            DashTo(clone, spot);
        }
    }

    private static void DashTo(SimEnemy? clone, Vector3 spot)
    {
        if (clone is null) return;
        clone.Cast(ActionId.Dash, spot, animationLock: 0.2f);
        clone.MoveTo(spot, speed: Vector3.Distance(clone.Position, spot) / DashSeconds, finalRotation: 0f);
    }

    private void SecondClonesCast()
    {
        foreach (var clone in secondFireClones) clone?.Cast(ActionId.TopTierSlamCast, animationLock: 0.2f);
        foreach (var clone in secondDarkClones) clone?.Cast(ActionId.MightyMagicCast, animationLock: 0.2f);
        foreach (var clone in secondCleaveClones) clone?.Cast(CleaveCast, animationLock: 0.2f);
    }

    private void DespawnSecondClones()
    {
        foreach (var clone in secondFireClones.Concat(secondDarkClones).Concat(secondCleaveClones)) clone?.Despawn();
        secondFireClones.Clear();
        secondDarkClones.Clear();
        secondCleaveClones.Clear();
    }

    private void DoubleSobatKick()
    {
        if (lindwurm is not { } boss || (Alive(PartyRole.MainTank) ?? Alive(PartyRole.OffTank)) is not { } tank) return;
        var follow = Bearing(boss.Position, tank.Position) + state.SobatTurns * MathF.PI / 2f;
        boss.Cast(Actions.DoubleSobatBuster(state.SobatTurns), tank);
        repeatHelper?.SetPosition(new Placement(boss.Position, follow));
        repeatHelper?.Cast(Actions.DoubleSobatRepeat);
        // UNVERIFIED: the boss turning to the follow-up; the four kicks' animations are the retail cue.
        world.Events.Add(0.6f, () => boss.TurnTo(follow));
    }

    private void EsotericFinisher()
    {
        if (lindwurm is not { } boss) return;
        var helper = 0;
        foreach (var role in new[] { PartyRole.MainTank, PartyRole.OffTank })
        {
            if (Alive(role) is not { } tank) continue;
            spreadHelpers[helper]?.SetPosition(boss.Placement());
            spreadHelpers[helper++]?.Cast(Actions.EsotericFinisher, tank);
        }
    }
}
