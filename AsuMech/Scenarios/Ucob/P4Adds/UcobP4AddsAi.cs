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

public sealed class UcobP4AddsAi : IScenarioAi<UcobP4AddsState>
{
    public string Name => "Bosses between 1 and 3, stacks in the middle";

    private static readonly Vector2 NewNorth = Vector2.Normalize(new Vector2(4f, -1.5f));
    private static readonly Vector2 NewEast = new(-NewNorth.Y, NewNorth.X);

    private static readonly Vector2 TwintaniaTankSpot = Spot(-2f, 17f);
    private static readonly Vector2 NaelTankSpot = Spot(5f, 17f);
    private static readonly Dictionary<PartyRole, Vector2> UptimeSpots = new()
    {
        [MeleeDpsA] = Spot(-3f, 8f), [MeleeDpsB] = Spot(4f, 8.5f),
        [RegenHealer] = Spot(-6f, 3f), [ShieldHealer] = Spot(7f, 3f),
        [CasterDps] = Spot(0f, 2f), [PhysRangedDps] = Spot(0f, -5f),
    };
    private static readonly Vector2[] HatchSafeSpots = [Spot(-8.5f, 17f), Spot(-13.5f, 14f), Spot(-17.5f, 9.5f)];
    private static readonly Vector2[] LiquidHellBaitPath = [.. new[] { 230f, 210f, 190f, 170f, 150f }.Select(d => OnRim(d, 19f)), Spot(6f, -10f)];
    private static readonly Vector2 FarNeurolinkWait = AddsNeurolinks.West - NewNorth * (AddsTwisters.TriggerRadius + 0.7f);
    private static readonly Vector2 TwisterStep = -4f * NewNorth;
    private static readonly Dictionary<PartyRole, float> QuoteRingDegrees = new()
    {
        [MeleeDpsA] = -80f, [RegenHealer] = -57f, [CasterDps] = -34f, [MainTank] = -11f,
        [OffTank] = 11f, [PhysRangedDps] = 34f, [ShieldHealer] = 57f, [MeleeDpsB] = 80f,
    };
    private const float DynamoRingRadius = 3.5f;
    private const float OutRingRadius = 10f;
    private const float BaitLead = 3f;
    private const float StepOff = 0.08f;
    private const float AfterDrop = 0.02f;
    private const float AfterMove = 0.05f;
    private const float AfterQuote = 0.2f;

    private State state = null!;
    private SimWorld world = null!;

    public void Run(UcobP4AddsState stateParam, SimWorld worldParam)
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
            ai.Move(hatchTwister + StepOff, () => AiMove.Create(StepOffTwistersIntoTheFarNeurolink(s)).NaturalOrder(), jitter: 0f, sprint: true);

            var moves = Scenario.QuoteMoves[s];
            var quote = state.NaelQuotes[s];
            ai.Move(Scenario.Quotes[s] + AfterQuote, () => AiMove.Create(QuoteSpots(quote.Moves[0])).NaturalOrder(), jitter: 0f, sprint: true);
            for (var k = 1; k < quote.Moves.Length; k++)
            {
                var move = quote.Moves[k];
                ai.Move(moves[k - 1] + AfterMove, () => AiMove.Create(QuoteSpots(move)).NaturalOrder(), jitter: 0f, sprint: true);
            }
            var quoteTwister = Scenario.TwisterSnapshots[2 * s + 1];
            ai.Move(quoteTwister + StepOff, () => AiMove.Create(StepOffTwisters()).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(TwistersGone(quoteTwister), () => AiMove.Create(Uptime(world.Events.Elapsed)).NaturalOrder(), jitter: 0f, sprint: true);
        }
        ai.Move(Scenario.AggroSwap + AfterMove, () => AiMove.Create(Uptime(world.Events.Elapsed)).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private static Vector2 Spot(float right, float forward) => right * NewEast + forward * NewNorth;

    private static Vector2 OnRim(float degreesFromNewNorth, float radius)
    {
        var radians = degreesFromNewNorth * MathF.PI / 180f;
        return Spot(radius * MathF.Sin(radians), radius * MathF.Cos(radians));
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

    private static Vector2? HomeNeurolink(PartyRole role) => role switch
    {
        MeleeDpsA => AddsNeurolinks.North,
        MeleeDpsB => AddsNeurolinks.East,
        PhysRangedDps => AddsNeurolinks.West,
        _ => null,
    };

    private Dictionary<PartyRole, Vector2> HatchNeurolinks(int set)
    {
        var targets = state.HatchTargets[set];
        var free = new List<Vector2> { AddsNeurolinks.North, AddsNeurolinks.East, AddsNeurolinks.West };
        var claimed = new Dictionary<PartyRole, Vector2>();
        foreach (var role in targets)
            if (HomeNeurolink(role) is { } home)
            {
                claimed[role] = home;
                free.Remove(home);
            }
        foreach (var role in targets.Where(r => !claimed.ContainsKey(r)))
        {
            claimed[role] = free[0];
            free.RemoveAt(0);
        }
        return claimed;
    }

    private Vector2?[] HatchSpots(int set)
    {
        var at = world.Events.Elapsed;
        var spots = new Vector2?[8];
        spots[(int)MainTank] = TankSpot(MainTank, at);
        spots[(int)OffTank] = TankSpot(OffTank, at);
        var neurolinks = HatchNeurolinks(set);
        foreach (var (role, neurolink) in neurolinks)
            spots[(int)role] = neurolink == AddsNeurolinks.West ? FarNeurolinkWait : neurolink;
        var waiting = Enum.GetValues<PartyRole>().Where(r => spots[(int)r] == null).ToList();
        for (var i = 0; i < waiting.Count; i++) spots[(int)waiting[i]] = HatchSafeSpots[Math.Min(i, HatchSafeSpots.Length - 1)];
        return spots;
    }

    private Vector2?[] StepOffTwisters()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
            if (world.Party.Get(role) is { } member)
                spots[(int)role] = UcobSim.Flat(member.Position) + TwisterStep;
        return spots;
    }

    private Vector2?[] StepOffTwistersIntoTheFarNeurolink(int set)
    {
        var spots = StepOffTwisters();
        foreach (var (role, neurolink) in HatchNeurolinks(set))
            if (neurolink == AddsNeurolinks.West) spots[(int)role] = AddsNeurolinks.West;
        return spots;
    }

    private Vector2 NaelPosition()
        => world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus) is { } nael
            ? UcobSim.Flat(nael.Position)
            : NaelTankSpot;

    private Vector2?[] QuoteSpots(NaelMove move) => move switch
    {
        NaelMove.LunarDynamo => RingAroundNael(DynamoRingRadius),
        NaelMove.ThermionicBeam => StackInTheMiddle(),
        _ => RingAroundNael(OutRingRadius),
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

    private static Vector2?[] StackInTheMiddle()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < 8; i++) spots[i] = new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1));
        return spots;
    }
}
