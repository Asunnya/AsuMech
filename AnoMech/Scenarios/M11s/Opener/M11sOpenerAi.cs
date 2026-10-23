using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M11s.Opener.M11sOpenerScenario;

namespace AnoMech.Scenarios.M11s.Opener;

public sealed class M11sOpenerAi : IScenarioAi<M11sOpenerState>
{
    public string Name => "Hector (Toxic)";

    public static readonly PartyRole[] LightPartyOne = [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps];
    public static readonly PartyRole[] LightPartyTwo = [PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.MeleeDpsB, PartyRole.CasterDps];

    private static readonly Vector2 TanksInFront = new(0f, -5f);
    private static readonly Vector2 PartyBehind = new(0f, 7f);
    private static readonly Vector2 WaitInTheMiddle = new(0f, 2f);

    private const float UnderTheScythe = 3.5f;
    private const float AcrossFromTheAxe = 2f;
    private const float AwayFromTheSword = 8.5f;
    private const float CometRing = 7f;
    private const float SpreadRing = 15f;
    private const float QuarterTurn = MathF.PI / 2f;

    private M11sOpenerState state = null!;
    private SimWorld world = null!;
    private readonly float[] firstCometStop = new float[2];

    public void Run(M11sOpenerState stateParam, SimWorld world)
    {
        state = stateParam;
        this.world = world;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(TanksInFrontPartyBehind));
        ai.Move(TrophyCastAt + 0.4f, () => ByRole(TakeTheRawSteelSpot), jitter: 0f);
        ai.Move(RawSteelHitAt + 0.8f, () => ByRole(role => WaitInTheMiddle + StackOffset(role)));
        for (var s = 0; s < 2; s++)
        {
            var set = s;
            var toFirstWeaponAt = set == 0 ? AssaultCastAt[0] + 1f : CometCastAt[0] + CometResolves + 0.1f;
            ai.Move(toFirstWeaponAt, () => ByRole(role => WeaponSpot(role, set, 0)), jitter: 0f, sprint: true);
            for (var i = 1; i < 3; i++)
            {
                var index = i;
                ai.Move(StrikeAt[set][index - 1] + StrikeResolves + 0.1f, () => ByRole(role => WeaponSpot(role, set, index)), jitter: 0f, sprint: true);
            }
        }
        for (var s = 0; s < 2; s++)
        {
            var set = s;
            var waves = CometiteAt[set];
            var startAt = set == 0 ? VoidStardustCastAt : StrikeAt[1][2] + StrikeResolves + 0.05f;
            ai.Move(startAt, () => ByRole(role => CometStop(role, set, 0, chooseStart: true)), jitter: 0f, sprint: true);
            ai.Move(waves[0] + 0.05f, () => ByRole(role => CometStop(role, set, 1)), jitter: 0f, sprint: true);
            ai.Move(waves[1] + 0.05f, () => ByRole(role => CometStop(role, set, 2)), jitter: 0f, sprint: true);
            ai.Move(waves[1] + CometiteResolves + 0.05f, () => ByRole(role => StackOrSpread(role, set)), jitter: 0f, sprint: true);
        }
        ai.Move(CometCastAt[1] + CometResolves + 0.3f, () => ByRole(role => StackOffset(role)));
    }

    private static Vector2? TanksInFrontPartyBehind(PartyRole role) =>
        role.IsTank() ? TanksInFront + StackOffset(role) : PartyBehind + StackOffset(role);

    private Vector2? TakeTheRawSteelSpot(PartyRole role) => state.AxeTrophyFirst ? AxeSpreads(role) : ScytheSplit(role);

    private static Vector2 ScytheSplit(PartyRole role) => role switch
    {
        PartyRole.MainTank => new Vector2(-5f, -5f),
        PartyRole.OffTank => new Vector2(5f, -5f),
        _ => PartyBehind + StackOffset(role),
    };

    private static Vector2 AxeSpreads(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => new Vector2(0f, -6f) + StackOffset(role),
        PartyRole.MeleeDpsA => new Vector2(-5f, 5f),
        PartyRole.MeleeDpsB => new Vector2(5f, 5f),
        PartyRole.RegenHealer => new Vector2(-6.3f, 13.9f),
        PartyRole.ShieldHealer => new Vector2(6.5f, 13.9f),
        PartyRole.PhysRangedDps => new Vector2(-16.6f, 7.6f),
        _ => new Vector2(16.4f, 7.5f),
    };

    private Vector2? WeaponSpot(PartyRole role, int set, int index)
    {
        var arrival = state.ArrivalBearing(set, index);
        var weapon = Flat(state.WeaponSpot(set, index));
        return state.Weapons[set][index] switch
        {
            Weapon.Axe => -AcrossFromTheAxe * Toward(state.WeaponBearing(set, index)) + StackOffset(role),
            Weapon.Scythe => weapon + UnderTheScythe * Toward(arrival + ProteanTurn(role)),
            _ => weapon + AwayFromTheSword * Toward(arrival + (LightPartyOne.Contains(role) ? LeftOfTheSword : RightOfTheSword)) + StackOffset(role),
        };
    }

    private const float LeftOfTheSword = MathF.PI * 3f / 4f;
    private const float RightOfTheSword = -MathF.PI * 3f / 4f;

    private static float ProteanTurn(PartyRole role) => role switch
    {
        PartyRole.MainTank => 0f,
        PartyRole.CasterDps => -1f,
        PartyRole.ShieldHealer => -2f,
        PartyRole.MeleeDpsB => -3f,
        PartyRole.OffTank => 4f,
        PartyRole.MeleeDpsA => 3f,
        PartyRole.RegenHealer => 2f,
        _ => 1f,
    } * MathF.PI / 4f;

    private Vector2? CometStop(PartyRole role, int set, int stop, bool chooseStart = false)
    {
        if (chooseStart) firstCometStop[set] = set == 0 ? MathF.PI : NearestCardinalToTheParty();
        return CometRing * Toward(firstCometStop[set] - stop * QuarterTurn) + StackOffset(role);
    }

    private Vector2? StackOrSpread(PartyRole role, int set)
    {
        if (state.StackOnSet(set)) return CometStop(role, set, 3);
        return SpreadRing * Toward(MathF.PI + ProteanTurn(role));
    }

    private float NearestCardinalToTheParty()
    {
        var members = Enum.GetValues<PartyRole>().Select(r => world.Party.Get(r)).Where(m => m != null && m.IsAlive()).ToList();
        if (members.Count == 0) return MathF.PI;
        var centre = members.Aggregate(Vector2.Zero, (sum, m) => sum + new Vector2(m!.Position.X, m.Position.Z)) / members.Count;
        var bearing = MathF.Atan2(centre.X, centre.Y);
        return MathF.Round(bearing / QuarterTurn) * QuarterTurn;
    }

    private static Vector2 Toward(float bearing) => new(MathF.Sin(bearing), MathF.Cos(bearing));

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
