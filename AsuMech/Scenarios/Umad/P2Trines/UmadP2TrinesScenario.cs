using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Umad.UmadConstants;

namespace AsuMech.Scenarios.Umad.P2Trines;

// Dancing Mad P2 after Forsaken: Trine, both Wings of Destruction, Ultimate Embrace and Aero III Assault.
// t=0 = pull +5:48.0 (Trine starts at 1.96).
public sealed class UmadP2TrinesScenario : IScenario
{
    public string Name => "Trines";
    public IPhase Phase => UmadZone.P2;
    public float BgmSecondsAtStart => 148.1f;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP2TrinesAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly UmadP2TrinesSettingsWindow settingsWindow = new();

    public const float TrineCastAt = 1.96f;
    public const float CleaveCastAt = 8.12f;
    public const float CleaveAt = 12.10f;
    public static readonly float[] TrineSpawnAt = [6.16f, 8.20f, 10.21f];
    public static readonly float[] TrineExplodeAt = [17.84f, 19.88f, 21.89f];
    public const float NearFarCastAt = 18.29f;
    public const float NearFarSnapshotAt = 21.89f;
    public const float NearFarAt = 22.33f;
    public const float EmbraceCastAt = 24.38f;
    public const float EmbraceAt = 29.34f;
    public const float AeroCastAt = 35.88f;
    public const float EndAt = 41f;

    public const float CircleRadius = 6f;

    // Each trine's centre, whether its lone circle points east, its layout and Arg2.
    public static readonly (Vector2 Centre, bool LoneEast, uint LayoutId, uint Arg2)[] Trines =
    [
        (new(0f, 0f), true, 0xBC17E1U, 0x00470003U),
        (new(2.89f, -15f), true, 0xBC17E6U, 0x00430003U),
        (new(11.55f, 10f), true, 0xBC17F1U, 0x00460003U),
        (new(-14.43f, 5f), true, 0xBC17EDU, 0x00450003U),
        (new(14.43f, -5f), false, 0xBC17E3U, 0x00420003U),
        (new(-11.55f, -10f), false, 0xBC17F5U, 0x00410003U),
        (new(-2.89f, 15f), false, 0xBC17E0U, 0x00440003U),
    ];

    public static IEnumerable<Vector2> Circles(int trine)
    {
        var (centre, loneEast, _, _) = Trines[trine];
        var east = loneEast ? 1f : -1f;
        yield return centre + new Vector2(east * 5.774f, 0f);
        yield return centre + new Vector2(-east * 2.887f, 5f);
        yield return centre + new Vector2(-east * 2.887f, -5f);
    }

    private SimWorld world = null!;
    private SimParty party = null!;
    private UmadP2TrinesState state = null!;
    private SimEnemy? kefka;
    private readonly SimEventObject?[] trineProps = new SimEventObject?[7];

    public UmadP2TrinesState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new UmadP2TrinesState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP2TrinesState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnKefka);
        world.Events.Add(TrineCastAt, () => kefka?.Cast(ActionId.Trine, animationLock: 3.1f));
        for (var s = 0; s < 3; s++)
        {
            var set = s;
            world.Events.Add(TrineSpawnAt[set], () => SpawnTrines(set));
            world.Events.Add(TrineSpawnAt[set] + 1.07f, () => BeatTrines(set, 0x10, 0x20));
            world.Events.Add(TrineSpawnAt[set] + 2.58f, () => BeatTrines(set, 0x40, 0x80));
            world.Events.Add(TrineExplodeAt[set] - 0.1f, () => BeatTrines(set, 0x4, 0x8));
            world.Events.Add(TrineExplodeAt[set], () => ExplodeTrines(set));
        }
        world.Events.Add(CleaveCastAt, CastHalfRoomWings);
        world.Events.Add(NearFarCastAt, () => kefka?.Cast(ActionId.WingsOfDestruction_NearFar, animationLock: 3.1f));
        world.Events.Add(NearFarSnapshotAt, BustNearAndFar);
        world.Events.Add(EmbraceCastAt, () => party.Get(PartyRole.MainTank)?.AttachLockonVfx(LockonId.UltimateEmbrace));
        world.Events.Add(EmbraceCastAt, () => kefka?.Cast(UmadActions.UltimateEmbrace, party.Get(PartyRole.MainTank)));
        world.Events.Add(AeroCastAt, () => kefka?.Cast(UmadActions.AeroIIIAssault));
        world.Events.Add(EndAt, () => { foreach (var prop in trineProps) prop?.Despawn(); });
    }

    private void SpawnKefka()
    {
        kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GodKefka, NameId: BNpcNameId.Kefka, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.GodKefka, Placement: new Placement(Vector3.Zero, MathF.PI)));
        kefka?.SetTarget(party.Get(PartyRole.MainTank), follow: false);
    }

    private void SpawnTrines(int set)
    {
        foreach (var trine in state.Set(set))
        {
            var (centre, loneEast, layoutId, arg2) = Trines[trine];
            trineProps[trine] = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = loneEast ? EObjId.TrineLoneEast : EObjId.TrineLoneWest, LayoutId = layoutId,
                EventId = EObjId.PropEventId, TargetableStatus = 5, Arg2 = arg2,
                Placement = new Placement(new Vector3(centre.X, 0f, centre.Y), 0f),
            });
        }
    }

    private void BeatTrines(int set, uint beat, uint mask)
    {
        foreach (var trine in state.Set(set)) trineProps[trine]?.PlayBeat(beat, mask);
    }

    private void ExplodeTrines(int set)
    {
        foreach (var trine in state.Set(set))
        {
            foreach (var circle in Circles(trine))
            {
                var at = new Vector3(circle.X, 0f, circle.Y);
                kefka?.CastFromHelper(UmadActions.TrineExplosion, at, at: new Placement(at, 0f));
            }
            trineProps[trine]?.Despawn();
            trineProps[trine] = null;
        }
    }

    // The wings sweep from the south wall over one half of the arena.
    private void CastHalfRoomWings()
    {
        var west = state.CleaveWest;
        kefka?.CastFromHelper(west ? UmadActions.WingsOfDestructionWest : UmadActions.WingsOfDestructionEast,
            at: new Placement(new Vector3(west ? -20f : 20f, 0f, 40f), MathF.PI));
    }

    // Picked as the last set goes off, landing a beat later on wherever they stand by then.
    private void BustNearAndFar()
    {
        var near = party.Find.Closest(Vector3.Zero);
        var far = party.Find.Farest(Vector3.Zero, near);
        world.Events.Add(NearFarAt - NearFarSnapshotAt, () =>
        {
            foreach (var target in new[] { near, far }.OfType<SimCharacter>().Where(t => t.IsAlive()))
                kefka?.CastFromHelper(UmadActions.WingsOfDestructionBuster, target);
        });
    }
}
