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

namespace AsuMech.Scenarios.M12s.Opener;

// Lindwurm's opener: The Fixer, then Mortal Slayer's eight orbs falling in pairs behind the boss;
// each orb hits the player closest to the boss on its half of the arena.
// t=0 = P1 -0:02 (The Fixer starts at 0:00).
public sealed class M12sOpenerScenario : IScenario
{
    public string Name => "The Fixer to Mortal Slayer";
    public IPhase Phase => M12sZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sOpenerAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sOpenerSettingsWindow settingsWindow = new();

    public static readonly Vector3 BossSpot = new(0f, 0f, -15f);

    public const float FixerCastAt = 2f;
    public const float MortalSlayerCastAt = 20.33f;
    public static readonly float[] OrbsAppearAt = [21.04f, 23.82f, 26.36f, 28.66f];
    public static readonly float[] OrbsHitAt = [32.41f, 35.44f, 38.44f, 41.42f];
    private const float OrbLingers = 1.9f;

    private static readonly float[] OrbX = [15f, 14.5f, 10f, 5.5f];
    private static readonly float[] OrbZ = [-31f, -34f, -31f, -29f];

    private SimWorld world = null!;
    private M12sOpenerState state = null!;
    private SimEnemy? lindwurm;
    private readonly SimEnemy?[] orbs = new SimEnemy?[M12sOpenerState.Pairs * 2];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sOpenerState(world.Rng, settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sOpenerState>)AiStrats[idx]).Run(state, world);

        // The party walks in from the entrance, south of the arena.
        world.Events.Add(3f, () => world.EnforceArenaBoundary(new RectArena(Geometry.P1HalfWidth, Geometry.P1HalfDepth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnLindwurm);
        world.Events.Add(1f, TankTheBoss);
        world.Events.Add(FixerCastAt, () => lindwurm?.Cast(Actions.TheFixer));
        world.Events.Add(19.39f, () => lindwurm?.SetModelState(0x1F));
        world.Events.Add(MortalSlayerCastAt, () => lindwurm?.Cast(ActionId.MortalSlayer, animationLock: 0.2f));
        for (var p = 0; p < M12sOpenerState.Pairs; p++)
        {
            var pair = p;
            world.Events.Add(OrbsAppearAt[pair], () => DropOrbs(pair));
            world.Events.Add(OrbsHitAt[pair], () => OrbsStrike(pair));
            world.Events.Add(OrbsHitAt[pair] + OrbLingers, () => ClearOrbs(pair));
        }
        world.Events.Add(43.5f, () => lindwurm?.SetModelState(0));
    }

    public void Tick(float delta, float elapsed)
    {
    }

    private void SpawnLindwurm()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP1,
            NameId: BNpcNameId.LindwurmP1,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(BossSpot, 0f)));
    }

    private void TankTheBoss() => lindwurm?.SetTarget(Alive(PartyRole.MainTank), follow: false);

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveMembers() => Enum.GetValues<PartyRole>().Select(Alive).OfType<SimCharacter>().ToList();

    private void DropOrbs(int pair)
    {
        for (var i = 0; i < 2; i++)
        {
            var orb = state.Orbs[pair][i];
            var spot = OrbSpot(pair, i);
            orbs[pair * 2 + i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: orb.Purple ? BNpcBaseId.PurpleOrb : BNpcBaseId.GreenOrb,
                NameId: BNpcNameId.LindwurmP1,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(spot, 0f)));
        }
    }

    // The n-th orb of the fight on its side falls a little further in than the one before.
    private Vector3 OrbSpot(int pair, int index)
    {
        var orb = state.Orbs[pair][index];
        var n = Enumerable.Range(0, pair * 2 + index).Count(k => state.Orbs[k / 2][k % 2].East == orb.East);
        return new Vector3((orb.East ? 1f : -1f) * OrbX[n], 0f, OrbZ[n]);
    }

    private void OrbsStrike(int pair)
    {
        foreach (var east in new[] { false, true })
        {
            var falling = Enumerable.Range(0, 2).Where(i => state.Orbs[pair][i].East == east)
                .OrderByDescending(i => MathF.Abs(OrbSpot(pair, i).X)).ToList();
            var closest = AliveMembers().Where(m => m.Position.X > 0f == east)
                .OrderBy(m => Vector3.DistanceSquared(m.Position, BossSpot)).ToList();
            for (var k = 0; k < falling.Count; k++)
            {
                var orb = orbs[pair * 2 + falling[k]];
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
            orbs[pair * 2 + i]?.Despawn();
            orbs[pair * 2 + i] = null;
        }
    }
}
