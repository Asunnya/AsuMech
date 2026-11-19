using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.CurtainCall.M12sCurtainCallScenario;

namespace AsuMech.Scenarios.M12s.CurtainCall;

public sealed class M12sCurtainCallAi : IScenarioAi<M12sCurtainCallState>
{
    public string Name => "Toxic Friends";

    private static readonly Vector2[] PuddleTrail = [new(-2f, 5f), new(-9f, 1f), new(-9f, -7f), new(-2f, -11f), new(2f, -3f)];
    private static readonly Vector2[] InTheCleave = [new(-4f, -5f), new(-12f, -3f), new(-3f, 6.5f), new(-13f, 8f)];
    private static readonly Vector2[] BesideTheHead = [new(6f, -13f), new(17f, -7f), new(17f, 7f), new(6f, 13f)];
    private static readonly float[] ChainRows = [-14f, -5f, 5f, 14f];
    private static readonly Vector2[] Huddle =
        [new(0f, -0.6f), new(0.6f, 0f), new(0f, 0.6f), new(-0.6f, 0f), new(0.45f, -0.45f), new(0.45f, 0.45f), new(-0.45f, 0.45f), new(-0.45f, -0.45f)];
    private const float ChainEdge = 19f;
    private const float CornerX = 18f;
    private const float CornerZ = 13.5f;
    private const float Reaction = 0.5f;

    private M12sCurtainCallState state = null!;

    public void Run(M12sCurtainCallState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => ByRole(role => OnTheTrail(0, role)));
        for (var w = 1; w < PuddleTrail.Length; w++)
        {
            var wave = w;
            ai.Move(SpotlightAt[wave - 1] + 0.05f, () => ByRole(role => OnTheTrail(wave, role)));
        }
        ai.Move(SpotlightAt[^1] + 0.05f, () => ByRole(GrotesquerieSpot), jitter: 0f);
        ai.Move(BurstingSpreadsAt + 0.15f, () => ByRole(role => Huddle[(int)role]));
        ai.Move(ChainsAt + Reaction, () => ByRole(ChainBreakingSpot), jitter: 0f);
        ai.Move(ChainsAt + 3.35f, () => ByRole(SafeCorner), sprint: true);
        ai.Move(BurstHitsAt + 1f, () => ByRole(role => new Vector2(0f, -5f) + Huddle[(int)role]));
    }

    private Vector2 Mirror(Vector2 p) => new(p.X * state.DragonSide, p.Y);

    private Vector2? OnTheTrail(int wave, PartyRole role) => Mirror(PuddleTrail[wave]) + Huddle[(int)role];

    private static int IndexInRole(PartyRole role)
        => Array.IndexOf(role.IsDps() ? M12sCurtainCallState.Dps : M12sCurtainCallState.Supports, role);

    private Vector2? GrotesquerieSpot(PartyRole role)
        => Mirror(state.HasRotting(role) ? InTheCleave[IndexInRole(role)] : BesideTheHead[IndexInRole(role)]);

    private static int Side(PartyRole role) => role.IsDps() ? 1 : -1;

    private Vector2? ChainBreakingSpot(PartyRole role)
    {
        var support = role.IsDps() ? state.ChainPartner[role] : role;
        return new Vector2(Side(role) * ChainEdge, ChainRows[IndexInRole(support)]);
    }

    private Vector2? SafeCorner(PartyRole role)
        => new Vector2(Side(role) * CornerX, state.SafeCornerZ(Side(role)) * CornerZ) + Huddle[(int)role];

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
