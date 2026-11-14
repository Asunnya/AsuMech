using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using State = AsuMech.Scenarios.Ucob.P4Adds.UcobP4AddsState;
using Scenario = AsuMech.Scenarios.Ucob.P4Adds.UcobP4AddsScenario;

namespace AsuMech.Scenarios.Ucob.P4Adds;

public sealed class UcobP4AddsTuuflessAi : IScenarioAi<State>, IUcobStrat
{
    public string Name => "Tuufless (Elemental)";
    public UcobMarkers Markers => UcobMarkers.Elemental;

    public static readonly Vector2 TopLeftNeurolink = UcobMarkers.Elemental.Place(AddsNeurolinks.North);
    public static readonly Vector2 TopRightNeurolink = UcobMarkers.Elemental.Place(AddsNeurolinks.West);
    public static readonly Vector2 BottomNeurolink = UcobMarkers.Elemental.Place(AddsNeurolinks.East);

    private static readonly Vector2 TwintaniaTankSpot = new(-3.5f, -15f);
    private static readonly Vector2 NaelTankSpot = new(3.5f, -15f);
    private static readonly Dictionary<PartyRole, Vector2> UptimeSpots = new()
    {
        [MeleeDpsA] = new(-1.5f, -7f), [MeleeDpsB] = new(1.5f, -7f),
        [RegenHealer] = new(-2f, -5f), [ShieldHealer] = new(2f, -5f),
        [CasterDps] = new(0f, -6f), [PhysRangedDps] = new(6f, 3f),
    };
    private static readonly Dictionary<PartyRole, Vector2> HealerEdgeSpots = new()
    {
        [RegenHealer] = new(-16.5f, -11f), [ShieldHealer] = new(16.5f, -11f),
    };
    private static readonly Dictionary<PartyRole, Vector2> UnmarkedDpsEdgeSpots = new()
    {
        [MeleeDpsA] = new(-11f, -17f), [MeleeDpsB] = new(11f, -17f),
        [PhysRangedDps] = new(-13f, 11f), [CasterDps] = new(0f, -19f),
    };
    private static readonly Vector2[] LiquidHellBaitPath = [.. new[] { 120f, 140f, 160f, 180f, 200f }.Select(d => OnRim(d, 20f)), new(-5f, 11f)];
    private static readonly Vector2 BottomNeurolinkWait = BottomNeurolink - new Vector2(AddsNeurolinks.Radius + 0.4f, 0f);
    private static readonly Vector2 TwisterStep = new(0f, 4f);
    private static readonly Dictionary<PartyRole, float> SpreadDegrees = new()
    {
        [MainTank] = -22.5f, [OffTank] = 22.5f, [MeleeDpsA] = -67.5f, [MeleeDpsB] = 67.5f,
        [RegenHealer] = -112.5f, [ShieldHealer] = 112.5f, [PhysRangedDps] = -157.5f, [CasterDps] = 157.5f,
    };
    private static readonly Dictionary<PartyRole, float> QuoteRingDegrees = new()
    {
        [MainTank] = -80f, [MeleeDpsA] = -57f, [RegenHealer] = -34f, [PhysRangedDps] = -11f,
        [CasterDps] = 11f, [ShieldHealer] = 34f, [MeleeDpsB] = 57f, [OffTank] = 80f,
    };
    private const float RavenDiveSpreadRadius = 9.2f;
    private const float TwisterSpreadRadius = 15.5f;
    private const float DynamoRingRadius = 3.5f;
    private const float ChariotRingRadius = 10f;
    private const float BaitLead = 3f;
    private const float StepOff = 0.08f;
    private const float AfterDrop = 0.02f;
    private const float AfterMove = 0.05f;
    private const float AfterQuote = 0.2f;
    private const float BackHatchResolved = 2f;

    private State state = null!;
    private SimWorld world = null!;

