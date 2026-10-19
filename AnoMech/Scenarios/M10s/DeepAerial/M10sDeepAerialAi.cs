using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.DeepAerial.M10sDeepAerialScenario;

namespace AnoMech.Scenarios.M10s.DeepAerial;

public sealed class M10sDeepAerialAi : IScenarioAi<M10sDeepAerialState>
{
    public string Name => "Hector (Parallel)";

    private static readonly Vector2 PartyWaits = new(8f, 0f);
    private const float FirstPartyDepth = 8f;
    private const float FirstFireX = -4f;
    private const float FirstFireDepth = 17f;
    private const float FireDepth = 18f;
    private const float WaterX = -13f;
    private const float WaterDepth = 16f;

    private M10sDeepAerialState state = null!;

    public void Run(M10sDeepAerialState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => role switch
        {
            PartyRole.RegenHealer => new Vector2(-1f, 0f),
            PartyRole.ShieldHealer => new Vector2(1f, 0f),
            _ => FirstPartySpot + StackOffset(role),
        }), jitter: 0f);
        for (var r = 0; r < M10sDeepAerialState.Rounds; r++)
        {
            var round = r;
            ai.Move(TetherAt[round] + 0.2f, () => ByRole(role => StretchTheTether(role, round)), jitter: 0f, sprint: true);
            ai.Move(TetherAt[round] + TetherToWave + 0.3f, () => ByRole(role => Tethered(role, round) ? WaitSpot(round + 1) + StackOffset(role) : null), jitter: 0f, sprint: true);
        }
        ai.Move(PrisonBreaksAt + 0.4f, () => ByRole(role => Vector2.Zero + StackOffset(role)));
    }

    private Vector2 FirstPartySpot => new(0f, FirstPartyDepth * state.Side);

    private Vector2 WaitSpot(int round) => round == 0 ? FirstPartySpot : PartyWaits;

    private bool Tethered(PartyRole role, int round) => state.Tethers[round].Fire == role || state.Tethers[round].Water == role;

    private Vector2? StretchTheTether(PartyRole role, int round)
    {
        var away = round % 2 == 0 ? -state.Side : state.Side;
        if (state.Tethers[round].Fire == role)
            return round == 0 ? new Vector2(FirstFireX, FirstFireDepth * away) : new Vector2(0f, FireDepth * away);
        if (state.Tethers[round].Water == role)
            return new Vector2(WaterX, WaterDepth * away);
        return null;
    }

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
