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

namespace AsuMech.Scenarios.M12s.Slaughtershed;

// Mortal Slayer to the P1 enrage, t=0 = P1 4:50. Mortal Slayer's eight orbs, then three
// Slaughtersheds: a raidwide, one role spreading while the other stacks, the blood vessels bursting,
// and the boss's arms (half-room cleaves or corner knockbacks) one side then the other. Ends on
// Refreshing Overkill resolving as the raidwide retail casts when the boss is pushed in time.
public sealed class M12sSlaughtershedScenario : IScenario
{
    public string Name => "Slaughtershed";
    public IPhase Phase => M12sZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sSlaughtershedAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sSlaughtershedSettingsWindow settingsWindow = new();

    public const float MortalSlayerCastAt = 3.47f;
    public static readonly float[] OrbsAppearAt = [4.31f, 6.83f, 9.26f, 12.06f];
    public static readonly float[] OrbsHitAt = [15.55f, 18.58f, 21.57f, 24.55f];
    private const float OrbLingers = 2.1f;

    public static readonly float[] SlaughtershedAt = [36.75f, 65.67f, 94.58f];
    public const float RaidwideAfter = 5.48f;
    public const float BlobsShowAfter = 6.02f;
    public const float MarkersAfter = 7.04f;
    public const float ArmsTellAfter = 7.13f;
    public const float BurstCastAfter = 12.13f;
    public const float StackSpreadAfter = 14.14f;
    public const float BurstHitsAfter = 14.6f;
    public static readonly float[] ArmAfter = [19.77f, 24.32f];
    public const float SerpentineHitsAfter = 0.98f;
    public const float KnucklesHitAfter = 0.76f;

    public const float RefreshingOverkillCastAt = 127.99f;
    public const float RefreshingOverkillHitsAt = 138.21f;
    private const float EndsAt = 141f;

    private const ushort BlobDormant = 1;
    private const float BlobPrimesBefore = 0.09f;
    private const float BlobLingers = 0.77f;
    private const float SerpentineX = 10f;
    private const float KnucklesX = 18f;
    private const float KnucklesZ = -11f;

    // The party zones in at the entrance, south of the stage; the edge kills once they have walked on.
    private const float BoundaryAt = 3.5f;

