using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Scenarios.M11s.UltimateWeapons.M11sUltimateWeaponsScenario;

namespace AnoMech.Scenarios.M11s.UltimateWeapons;

public sealed class M11sUltimateWeaponsAi : IScenarioAi<M11sUltimateWeaponsState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 TanksInFront = new(0f, -5f);
    private static readonly Vector2 PartyBehind = new(0f, 4f);
    private static readonly Vector2 WaitInTheMiddle = new(0f, 2f);

    private const float OutsideTheTornado = 4.5f;
    private const float BesideTheTornado = 6f;
    private const float AcrossThePair = 3f;
    private const float QuarterTurn = MathF.PI / 2f;
    private const float UnderTheScytheClearOfTheTornado = 4.5f;
    private const int Hops = 18;
    private const float HopSeconds = 0.2f;
    private const float RouteSpeed = 7f;
    private const float TornadoClearance = 5f;
    private const float StepOut = 6.5f;
    private const float RoundStep = MathF.PI / 6f;

    private M11sUltimateWeaponsState state = null!;

    public void Run(M11sUltimateWeaponsState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(TanksInFrontPartyBehind));
        ai.Move(EyeCastAt + 0.3f, () => ByRole(PairUpInTheGap), jitter: 0f);
        ai.Move(ExplosionCastAt + ExplosionResolves + 0.6f, () => ByRole(TanksInFrontPartyBehind));
        ai.Move(TrophyCastAt + 0.4f, () => ByRole(role => M11sOpenerAi.RawSteelSpot(role, state.AxeTrophy)), jitter: 0f);
        ai.Move(RawSteelHitAt + 0.8f, () => ByRole(role => WaitInTheMiddle + M11sOpenerAi.StackOffset(role)));
        ai.Move(WeaponAppearsAt[0] + 0.2f, () => ByRole(role => AroundTheWeapon(role, 0)), jitter: 0f);
        for (var i = 1; i < M11sUltimateWeaponsState.Strikes; i++)
        {
            var index = i;
            var leaveAt = StrikeAt[index - 1] + StrikeResolves + 0.1f;
            for (var h = 0; h < Hops; h++)
            {
                var walked = h * HopSeconds;
                ai.Move(leaveAt + walked, () => ByRole(role => Hop(AroundTheWeapon(role, index - 1), AroundTheWeapon(role, index), index - 1, walked)), jitter: 0f, sprint: true);
            }
        }
        var baitAt = StrikeAt[^1] + StrikeResolves + 0.1f;
        const int last = M11sUltimateWeaponsState.Strikes - 1;
        for (var h = 0; h < Hops; h++)
        {
            var walked = h * HopSeconds;
            ai.Move(baitAt + walked, () => ByRole(role => Hop(AroundTheWeapon(role, last), TornadoBait(role, BesideTheTornado, 0f), last, walked)), jitter: 0f, sprint: true);
        }
        ai.Move(baitAt + Hops * HopSeconds + 0.3f, () => ByRole(role => TornadoBait(role, AcrossThePair, OutsideTheTornado)), jitter: 0f);

        ai.Move(GustAt + 0.6f, () => ByRole(role => WaitInTheMiddle + M11sOpenerAi.StackOffset(role)));
    }

    private static Vector2? TanksInFrontPartyBehind(PartyRole role) =>
        (role.IsTank() ? TanksInFront : PartyBehind) + M11sOpenerAi.StackOffset(role);

    private Vector2? PairUpInTheGap(PartyRole role)
    {
        var southOpen = role switch
        {
            PartyRole.MainTank or PartyRole.MeleeDpsA => new Vector3(-9f, 0f, 0f),
            PartyRole.OffTank or PartyRole.MeleeDpsB => new Vector3(9f, 0f, 0f),
            PartyRole.RegenHealer or PartyRole.PhysRangedDps => new Vector3(-7.5f, 0f, 18f),
            _ => new Vector3(7.5f, 0f, 18f),
        };
        var p = state.FromSouthOpen(southOpen);
        return new Vector2(p.X, p.Z) + M11sOpenerAi.StackOffset(role);
    }

    private Vector2? AroundTheWeapon(PartyRole role, int index)
    {
        var kind = state.Weapons[index];
        var spot = state.WeaponSpot(index);
        var arrival = state.ArrivalBearing(index);
        if (kind == Weapon.Scythe && index >= M11sUltimateWeaponsState.FirstTornadoWeapon)
        {
            var under = M11sOpenerAi.AroundTheWeapon(role, kind, spot, arrival) - new Vector2(spot.X, spot.Z);
            return new Vector2(spot.X, spot.Z) + Vector2.Normalize(under) * UnderTheScytheClearOfTheTornado;
        }
        return M11sOpenerAi.AroundTheWeapon(role, kind, spot, arrival);
    }

    private Vector2? TornadoBait(PartyRole role, float across, float outward)
    {
        foreach (var tornado in state.TornadoSpots())
        {
            var bearing = MathF.Atan2(tornado.X, tornado.Z);
            var pair = BaitersOf(MathF.Round(bearing / QuarterTurn));
            var side = role == pair.First ? -1f : role == pair.Second ? 1f : 0f;
            if (side == 0f) continue;
            var at = new Vector2(tornado.X, tornado.Z) + outward * Toward(bearing) + across * Toward(bearing + side * QuarterTurn);
            return at;
        }
        return null;
    }

    private static (PartyRole First, PartyRole Second) BaitersOf(float quarter) => (((int)quarter % 4 + 4) % 4) switch
    {
        0 => (PartyRole.OffTank, PartyRole.MeleeDpsB),
        1 => (PartyRole.ShieldHealer, PartyRole.CasterDps),
        2 => (PartyRole.MainTank, PartyRole.PhysRangedDps),
        _ => (PartyRole.RegenHealer, PartyRole.MeleeDpsA),
    };

    private Vector2? Hop(Vector2? from, Vector2? to, int lastStrike, float walked)
    {
        if (from is not { } a || to is not { } b) return to;
        var reached = walked * RouteSpeed;
        var at = a;
        foreach (var point in AroundTheTornadoes(a, b, lastStrike))
        {
            reached -= Vector2.Distance(at, point);
            if (reached < 0f) return point;
            at = point;
        }
        return b;
    }

    private List<Vector2> AroundTheTornadoes(Vector2 from, Vector2 to, int lastStrike)
    {
        var tornadoes = state.TornadoSpots().Take(Math.Max(0, lastStrike - M11sUltimateWeaponsState.FirstTornadoWeapon + 1))
            .Select(t => new Vector2(t.X, t.Z)).ToList();
        var route = new List<Vector2>();
        var at = from;
        for (var round = 0; round < tornadoes.Count; round++)
        {
            var here = at;
            var blocking = tornadoes.Where(c => Crosses(here, to, c)).OrderBy(c => AlongTheWay(here, to, c)).ToList();
            if (blocking.Count == 0) break;
            var c = blocking[0];
            var ab = to - at;
            var out_ = Vector2.Distance(at, c) < StepOut ? at - c : at + AlongTheWay(at, to, c) * ab - c;
            if (out_.LengthSquared() < 0.01f) out_ = new Vector2(-ab.Y, ab.X);
            var angle = MathF.Atan2(out_.X, out_.Y);
            var goal = MathF.Atan2(to.X - c.X, to.Y - c.Y);
            var turn = MathF.IEEERemainder(goal - angle, MathF.Tau);
            at = c + StepOut * Toward(angle);
            route.Add(at);
            for (var step = 0; step < 6 && Crosses(at, to, c); step++)
            {
                angle += MathF.Sign(turn) * MathF.Min(RoundStep, MathF.Abs(turn));
                at = c + StepOut * Toward(angle);
                route.Add(at);
            }
        }
        route.Add(to);
        return route;
    }

    private static float AlongTheWay(Vector2 a, Vector2 b, Vector2 c)
    {
        var ab = b - a;
        return ab.LengthSquared() < 0.01f ? 0f : Math.Clamp(Vector2.Dot(c - a, ab) / ab.LengthSquared(), 0f, 1f);
    }

    private static bool Crosses(Vector2 a, Vector2 b, Vector2 c) => Vector2.Distance(a + AlongTheWay(a, b, c) * (b - a), c) < TornadoClearance;

    private static Vector2 Toward(float bearing) => new(MathF.Sin(bearing), MathF.Cos(bearing));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
