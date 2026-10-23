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
    private static readonly Vector2 BesideTheBoss = new(4f, 2f);

    private M11sOrbitalOmenState state = null!;

    public void Run(M11sOrbitalOmenState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(TanksBaitTheLinesPartyBehind));
        ai.Move(LineAt[0] + 0.3f, () => ByRole(role => role == PartyRole.MainTank ? MainTankStepsNorthwest : null), jitter: 0f, sprint: true);
        ai.Move(LineAt[1] + 0.3f, () => ByRole(role => role == PartyRole.OffTank ? OffTankStepsEast : null), jitter: 0f, sprint: true);
        ai.Move(FuryCastAt, () => ByRole(role => InTheSecondPairsQuadrant(1f) + M11sOpenerAi.StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(PairCastAt[0] + PairResolves + 0.05f, () => ByRole(role => InTheSecondPairsQuadrant(-1f) + M11sOpenerAi.StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(PairCastAt[1] + PairResolves + 0.05f, () => ByRole(role => InTheSecondPairsQuadrant(1f) + M11sOpenerAi.StackOffset(role)), jitter: 0f, sprint: true);
    }

    private static Vector2? TanksBaitTheLinesPartyBehind(PartyRole role) => role switch
    {
        PartyRole.MainTank => MainTankTakesTheFirstLine,
        PartyRole.OffTank => OffTankWaitsNortheast,
        _ => PartyBehind + M11sOpenerAi.StackOffset(role),
    };

    private Vector2 InTheSecondPairsQuadrant(float side)
        => side * new Vector2(BesideTheBoss.X * MathF.Sign(state.NorthLines[1]), BesideTheBoss.Y * MathF.Sign(state.EastLines[1]));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
