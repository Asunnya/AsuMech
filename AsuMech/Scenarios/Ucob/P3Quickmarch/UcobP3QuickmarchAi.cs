using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using Scenario = AsuMech.Scenarios.Ucob.P3Quickmarch.UcobP3QuickmarchScenario;
using State = AsuMech.Scenarios.Ucob.P3Quickmarch.UcobP3QuickmarchState;

namespace AsuMech.Scenarios.Ucob.P3Quickmarch;

public sealed class UcobP3QuickmarchAi(bool tuufless = false) : IScenarioAi<UcobP3QuickmarchState>, IUcobStrat
{
    public string Name => tuufless ? "Tuufless (Elemental)" : "Uptime: light parties beside the dives, healers and a DPS bait Earth Shakers";

    public UcobMarkers Markers => tuufless ? UcobMarkers.Elemental : UcobMarkers.Aether;

    private static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();

    private static readonly Dictionary<PartyRole, Vector2> OpeningSpots = new()
    {
        [MainTank] = new(0f, -3f), [OffTank] = new(0f, 2.5f), [RegenHealer] = new(-1.5f, 4f), [ShieldHealer] = new(1.5f, 4f),
        [MeleeDpsA] = new(-1f, 3f), [MeleeDpsB] = new(1f, 3f), [PhysRangedDps] = new(-1f, 5.5f), [CasterDps] = new(1f, 5.5f),
    };

    private static readonly Dictionary<PartyRole, Vector2> BesideTheDives = new()
    {
        [MainTank] = new(-16.5f, 10f), [RegenHealer] = new(-19f, 4f), [PhysRangedDps] = new(-20f, -1.5f), [MeleeDpsA] = new(-18.5f, -7f),
        [OffTank] = new(16.5f, 10f), [ShieldHealer] = new(19f, 4f), [CasterDps] = new(20f, -1.5f), [MeleeDpsB] = new(18.5f, -7f),
    };

    private static readonly Dictionary<PartyRole, float> SpreadBearings = new()
    {
        [MainTank] = -25f, [RegenHealer] = -60f, [PhysRangedDps] = -100f, [MeleeDpsA] = -140f,
        [OffTank] = 25f, [ShieldHealer] = 60f, [CasterDps] = 100f, [MeleeDpsB] = 140f,
    };

    private static readonly Dictionary<PartyRole, float> InsideThePuddlesBearings = new()
    {
        [MainTank] = -37f, [RegenHealer] = -70f, [PhysRangedDps] = -110f, [MeleeDpsA] = -150f,
        [OffTank] = 37f, [ShieldHealer] = 70f, [CasterDps] = 110f, [MeleeDpsB] = 150f,
    };

    private const float SpreadRadius = 14f;
    private const float InsideThePuddlesRadius = 5f;
    private static readonly Vector2 MainTankAtTheBoss = new(-3.5f, 3.5f);
    private static readonly Vector2 OffTankAtTheBoss = new(3.5f, 3.5f);
    private static readonly Vector2 RegenHealerEarthShaker = Polar(-45f, 14f);
    private static readonly Vector2 ShieldHealerEarthShaker = Polar(45f, 14f);
    private static readonly Vector2 DpsEarthShaker = Polar(135f, 14f);
    private static readonly Vector2 DpsStack = Polar(-135f, 8f);
    private static readonly Vector2 MainTankInFrontOfRegenHealer = Polar(-90f, 7f);
    private static readonly Vector2 OffTankInFrontOfShieldHealer = Polar(90f, 7f);
    private static readonly Vector2 RegenHealerEarthShakerWest = Polar(-90f, 15f);
    private static readonly Vector2 ShieldHealerEarthShakerEast = Polar(90f, 15f);
    private static readonly Vector2 DpsEarthShakerNorth = Polar(0f, 14f);
    private static readonly Vector2 DpsStackSouth = Polar(180f, 8f);
    private static readonly Vector2 RegenHealerClearOfTheTempestWing = Polar(-125f, 14f);
    private static readonly Vector2 ShieldHealerClearOfTheTempestWing = Polar(125f, 14f);
    private static readonly Vector2 MainTankFacingFlareBreath = new(0f, 4.5f);
    private static readonly Vector2 OffTankBehindBahamut = new(2.5f, -3f);
    private static readonly Vector2 PartyBehindBahamut = new(0f, -5f);

    private const float MoveBesideTheDives = 10.8f;
    private const float StepOffTheTwisters = 16.5f;
    private const float RunInsideThePuddles = 19.85f;
    private const float TakeEarthShakersAndStack = 22.1f;
    private const float GatherBehindForFlareBreath = 29.6f;
    private const float LeaveTheEarthShakerSpots = 27f;

