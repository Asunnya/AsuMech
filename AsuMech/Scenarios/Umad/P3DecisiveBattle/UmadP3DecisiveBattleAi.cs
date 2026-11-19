using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Umad.P3DecisiveBattle.UmadP3DecisiveBattleScenario;

namespace AsuMech.Scenarios.Umad.P3DecisiveBattle;

public sealed class UmadP3DecisiveBattleAi : IScenarioAi<UmadP3DecisiveBattleState>
{
    public string Name => "Tank LB3 BoA";

    private const float OutsideThePbaoe = 17.5f;
    private const float ConeMargin = 0.21f;

    private static readonly PartyRole[] ChaosGroup = [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];
    private static readonly PartyRole[] BaiterPreference = [PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];

    private UmadP3DecisiveBattleState state = null!;
    private SimWorld world = null!;

    public void Run(UmadP3DecisiveBattleState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(StandByYourHeroBoss), jitter: 0f);
        ai.Move(DecisiveBattleAt + 1f, () => ByRole(role => Cluster(new Vector2(0f, 2.5f), role)), jitter: 0f, arrivalTime: BowelsCastAt);
        ai.Move(BowelsAt + 0.3f, () => ByRole(role => ElementSpot(role, state.FireShort, holdExdeath: true)), jitter: 0f, arrivalTime: PbaoeCastAt);
        ai.Move(PbaoeCastAt + 0.2f, () => ByRole(role => ElementSpot(role, state.FireShort, holdExdeath: false)), jitter: 0f, arrivalTime: ShortDebuffsAt - 0.6f);
        ai.Move(ShortCrystalAt + 0.3f, () => ByRole(TakeTheBusterOrSwapCrystals), jitter: 0f, arrivalTime: BusterAt[0] - 0.8f);
        ai.Move(ImplosionCastAt + 0.1f, () => ByRole(role => DodgeImplosion(role, firstHit: true)), jitter: 0f, arrivalTime: ShockwaveAt[0] - 0.5f);
        ai.Move(ShockwaveAt[0] + 0.1f, () => ByRole(role => DodgeImplosion(role, firstHit: false)), jitter: 0f, arrivalTime: ShockwaveAt[1] - 0.3f);
        ai.Move(ShockwaveAt[1] + 0.1f, () => ByRole(role => ElementSpot(role, !state.FireShort, holdExdeath: false)), jitter: 0f, arrivalTime: LongDebuffsAt - 0.3f);
    }

    private static Vector2 Cluster(Vector2 centre, PartyRole role)
    {
        var angle = (int)role * MathF.PI / 4f;
        return centre + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * 0.6f;
    }

    private static Vector2 StandByYourHeroBoss(PartyRole role)
        => Cluster(new Vector2(ChaosGroup.Contains(role) ? -4.5f : 4.5f, 0f), role);

    private static Vector2 Toward(int spot, float radius) => UmadP3DecisiveBattleState.Intercardinal(spot) * radius;

    private static Vector2 LeftLookingIn(int spot)
    {
        var outward = UmadP3DecisiveBattleState.Intercardinal(spot);
        return new Vector2(outward.Y, -outward.X);
    }

    private IReadOnlyList<PartyRole> CrystalBaiters(bool fire)
        => BaiterPreference.Where(r => !state.HoldsElement(fire, r)).Take(2).ToList();

    private Vector2 ElementSpot(PartyRole role, bool fire, bool holdExdeath)
    {
        var baiters = CrystalBaiters(fire);
        var crystal = fire ? state.FireSpot : state.WaterSpot;
        if (baiters.Contains(role))
        {
            var across = LeftLookingIn(crystal) * (fire ? 0.3f : 3.5f) * (baiters[0] == role ? 1f : -1f);
            return Toward(crystal, OutsideThePbaoe) + across;
        }
        if (holdExdeath && role == PartyRole.OffTank) return new Vector2(-1f, 1.6f);
        var group = Toward(state.WindSpot, OutsideThePbaoe);
        if (fire && state.HoldsEntropy(role))
            return group + LeftLookingIn(state.WindSpot) * (role.IsDps() ? -7f : 7f);
        return Cluster(group, role);
    }

    private Vector2 TakeTheBusterOrSwapCrystals(PartyRole role)
        => role == PartyRole.OffTank ? new Vector2(-1f, 1.6f) : ElementSpot(role, !state.FireShort, holdExdeath: false);

    private Vector2 DodgeImplosion(PartyRole role, bool firstHit)
    {
        var wanted = role == PartyRole.OffTank && firstHit ? new Vector2(-1f, 1.6f) : ElementSpot(role, !state.FireShort, holdExdeath: false);
        var sidesNow = firstHit == state.Latitudinal;
        var chaos = Toward(state.WindSpot, ChaosAtWall);
        var facing = UmadP3DecisiveBattleState.Intercardinal(state.WindSpot);
        if (!InShockwave(wanted, chaos, facing, sidesNow)) return wanted;
        var best = wanted;
        var bestDistance = float.MaxValue;
        for (var x = -19f; x <= 19f; x += 0.5f)
            for (var z = -19f; z <= 19f; z += 0.5f)
            {
                var spot = new Vector2(x, z);
                if (spot.Length() > 18.8f || Vector2.Distance(spot, chaos) < 3f || InShockwave(spot, chaos, facing, sidesNow)) continue;
                var distance = Vector2.Distance(spot, wanted);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = spot;
            }
        return best;
    }

    private static bool InShockwave(Vector2 spot, Vector2 chaos, Vector2 facing, bool sides)
    {
        var offset = spot - chaos;
        if (offset.LengthSquared() < 0.01f) return true;
        var along = MathF.Abs(Vector2.Dot(Vector2.Normalize(offset), facing));
        var angleFromAxis = MathF.Acos(Math.Clamp(along, 0f, 1f));
        return sides ? angleFromAxis >= MathF.PI / 4f - ConeMargin : angleFromAxis <= MathF.PI / 4f + ConeMargin;
    }

    private static IAiMove ByRole(Func<PartyRole, Vector2> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(r => (Vector2?)at(r)).ToArray()).NaturalOrder();
}
