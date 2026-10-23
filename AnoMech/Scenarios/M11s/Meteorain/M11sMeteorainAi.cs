using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Scenarios.M11s.Meteorain.M11sMeteorainScenario;

namespace AnoMech.Scenarios.M11s.Meteorain;

public sealed class M11sMeteorainAi : IScenarioAi<M11sMeteorainState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 HugTheBossNorthwest = new(-1.5f, -1.5f);
    private static readonly Vector2 HugTheBossSouthwest = new(-1.5f, 1.5f);
    private static readonly Vector2 HugTheBossNortheast = new(1.5f, -1.5f);
    private static readonly Vector2 HugTheBossWest = new(-2f, 0.5f);
    private static readonly Vector2 HugTheBossSouth = new(-0.5f, 2f);
    private static readonly Vector2 FirstStackNorthwest = new(-5.3f, -5.1f);
    private static readonly Vector2 OffTankFrontOfTheFirstLine = new(-3.2f, -3.2f);
    private static readonly Vector2 MainTankFrontOfTheFirstLine = new(-4.5f, -2.3f);
    private static readonly Vector2 NortheastMeteor = new(5f, -5f);
    private static readonly Vector2 SouthwestMeteor = new(-5f, 5f);
    private static readonly Vector2 NorthwestMeteor = new(-4.9f, -4.8f);
    private static readonly Vector2 SecondSouthwestMeteor = new(-9.8f, 9.9f);
    private static readonly Vector2 ThirdSouthwestMeteor = new(-15.1f, 15f);
    private static readonly Vector2 LastNortheastMeteor = new(5.1f, -5f);
    private static readonly Vector2 BehindTheNortheastMeteor = new(6.8f, -6.9f);
    private static readonly Vector2 BehindTheNorthwestMeteor = new(-6.8f, -6.9f);
    private static readonly Vector2 BehindTheLastNortheastMeteor = new(7.2f, -7.1f);
    private static readonly Vector2 NorthOfTheExplosion = new(-3f, -17f);
    private static readonly Vector2 HideBehindTheSouthwestMeteors = new(-17.3f, 17.3f);

    private const float NearTheWall = 16f;

    private M11sMeteorainState state = null!;

    public void Run(M11sMeteorainState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        var fall = MeteorMarkedAt.Select(t => t + MeteorFallsAfter).ToArray();
        var line = FireballCastAt.Select(t => t + FireballLineAfter).ToArray();

        ai.Move(0.5f, () => ByRole(MeleesHugTheBossPartyStacksNorthwest), jitter: 0f);
        ai.Move(MeteorMarkedAt[0] + 0.2f, () => ByRole(role => role switch
        {
            PartyRole.MeleeDpsA => NortheastMeteor,
            PartyRole.MeleeDpsB => SouthwestMeteor,
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(line[0] + 0.15f, () => ByRole(role => role switch
        {
            PartyRole.RegenHealer => HugTheBossNorthwest,
            PartyRole.ShieldHealer => HugTheBossSouthwest,
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(line[0] + 0.15f, () => ByRole(role => role is PartyRole.MainTank or PartyRole.OffTank ? TakeTheTether(role, 0) : null), jitter: 0f, sprint: true);
        ai.Move(fall[0] + 0.15f, () => ByRole(role => role switch
        {
            PartyRole.MeleeDpsA or PartyRole.MeleeDpsB or PartyRole.PhysRangedDps or PartyRole.CasterDps => BehindTheNortheastMeteor + M11sOpenerAi.StackOffset(role),
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(MeteorMarkedAt[1] + 0.2f, () => ByRole(role => role switch
        {
            PartyRole.RegenHealer => NorthwestMeteor,
            PartyRole.ShieldHealer => SecondSouthwestMeteor,
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(line[1] + 0.15f, () => ByRole(role => role switch
        {
            PartyRole.PhysRangedDps => HugTheBossWest,
            PartyRole.CasterDps => HugTheBossSouth,
            PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => BehindTheNorthwestMeteor + M11sOpenerAi.StackOffset(role),
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(fall[0] + FatalityAfterFall + 0.2f, () => ByRole(role => role is PartyRole.MainTank or PartyRole.OffTank ? TakeTheTether(role, 1) : null), jitter: 0f, sprint: true);
        ai.Move(fall[1] + 0.15f, () => ByRole(role => role switch
        {
            PartyRole.RegenHealer or PartyRole.ShieldHealer => BehindTheNorthwestMeteor + M11sOpenerAi.StackOffset(role),
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(MeteorMarkedAt[2] + 0.2f, () => ByRole(role => role switch
        {
            PartyRole.PhysRangedDps => ThirdSouthwestMeteor,
            PartyRole.CasterDps => LastNortheastMeteor,
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(line[2] + 0.15f, () => ByRole(role => role is PartyRole.MeleeDpsA or PartyRole.MeleeDpsB or PartyRole.RegenHealer or PartyRole.ShieldHealer
            ? NorthOfTheExplosion + M11sOpenerAi.StackOffset(role)
            : null), jitter: 0f, sprint: true);
        ai.Move(fall[1] + FatalityAfterFall + 0.2f, () => ByRole(role => role is PartyRole.MainTank or PartyRole.OffTank ? TakeTheTether(role, 2) : null), jitter: 0f, sprint: true);
        ai.Move(fall[2] + 0.15f, () => ByRole(role => role is PartyRole.MainTank or PartyRole.OffTank ? null : BehindTheLastNortheastMeteor + M11sOpenerAi.StackOffset(role)), jitter: 0f, sprint: true);
        for (var k = 0; k < M11sMeteorainState.MeteorSets; k++)
        {
            var set = k;
            ai.Move(fall[set] + TetherAfterFall + 0.05f, () => ByRole(role => role is PartyRole.MainTank or PartyRole.OffTank ? TakeTheTether(role, set, state.Meteors) : null), jitter: 0f, sprint: true);
        }
        ai.Move(line[3] + 0.15f, () => ByRole(role => HideBehindTheSouthwestMeteors + M11sOpenerAi.StackOffset(role)), jitter: 0f, sprint: true);
    }

    private static Vector2? MeleesHugTheBossPartyStacksNorthwest(PartyRole role) => role switch
    {
        PartyRole.MeleeDpsA => HugTheBossNortheast,
        PartyRole.MeleeDpsB => HugTheBossSouthwest,
        PartyRole.MainTank => MainTankFrontOfTheFirstLine,
        PartyRole.OffTank => OffTankFrontOfTheFirstLine,
        _ => FirstStackNorthwest + M11sOpenerAi.StackOffset(role),
    };

    private Vector2? TakeTheTether(PartyRole role, int set) => TakeTheTether(role, set, MeteorsStandingFor(set));

    private Vector2? TakeTheTether(PartyRole role, int set, System.Collections.Generic.IEnumerable<Vector3> standing)
    {
        var tethered = state.TetheredMeteors(set, standing);
        if (tethered.Count == 0) return null;
        var north = tethered.OrderBy(t => t.Meteor.Z).ToList();
        var (side, meteor) = role == PartyRole.MainTank ? north[0] : north[^1];
        var shade = M11sMeteorainState.ShadeSpot(side);
        var spot = meteor;
        for (var t = 0f; t <= 1f; t += 0.02f)
        {
            var p = Vector3.Lerp(meteor, shade, t);
            if (MathF.Max(MathF.Abs(p.X), MathF.Abs(p.Z)) > NearTheWall) break;
            spot = p;
        }
        return new Vector2(spot.X, spot.Z);
    }

    private static Vector3[] MeteorsStandingFor(int set) => set switch
    {
        0 => [Flat(NortheastMeteor), Flat(SouthwestMeteor)],
        1 => [Flat(SouthwestMeteor), Flat(NorthwestMeteor), Flat(SecondSouthwestMeteor)],
        _ => [Flat(SouthwestMeteor), Flat(SecondSouthwestMeteor), Flat(ThirdSouthwestMeteor), Flat(LastNortheastMeteor)],
    };

    private static Vector3 Flat(Vector2 p) => new(p.X, 0f, p.Y);

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