    private SimWorld world = null!;
    private M12sSlaughtershedState state = null!;
    private SimEnemy? lindwurm;
    private SimEnemy? raidwideHelper;
    private SimEnemy? armHelper;
    private readonly SimEnemy?[,] orbs = new SimEnemy?[M12sSlaughtershedState.Pairs, 2];
    private readonly SimEnemy?[] markerHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] burstHelpers = new SimEnemy?[5];
    private readonly List<SimEventObject> blobs = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sSlaughtershedState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        blobs.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sSlaughtershedState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(BoundaryAt, () => world.EnforceArenaBoundary(M12sZone.Instance.Arena, "Fell off the stage"));
        world.Events.Add(2.53f, () => lindwurm?.SetModelState(ModelState.MortalSlayer));
        world.Events.Add(MortalSlayerCastAt, () => lindwurm?.Cast(ActionId.MortalSlayer, animationLock: 0.2f));
        for (var p = 0; p < M12sSlaughtershedState.Pairs; p++)
        {
            var pair = p;
            world.Events.Add(OrbsAppearAt[pair], () => DropOrbs(pair));
            world.Events.Add(OrbsHitAt[pair], () => OrbsStrike(pair));
            world.Events.Add(OrbsHitAt[pair] + OrbLingers, () => ClearOrbs(pair));
        }
        world.Events.Add(29.4f, () => lindwurm?.SetModelState(ModelState.Idle));
        for (var r = 0; r < M12sSlaughtershedState.Rounds; r++)
        {
            var round = r;
            var at = SlaughtershedAt[round];
            world.Events.Add(at, () => lindwurm?.Cast(ActionId.SlaughtershedHeads, animationLock: 0.2f));
            world.Events.Add(at, () => SpawnBlobs(round));
            world.Events.Add(at + RaidwideAfter, () => raidwideHelper?.Cast(Actions.SlaughtershedRaidwide));
            world.Events.Add(at + RaidwideAfter, () => lindwurm?.SetModelState(ModelState.Slaughtershed));
            world.Events.Add(at + BlobsShowAfter, () => AnimateBlobs(0x10, 0x20));
            world.Events.Add(at + MarkersAfter, () => MarkStackAndSpreads(round));
            world.Events.Add(at + ArmsTellAfter, () => lindwurm?.Cast(ArmsTell(state.ArmsOf[round]), animationLock: 0.2f));
            world.Events.Add(at + 8.96f, () => lindwurm?.SetModelState(ModelState.Idle));
            world.Events.Add(at + BurstCastAfter - BlobPrimesBefore, () => AnimateBlobs(1, 2));
            world.Events.Add(at + BurstCastAfter, () => { foreach (var helper in burstHelpers) helper?.Cast(Actions.Burst); });
            world.Events.Add(at + StackSpreadAfter, () => StackAndSpreads(round));
            world.Events.Add(at + BurstHitsAfter, () => AnimateBlobs(4, 8));
            world.Events.Add(at + BurstHitsAfter + BlobLingers, ClearBlobs);
            for (var s = 0; s < 2; s++)
            {
                var second = s == 1;
                world.Events.Add(at + ArmAfter[s], () => SwingArm(state.ArmsOf[round], second));
            }
        }
        world.Events.Add(RefreshingOverkillCastAt, () => lindwurm?.Cast(ActionId.RefreshingOverkill, animationLock: 0.2f));
        world.Events.Add(RefreshingOverkillHitsAt, () => lindwurm?.Cast(Actions.RefreshingOverkillRaidwide));
        world.Events.Add(EndsAt, () => { });
    }

    private void SpawnActors()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP1,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Boss, 0f)));
        raidwideHelper = SpawnHelper(new Placement(Vector3.Zero, 0f));
        armHelper = SpawnHelper(default);
        for (var i = 0; i < markerHelpers.Length; i++) markerHelpers[i] = SpawnHelper(default);
        for (var i = 0; i < burstHelpers.Length; i++) burstHelpers[i] = SpawnHelper(default);
    }

    public static readonly Vector3 Boss = new(0f, 0f, -15f);

    private SimEnemy? SpawnHelper(Placement at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.LindwurmP1,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: at));

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private void DropOrbs(int pair)
    {
        for (var i = 0; i < 2; i++)
        {
            var orb = state.Orbs[pair][i];
            orbs[pair, i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: orb.Purple ? BNpcBaseId.PurpleOrb : BNpcBaseId.GreenOrb,
                NameId: BNpcNameId.LindwurmP1,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(state.OrbSpot(pair, i), 0f)));
        }
    }

    // Each falling orb takes the player nearest the boss on its side, the outermost orb the nearest.
    private void OrbsStrike(int pair)
    {
        foreach (var east in new[] { false, true })
        {
            var falling = state.FallingOn(pair, east);
            var closest = AliveMembers().Where(m => m.Position.X > 0f == east)
                .OrderBy(m => Vector3.Distance(m.Position, Boss)).ToList();
            for (var k = 0; k < falling.Count; k++)
            {
                var orb = orbs[pair, falling[k]];
                var purple = state.Orbs[pair][falling[k]].Purple;
                if (k < closest.Count)
                    orb?.Cast(purple ? Actions.MortalSlayerPurple : Actions.MortalSlayerGreen, closest[k]);
                else
                    orb?.Cast(purple ? Actions.ArmySlayerPurple : Actions.ArmySlayerGreen);
            }
        }
    }

    private void ClearOrbs(int pair)
    {
        for (var i = 0; i < 2; i++)
        {
            orbs[pair, i]?.Despawn();
            orbs[pair, i] = null;
        }
    }

    private void SpawnBlobs(int round)
    {
        var spots = state.Blobs(round);
        for (var i = 0; i < spots.Count && i < burstHelpers.Length; i++)
        {
            burstHelpers[i]?.SetPosition(new Placement(spots[i], 0f));
            if (world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.BloodVessel, Placement = new Placement(spots[i], 0f), TimelineState = BlobDormant }) is { } blob)
                blobs.Add(blob);
        }
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

    private void MarkStackAndSpreads(int round)
    {
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => state.Spreads(round, r)))
            Alive(role)?.AttachLockonVfx(LockonId.SlaughtershedSpread);
        Alive(state.StackTargets[round])?.AttachLockonVfx(LockonId.SlaughtershedStack);
    }

    private void StackAndSpreads(int round)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (Alive(role) is not { } member) continue;
            var helper = markerHelpers[(int)role];
            if (state.Spreads(round, role))
            {
                helper?.SetPosition(new Placement(member.Position, 0f));
                helper?.Cast(Actions.CurtainCallDramaticLysis, member);
            }
            else if (role == state.StackTargets[round])
            {
                helper?.SetPosition(new Placement(member.Position, 0f));
                helper?.Cast(Actions.CurtainCallFourthWallFusion, member);
            }
        }
    }

    private static uint ArmsTell(Arms arms) => (arms.Serpentine, arms.WestFirst) switch
    {
        (true, true) => ActionId.SerpentineScourgeWestEast,
        (true, false) => ActionId.SerpentineScourgeEastWest,
        (false, true) => ActionId.RaptorKnucklesWestEast,
        _ => ActionId.RaptorKnucklesEastWest,
    };

    public static int ArmSide(Arms arms, bool second) => arms.WestFirst != second ? -1 : 1;

    private void SwingArm(Arms arms, bool second)
    {
        var side = ArmSide(arms, second);
        if (arms.Serpentine)
        {
            lindwurm?.Cast(side < 0 ? ActionId.SerpentineScourgeWest : ActionId.SerpentineScourgeEast, animationLock: 0.2f);
            armHelper?.SetPosition(new Placement(new Vector3(side * SerpentineX, 0f, -15f), 0f));
            armHelper?.Cast(Actions.SerpentineScourge);
        }
        else
        {
            lindwurm?.Cast(side < 0 ? ActionId.RaptorKnucklesWest : ActionId.RaptorKnucklesEast, animationLock: 0.2f);
            armHelper?.SetPosition(new Placement(new Vector3(side * KnucklesX, 0f, KnucklesZ), 0f));
            armHelper?.Cast(Actions.RaptorKnuckles);
        }
    }
}