    private State state = null!;
    private SimWorld world = null!;

    public void Run(UcobP3QuickmarchState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => Absolute(OpeningSpots), jitter: 0f);
        ai.Move(MoveBesideTheDives, () => FromTheDives(r => BesideTheDives[EdgeSlot(r)]), jitter: 0f, sprint: true);
        ai.Move(StepOffTheTwisters, () => FromTheDives(r => Polar(SpreadBearings[EdgeSlot(r)], SpreadRadius)), jitter: 0f, sprint: true);
        ai.Move(RunInsideThePuddles, () => FromTheDives(r => Polar(InsideThePuddlesBearings[EdgeSlot(r)], InsideThePuddlesRadius)), jitter: 0f, sprint: true);
        if (tuufless)
        {
            ai.Move(TakeEarthShakersAndStack, () => FromTheDives(TanksInFrontOfHealersDpsStackSouthSpot), jitter: 0f, sprint: true);
            ai.Move(LeaveTheEarthShakerSpots, () => FromTheDives(ClearOfTheTempestWingSpot), jitter: 0f, sprint: true);
        }
        else
        {
            ai.Move(TakeEarthShakersAndStack, () => FromTheDives(EarthShakerAndStackSpot), jitter: 0f, sprint: true);
        }
        ai.Move(GatherBehindForFlareBreath, () => FromTheDives(FlareBreathSpot), jitter: 0f, sprint: true);
    }

    private Vector2 EarthShakerAndStackSpot(PartyRole role) => role switch
    {
        MainTank => MainTankAtTheBoss,
        OffTank => OffTankAtTheBoss,
        RegenHealer => RegenHealerEarthShaker,
        ShieldHealer => ShieldHealerEarthShaker,
        _ when role == state.EarthShakerDps => DpsEarthShaker,
        _ => DpsStack,
    };

    private PartyRole EdgeSlot(PartyRole role) => !tuufless ? role : role switch
    {
        MeleeDpsA => PhysRangedDps,
        PhysRangedDps => MeleeDpsA,
        MeleeDpsB => CasterDps,
        CasterDps => MeleeDpsB,
        _ => role,
    };

    private Vector2 TanksInFrontOfHealersDpsStackSouthSpot(PartyRole role) => role switch
    {
        MainTank => MainTankInFrontOfRegenHealer,
        OffTank => OffTankInFrontOfShieldHealer,
        RegenHealer => RegenHealerEarthShakerWest,
        ShieldHealer => ShieldHealerEarthShakerEast,
        _ when role == state.EarthShakerDps => DpsEarthShakerNorth,
        _ => DpsStackSouth,
    };

    private static Vector2 ClearOfTheTempestWingSpot(PartyRole role) => role switch
    {
        MainTank => MainTankInFrontOfRegenHealer,
        OffTank => OffTankInFrontOfShieldHealer,
        RegenHealer => RegenHealerClearOfTheTempestWing,
        ShieldHealer => ShieldHealerClearOfTheTempestWing,
        _ => DpsStackSouth,
    };

    private static Vector2 FlareBreathSpot(PartyRole role) => role switch
    {
        MainTank => MainTankFacingFlareBreath,
        OffTank => OffTankBehindBahamut,
        _ => PartyBehindBahamut,
    };

    private static IAiMove Absolute(Dictionary<PartyRole, Vector2> spots)
    {
        var coords = new Vector2?[Roles.Length];
        foreach (var role in Roles) coords[(int)role] = spots[role];
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove FromTheDives(Func<PartyRole, Vector2> spot)
    {
        var coords = new Vector2?[Roles.Length];
        foreach (var role in Roles) coords[(int)role] = TurnToTheDives(spot(role));
        return AiMove.Create(coords).NaturalOrder();
    }

    private Vector2 TurnToTheDives(Vector2 clockwiseAndNorth)
    {
        var theta = state.DiveFrom.RadiansFromNorth;
        var towardTheDives = new Vector2(MathF.Sin(theta), -MathF.Cos(theta));
        var clockwise = new Vector2(MathF.Cos(theta), MathF.Sin(theta));
        return clockwise * clockwiseAndNorth.X + towardTheDives * clockwiseAndNorth.Y;
    }

    private static Vector2 Polar(float bearingDegrees, float radius)
    {
        var radians = bearingDegrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(radians), MathF.Cos(radians)) * radius;
    }
}
