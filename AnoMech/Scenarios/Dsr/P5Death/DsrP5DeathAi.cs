using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P1Knights;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P5Death;

public sealed class DsrP5DeathAi : IScenarioAi<DsrP5DeathState>
{
    public string Name => "NAUR (conga, dooms anchor)";

    private const float D = DsrP5DeathScenario.Death;
    private static readonly PartyRole[] RoleCongaLine =
        [PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps, PartyRole.MainTank, PartyRole.OffTank, PartyRole.CasterDps, PartyRole.MeleeDpsB, PartyRole.ShieldHealer];
    private const float CongaSpacing = 1.5f;
    private const float CongaLineA = -2f;
    private const float StepAside = 1.5f;
    private const float OuterDoomRadius = 14f;
    private const float WallRadius = 20f;
    private const float IntercardRadius = 19.5f;
    private const float IntercardOffset = 40f;
    private const float OuterDoomAnchorRadius = 4f;
    private const float AnchorRadius = 2.5f;

    private DsrP5DeathState state = null!;
    private SimWorld world = null!;
    private PartyRole[] CongaLine = RoleCongaLine;

    public void Run(DsrP5DeathState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        CongaLine = CongaLineWithThePlayerAt(state.PlayerCongaSlot, world.Party.PlayerRole);
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(MainTankSouthPartyNorth()).NaturalOrder());
        ai.Move(D + 9.0f, () => AiMove.Create(LineUpFacingTheWarrior()).NaturalOrder(), jitter: 0f);
        ai.Move(D + 11.9f, () => AiMove.Create(DoomsStepTowardTheWarrior()).NaturalOrder(), jitter: 0f);
        ai.Move(D + 13.5f, () => AiMove.Create(SpreadToDiveSafeSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP5DeathScenario.LightningLands + 0.08f, () => AiMove.Create(StepOffTwistersAfterLightning()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP5DeathScenario.Earthquakes[2] + 0.1f, () => AiMove.Create(WallNonDoomsInBehindTheThirdQuake()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP5DeathScenario.Earthquakes[3] + 0.07f, () => AiMove.Create(IntercardNonDoomsInBehindTheFourthQuake()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(D + 27.0f, () => AiMove.Create(AnchorForKnockback()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(DsrP5DeathScenario.SymbolsMarked + 0.2f, () => AiMove.Create(TakeSymbolSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        world.Events.Add(DsrP5DeathScenario.Knockback - 0.15f, FaceAwayFromBothGazes);
        ai.Move(DsrP5DeathScenario.Knockback + 0.9f, () => AiMove.Create(DoomsIntoPuddles()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(D + 38.5f, () => AiMove.Create(Stacked(Vector2.Zero)).NaturalOrder());
    }

    public static PartyRole[] CongaLineWithThePlayerAt(int? slot, PartyRole player)
    {
        if (slot is not { } at || at is < 0 or > 7) return RoleCongaLine;
        var line = RoleCongaLine.Where(r => r != player).ToList();
        line.Insert(at, player);
        return line.ToArray();
    }

    private static Vector2?[] MainTankSouthPartyNorth()
    {
        var spots = Stacked(new Vector2(0f, -4f));
        spots[(int)PartyRole.MainTank] = new Vector2(0f, 4f);
        return spots;
    }

    private Vector2?[] LineUpFacingTheWarrior()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < CongaLine.Length; i++)
            spots[(int)CongaLine[i]] = Local(CongaLineA, CongaSpacing * (i - 3.5f));
        return spots;
    }

    private Vector2?[] DoomsStepTowardTheWarrior()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < CongaLine.Length; i++)
        {
            var role = CongaLine[i];
            spots[(int)role] = Local(CongaLineA + (state.IsDoomed(role) ? StepAside : -StepAside), CongaSpacing * (i - 3.5f));
        }
        return spots;
    }

    private List<PartyRole> DoomsInLine => CongaLine.Where(state.IsDoomed).ToList();

    private List<PartyRole> NonDoomsInLine => CongaLine.Where(r => !state.IsDoomed(r)).ToList();

    private static float Side(int indexInFour) => indexInFour < 2 ? -1f : 1f;

    private static bool IsOuter(int indexInFour) => indexInFour is 0 or 3;

    private Vector2?[] SpreadToDiveSafeSpots()
    {
        var spots = new Vector2?[8];
        var dooms = DoomsInLine;
        for (var i = 0; i < dooms.Count; i++)
            spots[(int)dooms[i]] = IsOuter(i) ? Local(0f, Side(i) * OuterDoomRadius) : WarriorFrame(IntercardRadius, Side(i) * IntercardOffset);
        var nonDooms = NonDoomsInLine;
        for (var i = 0; i < nonDooms.Count; i++)
            spots[(int)nonDooms[i]] = IsOuter(i) ? Local(0f, Side(i) * WallRadius) : WarriorFrame(IntercardRadius, 180f - Side(i) * IntercardOffset);
        return spots;
    }

    private Vector2?[] StepOffTwistersAfterLightning()
    {
        var spots = new Vector2?[8];
        var dooms = DoomsInLine;
        for (var i = 0; i < dooms.Count; i++)
            spots[(int)dooms[i]] = IsOuter(i) ? Local(4f, Side(i) * 10f) : Local(11f, Side(i) * 8f);
        var nonDooms = NonDoomsInLine;
        for (var i = 0; i < nonDooms.Count; i++)
            spots[(int)nonDooms[i]] = IsOuter(i) ? Local(-3f, Side(i) * 19.5f) : Local(-16.5f, Side(i) * 10f);
        return spots;
    }

    private Vector2?[] WallNonDoomsInBehindTheThirdQuake()
    {
        var spots = new Vector2?[8];
        var nonDooms = NonDoomsInLine;
        for (var i = 0; i < nonDooms.Count; i++)
            if (IsOuter(i)) spots[(int)nonDooms[i]] = Local(-3.5f, Side(i) * 12.5f);
        return spots;
    }

    private Vector2?[] IntercardNonDoomsInBehindTheFourthQuake()
    {
        var spots = new Vector2?[8];
        var nonDooms = NonDoomsInLine;
        for (var i = 0; i < nonDooms.Count; i++)
            if (!IsOuter(i)) spots[(int)nonDooms[i]] = Local(-12f, Side(i) * 10f);
        return spots;
    }

    private float DoomAnchorBearing(PartyRole doom)
    {
        var i = DoomsInLine.IndexOf(doom);
        return IsOuter(i) ? Side(i) * 90f : 180f - Side(i) * 45f;
    }

    private Vector2?[] AnchorForKnockback()
    {
        var spots = new Vector2?[8];
        var dooms = DoomsInLine;
        for (var i = 0; i < dooms.Count; i++)
            spots[(int)dooms[i]] = WarriorFrame(IsOuter(i) ? OuterDoomAnchorRadius : AnchorRadius, DoomAnchorBearing(dooms[i]));
        var nonDooms = NonDoomsInLine;
        for (var i = 0; i < nonDooms.Count; i++)
            spots[(int)nonDooms[i]] = Local(0.5f * (i - 1.5f), 0f);
        return spots;
    }

    private Vector2?[] TakeSymbolSpots()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (state.IsDoomed(role))
            {
                spots[(int)role] = WarriorFrame(DoomsInLine.IndexOf(role) is 0 or 3 ? OuterDoomAnchorRadius : AnchorRadius, DoomAnchorBearing(role));
                continue;
            }
            if (!state.Symbols.TryGetValue(role, out var symbol)) continue;
            var partner = state.Symbols.Where(kv => kv.Value == symbol && kv.Key != role).Select(kv => (PartyRole?)kv.Key).FirstOrDefault();
            if (partner is { } doom && state.IsDoomed(doom))
            {
                spots[(int)role] = WarriorFrame(AnchorRadius, DoomAnchorBearing(doom) + 180f);
                continue;
            }
            var first = partner is not { } other || role < other;
            spots[(int)role] = WarriorFrame(AnchorRadius, first ? 0f : 180f);
        }
        return spots;
    }

    private Vector2?[] DoomsIntoPuddles()
    {
        var spots = new Vector2?[8];
        foreach (var role in state.Dooms)
        {
            if (world.Party.Get(role) is not { } member || state.Puddles.Count == 0) continue;
            var at = Flat(member.Position);
            spots[(int)role] = state.Puddles.Select(Flat).OrderBy(p => Vector2.Distance(p, at)).First();
        }
        return spots;
    }

    private void FaceAwayFromBothGazes()
    {
        var sources = new[] { Flat(state.ThordanSpot), Flat(state.EyeSpot) };
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (world.Party.Get(role) is not { } member || !world.Party.IsBotDriven(member)) continue;
            var at = Flat(member.Position);
            var bestFacing = 0f;
            var bestMargin = -1f;
            for (var degrees = 0f; degrees < 360f; degrees += 5f)
            {
                var facing = degrees * MathF.PI / 180f;
                var forward = new Vector2(MathF.Sin(facing), MathF.Cos(facing));
                var margin = sources.Min(source => MathF.Acos(Math.Clamp(Vector2.Dot(forward, Vector2.Normalize(source - at)), -1f, 1f)));
                if (margin <= bestMargin) continue;
                bestMargin = margin;
                bestFacing = facing;
            }
            member.SetRotation(bestFacing);
        }
    }

    private Vector2 WarriorFrame(float radius, float bearing) => Flat(AtBearing(state.WarriorBearing + bearing, radius));

    private Vector2 Local(float towardWarrior, float east)
    {
        var u = Flat(AtBearing(state.WarriorBearing, 1f));
        var v = Flat(AtBearing(state.WarriorBearing + 90f, 1f));
        return u * towardWarrior + v * east;
    }

    private static Vector2?[] Stacked(Vector2 centre) =>
        Enumerable.Range(0, 8).Select(i => (Vector2?)(centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1)))).ToArray();

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