    public void Run(State stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(Uptime(0.3f)).NaturalOrder(), jitter: 0f);
        for (var set = 0; set < Scenario.GenerateCasts.Length; set++)
        {
            var s = set;
            var drops = Scenario.LiquidHellDrops[s];
            ai.Move(drops[0] - BaitLead, () => AiMove.Create(Only(PhysRangedDps, LiquidHellBaitPath[0])).NaturalOrder(), jitter: 0f, sprint: true);
            for (var i = 0; i < drops.Length; i++)
            {
                var next = LiquidHellBaitPath[i + 1];
                ai.Move(drops[i] + AfterDrop, () => AiMove.Create(Only(PhysRangedDps, next)).NaturalOrder(), jitter: 0f, sprint: true);
            }
            ai.Move(Scenario.GenerateCasts[s], () => AiMove.Create(HatchSpots(s)).NaturalOrder(), jitter: 0f, sprint: true);
            var hatchTwister = Scenario.TwisterSnapshots[2 * s];
            ai.Move(hatchTwister + StepOff, () => AiMove.Create(StepOffTwistersThenEnterTheBottomNeurolink(s)).NaturalOrder(), jitter: 0f, sprint: true);

            var moves = Scenario.QuoteMoves[s];
            var quote = state.NaelQuotes[s];
            var opener = quote.Moves[0];
            ai.Move(Scenario.Quotes[s] + AfterQuote, () => AiMove.Create(Without(BottomNeurolinkHolder(s), QuoteSpots(opener))).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(Scenario.Quotes[s] + BackHatchResolved, () => AiMove.Create(Only(BottomNeurolinkHolder(s), QuoteSpots(opener))).NaturalOrder(), jitter: 0f, sprint: true);
            for (var k = 1; k < quote.Moves.Length; k++)
            {
                var move = quote.Moves[k];
                ai.Move(moves[k - 1] + AfterMove, () => AiMove.Create(QuoteSpots(move)).NaturalOrder(), jitter: 0f, sprint: true);
            }
            var quoteTwister = Scenario.TwisterSnapshots[2 * s + 1];
            ai.Move(quoteTwister + StepOff, () => AiMove.Create(Spread(TwisterSpreadRadius)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(TwistersGone(quoteTwister), () => AiMove.Create(Uptime(world.Events.Elapsed)).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(Scenario.AggroSwap + AfterMove, () => AiMove.Create(Uptime(world.Events.Elapsed)).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private static Vector2 OnRim(float degreesFromNorth, float radius)
    {
        var radians = degreesFromNorth * MathF.PI / 180f;
        return new Vector2(radius * MathF.Sin(radians), -radius * MathF.Cos(radians));
    }

    private static float TwistersGone(float snapshot) => snapshot + AddsTwisters.Fades + AfterMove;

    private Vector2 TankSpot(PartyRole tank, float at) => tank == Scenario.TwintaniaHolder(state, at) ? TwintaniaTankSpot : NaelTankSpot;

    private Vector2?[] Uptime(float at)
    {
        var spots = new Vector2?[8];
        foreach (var (role, spot) in UptimeSpots) spots[(int)role] = spot;
        spots[(int)MainTank] = TankSpot(MainTank, at);
        spots[(int)OffTank] = TankSpot(OffTank, at);
        return spots;
    }

    private static Vector2?[] Only(PartyRole role, Vector2 spot)
    {
        var spots = new Vector2?[8];
        spots[(int)role] = spot;
        return spots;
    }

    private static Vector2?[] Only(PartyRole role, Vector2?[] from)
    {
        var spots = new Vector2?[8];
        spots[(int)role] = from[(int)role];
        return spots;
    }

    private static Vector2?[] Without(PartyRole role, Vector2?[] spots)
    {
        spots[(int)role] = null;
        return spots;
    }

    private static Vector2 HomeNeurolink(PartyRole role) => role switch
    {
        MeleeDpsA => TopLeftNeurolink,
        MeleeDpsB => TopRightNeurolink,
        _ => BottomNeurolink,
    };

    public static Dictionary<PartyRole, Vector2> HatchNeurolinks(IReadOnlyList<PartyRole> marked)
    {
        var claimed = marked.Where(r => r != CasterDps).ToDictionary(r => r, HomeNeurolink);
        if (marked.Contains(CasterDps))
            claimed[CasterDps] = HomeNeurolink(new[] { MeleeDpsA, MeleeDpsB, PhysRangedDps }.First(r => !marked.Contains(r)));
        return claimed;
    }

    private PartyRole BottomNeurolinkHolder(int set) => HatchNeurolinks(state.HatchTargets[set]).First(c => c.Value == BottomNeurolink).Key;

    private Vector2?[] HatchSpots(int set)
    {
        var at = world.Events.Elapsed;
        var spots = new Vector2?[8];
        spots[(int)MainTank] = TankSpot(MainTank, at);
        spots[(int)OffTank] = TankSpot(OffTank, at);
        foreach (var (role, spot) in HealerEdgeSpots) spots[(int)role] = spot;
        foreach (var (role, spot) in UnmarkedDpsEdgeSpots) spots[(int)role] = spot;
        foreach (var (role, neurolink) in HatchNeurolinks(state.HatchTargets[set]))
            spots[(int)role] = neurolink == BottomNeurolink ? BottomNeurolinkWait : neurolink;
        return spots;
    }

    private Vector2?[] StepOffTwistersThenEnterTheBottomNeurolink(int set)
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
            if (world.Party.Get(role) is { } member)
                spots[(int)role] = UcobSim.Flat(member.Position) + TwisterStep;
        spots[(int)BottomNeurolinkHolder(set)] = BottomNeurolink;
        return spots;
    }

    private Vector2 NaelPosition()
        => world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus) is { } nael
            ? UcobSim.Flat(nael.Position)
            : NaelTankSpot;

    private Vector2?[] QuoteSpots(NaelMove move) => move switch
    {
        NaelMove.LunarDynamo => RingAroundNael(DynamoRingRadius),
        NaelMove.IronChariot => RingAroundNael(ChariotRingRadius),
        NaelMove.ThermionicBeam => StackInTheMiddle(),
        _ => Spread(RavenDiveSpreadRadius),
    };

    private Vector2?[] RingAroundNael(float radius)
    {
        var nael = NaelPosition();
        var inward = MathF.Atan2(-nael.X, -nael.Y);
        var spots = new Vector2?[8];
        foreach (var (role, degrees) in QuoteRingDegrees)
        {
            var angle = inward + degrees * MathF.PI / 180f;
            spots[(int)role] = nael + radius * new Vector2(MathF.Sin(angle), MathF.Cos(angle));
        }
        return spots;
    }

    private static Vector2?[] Spread(float radius)
    {
        var spots = new Vector2?[8];
        foreach (var (role, degrees) in SpreadDegrees) spots[(int)role] = OnRim(degrees, radius);
        return spots;
    }

    private static Vector2?[] StackInTheMiddle()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < 8; i++) spots[i] = new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1));
        return spots;
    }
}
