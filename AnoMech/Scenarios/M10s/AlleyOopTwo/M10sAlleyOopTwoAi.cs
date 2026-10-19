using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Scenarios.M10s.AlleyOopTwo.M10sAlleyOopTwoScenario;

namespace AnoMech.Scenarios.M10s.AlleyOopTwo;

public sealed class M10sAlleyOopTwoAi : IScenarioAi<M10sAlleyOopTwoState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 UnderDeepBlue = new(3f, -1.5f);
    private static readonly Vector2[] AroundRedHot = [new(-1.5f, -3.5f), new(-1f, -9f)];
    private static readonly Vector2 ClearOfThePuddles = new(-4f, 2f);
    private const float DodgeTurn = MathF.PI / 8f;
    private const float FarFromDeepBlue = 18f;
    private const float PushedBy = 6f;
    private const float PuddleMargin = 1.5f;
    private const float InsideTheWalls = 18.5f;
    private const float SearchStep = 0.5f;

    private M10sAlleyOopTwoState state = null!;

    public void Run(M10sAlleyOopTwoState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => OnYourMarker(role)));
        ai.Move(FirstConesAt + 0.25f, () => ByRole(role => state.DoubleDip ? Turned(OnYourMarker(role), DodgeTurn) : null), jitter: 0f, sprint: true);
        ai.Move(SecondConesAt + 0.2f, () => ByRole(role => UnderDeepBlue + StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(SteamHitAt + 0.2f, () => ByRole(role => role == PartyRole.MainTank ? BaitDeepImpactFar() : null), jitter: 0f, sprint: true);
        for (var i = 0; i < AroundRedHot.Length; i++)
        {
            var spot = AroundRedHot[i];
            ai.Move(PyrotationHitAt[i] + 0.1f, () => ByRole(role => role == PartyRole.MainTank ? null : spot + StackOffset(role)), jitter: 0f, sprint: true);
        }
        ai.Move(PyrotationHitAt[^1] + 0.5f, () => ByRole(role => role == PartyRole.MainTank ? null : ClearOfThePuddles + StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(DeepImpactHitAt + 1.5f, () => ByRole(role => role == PartyRole.MainTank ? ClearOfThePuddles + StackOffset(role) : null), jitter: 0f, sprint: true);
    }

    private static Vector2 OnYourMarker(PartyRole role)
    {
        var slot = role switch
        {
            PartyRole.MainTank => WaymarkSlot.A,
            PartyRole.OffTank => WaymarkSlot.C,
            PartyRole.RegenHealer => WaymarkSlot.D,
            PartyRole.ShieldHealer => WaymarkSlot.B,
            PartyRole.MeleeDpsA => WaymarkSlot.Four,
            PartyRole.MeleeDpsB => WaymarkSlot.Three,
            PartyRole.PhysRangedDps => WaymarkSlot.One,
            _ => WaymarkSlot.Two,
        };
        var at = ToxicWaymarks.First(w => w.Slot == slot).Offset;
        return new Vector2(at.X, at.Z);
    }

    private static Vector2 Turned(Vector2 spot, float clockwise)
    {
        var bearing = MathF.Atan2(spot.X, -spot.Y) + clockwise;
        return spot.Length() * new Vector2(MathF.Sin(bearing), -MathF.Cos(bearing));
    }

    private Vector2? BaitDeepImpactFar()
    {
        var boss = new Vector2(DeepBlueImpactSpot.X, DeepBlueImpactSpot.Z);
        Vector2? best = null;
        var bestDistance = float.MaxValue;
        for (var x = -InsideTheWalls; x <= InsideTheWalls; x += SearchStep)
        for (var z = -InsideTheWalls; z <= InsideTheWalls; z += SearchStep)
        {
            var spot = new Vector2(x, z);
            if (Vector2.Distance(spot, boss) < FarFromDeepBlue) continue;
            var landing = spot + Vector2.Normalize(spot - boss) * PushedBy;
            if (MathF.Abs(landing.X) > InsideTheWalls || MathF.Abs(landing.Y) > InsideTheWalls) continue;
            if (!ClearOfFire(spot) || !ClearOfFire((spot + landing) / 2f) || !ClearOfFire(landing)) continue;
            var distance = Vector2.Distance(spot, UnderDeepBlue);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = spot;
        }
        return best;
    }

    private bool ClearOfFire(Vector2 spot) =>
        state.InfernoPuddles.All(p => Vector2.Distance(new Vector2(p.X, p.Z), spot) >= PuddleRadius + PuddleMargin);

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
