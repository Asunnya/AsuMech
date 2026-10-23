using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Scenarios.M11s.OrbitalOmen.M11sOrbitalOmenScenario;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

public sealed class M11sOrbitalOmenAi : IScenarioAi<M11sOrbitalOmenState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 MainTankTakesTheFirstLine = new(0f, -5f);
    private static readonly Vector2 OffTankWaitsNortheast = new(4.5f, -4.5f);
    private static readonly Vector2 MainTankStepsNorthwest = new(-4.5f, -4.5f);
    private static readonly Vector2 OffTankStepsEast = new(5f, 0f);
    private static readonly Vector2 PartyBehind = new(0f, 6f);

    private const float LineHalfWidth = 5f;
    private const float LineMargin = 1f;
    private const float FlankMargin = 2f;
    private const float RunSpeed = 7f;
    private const float SearchStep = 0.5f;
    private const float InsideTheWalls = 17f;

    private M11sOrbitalOmenState state = null!;

    public void Run(M11sOrbitalOmenState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(TanksBaitTheLinesPartyBehind));
        ai.Move(LineAt[0] + 0.3f, () => ByRole(role => role == PartyRole.MainTank ? MainTankStepsNorthwest : null), jitter: 0f, sprint: true);
        ai.Move(LineAt[1] + 0.3f, () => ByRole(role => role == PartyRole.OffTank ? OffTankStepsEast : null), jitter: 0f, sprint: true);
        ai.Move(OrbitalCastAt + 0.5f, () => ByRole(role => FlankClearOfTheFirstLines() + M11sOpenerAi.StackOffset(role)), jitter: 0f);
        ai.Move(FuryCastAt + FuryResolves + 0.1f, () => ByRole(role => FirstCrossing() + M11sOpenerAi.StackOffset(role)), jitter: 0f, sprint: true);
    }

    private static Vector2? TanksBaitTheLinesPartyBehind(PartyRole role) => role switch
    {
        PartyRole.MainTank => MainTankTakesTheFirstLine,
        PartyRole.OffTank => OffTankWaitsNortheast,
        _ => PartyBehind + M11sOpenerAi.StackOffset(role),
    };

    private Vector2 FirstCrossing() => new(state.FirstCrossing.X, state.FirstCrossing.Z);

    private Vector2 FlankClearOfTheFirstLines()
    {
        var crossing = FirstCrossing();
        var dodgeTime = PairCastAt[1] + PairResolves - (FuryCastAt + FuryResolves + 0.1f);
        Vector2? best = null;
        var bestDistance = float.MaxValue;
        for (var x = -InsideTheWalls; x <= InsideTheWalls; x += SearchStep)
        for (var z = -InsideTheWalls; z <= InsideTheWalls; z += SearchStep)
        {
            var spot = new Vector2(x, z);
            if (MathF.Abs(x) < MathF.Abs(z) + FlankMargin) continue;
            if (InPair(spot, 0, LineMargin)) continue;
            var toCrossing = crossing - spot;
            var distance = toCrossing.Length();
            var dodged = distance < 0.01f ? spot : spot + Vector2.Normalize(toCrossing) * MathF.Min(distance, RunSpeed * dodgeTime);
            if (InPair(dodged, 1, LineMargin * 0.5f)) continue;
            if (distance >= bestDistance) continue;
            best = spot;
            bestDistance = distance;
        }
        return best ?? new Vector2(12f, 0f);
    }

    private bool InPair(Vector2 p, int pair, float margin)
        => MathF.Abs(p.X - state.NorthLines[pair]) < LineHalfWidth + margin || MathF.Abs(p.Y - state.EastLines[pair]) < LineHalfWidth + margin;

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
