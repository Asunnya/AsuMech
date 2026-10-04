using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Uwu.PrimalRoulette;

public sealed class PrimalRouletteAi : IScenarioAi<PrimalRouletteState>
{
    public string Name => "NAUR (party right)";

    private const float WickedWheelEdge = 10.5f;
    private const float InsideTheTornado = 3.5f;

    private static readonly Vector2 TanksBesideUltima = new(-4.3f, 3.5f);
    private static readonly Vector2 PartyBesideUltima = new(4f, 4.5f);
    private static readonly Vector2 TanksSouthOrb = new(-9.5f, 6.5f);
    private static readonly Vector2 PartySouthOrb = new(9.5f, 6.5f);
    private static readonly Vector2 TanksNorthOrb = new(-9.5f, -6.5f);
    private static readonly Vector2 PartyNorthOrb = new(9.5f, -6.5f);

    private PrimalRouletteState state = null!;

    public void Run(PrimalRouletteState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => TanksAndParty(TanksBesideUltima, PartyBesideUltima));
        ai.Move(30.15f, () => TanksAndParty(TanksSouthOrb, PartySouthOrb));
        ai.Move(32.20f, () => TanksAndParty(TanksNorthOrb, PartyNorthOrb));
        ai.Move(35.30f, () => AiMove.All(PrimalRouletteState.North));

        for (var segment = 0; segment < PrimalRouletteState.SegmentStarts.Length; segment++)
        {
            var start = PrimalRouletteState.SegmentStarts[segment];
            var anchor = state.StartAnchor(segment);
            switch (state.Order[segment])
            {
                case Primal.Titan: DodgeTripleWeights(ai, start, anchor); break;
                case Primal.Ifrit: BaitEruptionsThenSprintToTheIntercard(ai, start); break;
                default: OutOfTheWheelThenIntoTheTornado(ai, start, anchor); break;
            }
        }

        ai.Move(115.50f, () => AiMove.All(PrimalRouletteState.North));
    }

    private static IAiMove TanksAndParty(Vector2 tanks, Vector2 party) =>
        AiMove.Create(tanks, tanks, party, party, party, party, party, party).NaturalOrder();

    private static void DodgeTripleWeights(AiManager ai, float start, Vector2 anchor)
    {
        var other = PrimalRouletteState.OtherAnchor(anchor);
        ai.Move(start + 4.30f, () => AiMove.All(other), sprint: true);
        ai.Move(start + 7.30f, () => AiMove.All(anchor), sprint: true);
        ai.Move(start + 10.30f, () => AiMove.All(other), sprint: true);
        ai.Move(start + 16.50f, () => AiMove.All(PrimalRouletteState.North));
    }

    private static void BaitEruptionsThenSprintToTheIntercard(AiManager ai, float start) =>
        ai.Move(start + 6.40f, () => AiMove.All(PrimalRouletteState.NorthWest), sprint: true);

    private static void OutOfTheWheelThenIntoTheTornado(AiManager ai, float start, Vector2 anchor)
    {
        ai.Move(start + 1.00f, () => AiMove.All(PrimalRouletteState.TowardCentre(anchor, WickedWheelEdge)));
        ai.Move(start + 9.25f, () => AiMove.All(PrimalRouletteState.TowardCentre(anchor, InsideTheTornado)));
        ai.Move(start + 22.65f, () => AiMove.All(PrimalRouletteState.North), sprint: true);
    }
}
