using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.P2MirrorMirror.FruP2MirrorMirrorState;

namespace AsuMech.Scenarios.Fru.P2MirrorMirror;

public sealed class FruP2MirrorMirrorAi : IScenarioAi<FruP2MirrorMirrorState>
{
    public string Name => "NA · LesBin";
    public string Group => "NA";

    private const float BaitDistance = 2.2f;
    private const float S = 1f / 2.358f;
    private static readonly Vector3 RayStackSpot = new(0, 0, -4);

    private FruP2MirrorMirrorState state = null!;
    private SimWorld world = null!;

    public void Run(FruP2MirrorMirrorState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(3f, () => ByRole(_ => RayStack()), arrivalTime: 6f);
        ai.Move(5.6f, () => ByRole(_ => RayStack()), arrivalTime: 6.9f);
        ai.Move(19f, () => ByRole(KickBait), jitter: 0f, arrivalTime: 24.5f);
        ai.Move(32.3f, () => ByRole(RedMirrorBait), jitter: 0f, arrivalTime: 38f);
        ai.Move(42.3f, () => ByRole(BanishSpot), arrivalTime: 46.2f);
    }

    private static bool BaitsWithBoss(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;

    private Vector3 RayStack()
        => world.Party.Get(state.RayTarget) is { } target && ReferenceEquals(target, world.Party.Player) ? target.Position : RayStackSpot;

    private Vector3? KickBait(PartyRole role) => BaitsWithBoss(role)
        ? Around(state.BossSpot, Vector3.Normalize(state.BossSpot), role switch
        {
            PartyRole.MainTank => 100,
            PartyRole.OffTank => -100,
            PartyRole.MeleeDpsA => 35,
            _ => -35,
        })
        : Around(state.Blue, -Vector3.Normalize(state.Blue), role switch
        {
            PartyRole.RegenHealer => 78,
            PartyRole.ShieldHealer => -78,
            PartyRole.PhysRangedDps => 45,
            _ => -45,
        });

    private int RangedRed()
    {
        var reds = state.RedOctants;
        return reds.OrderBy(r => OctantGap(r, state.BlueOctant)).ThenBy(r => (r - state.BlueOctant + 8) % 8).First();
    }

    private static int OctantGap(int a, int b) => Math.Min((a - b + 8) % 8, (b - a + 8) % 8);

    private Vector3? RedMirrorBait(PartyRole role)
    {
        var ranged = RangedRed();
        var mine = BaitsWithBoss(role) ? state.RedOctants.First(r => r != ranged) : ranged;
        var other = state.RedOctants.First(r => r != mine);
        var red = Point(mine, MirrorRadius);
        var inward = -Vector3.Normalize(red);
        var towardOther = Vector3.Dot(Point(other, MirrorRadius) - red, Right(inward)) > 0 ? 1 : -1;
        var slot = BaitsWithBoss(role)
            ? role switch { PartyRole.MainTank => 0, PartyRole.OffTank => 1, PartyRole.MeleeDpsA => 2, _ => 3 }
            : role switch { PartyRole.RegenHealer => 0, PartyRole.ShieldHealer => 1, PartyRole.PhysRangedDps => 2, _ => 3 };
        float[] awayFromOtherRedsCleave = [5, 75, -40, -75];
        return Around(red, inward, towardOther * awayFromOtherRedsCleave[slot]);
    }

    private static Vector3 Right(Vector3 facing) => new(-facing.Z, 0, facing.X);

    private static Vector3 Around(Vector3 source, Vector3 facing, float degrees)
    {
        var a = degrees * MathF.PI / 180f;
        return source + BaitDistance * (MathF.Cos(a) * facing + MathF.Sin(a) * Right(facing));
    }

    private static Vector3 FromSim(float north, float east) => new(east * S, 0, -north * S);

    private Vector3? BanishSpot(PartyRole role) => state.Pairs && (int)role < 4
        ? ClockSpot(role switch
        {
            PartyRole.MainTank => PartyRole.PhysRangedDps,
            PartyRole.OffTank => PartyRole.CasterDps,
            PartyRole.RegenHealer => PartyRole.MeleeDpsA,
            _ => PartyRole.MeleeDpsB,
        })
        : ClockSpot(role);

    private static Vector3? ClockSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => FromSim(18, 0),
        PartyRole.OffTank => FromSim(0, 18),
        PartyRole.RegenHealer => FromSim(0, -18),
        PartyRole.ShieldHealer => FromSim(-18, 0),
        PartyRole.MeleeDpsA => FromSim(-12, -12),
        PartyRole.MeleeDpsB => FromSim(-12, 12),
        PartyRole.PhysRangedDps => FromSim(12, -12),
        _ => FromSim(12, 12),
    };

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
