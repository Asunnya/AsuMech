using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruCyclonicBreakDance;

namespace AsuMech.Scenarios.Fru.P1UtopianSky;

public sealed class FruP1UtopianSkyAi : IScenarioAi<FruP1UtopianSkyState>
{
    public string Name => "NA · LesBin";
    public string Group => "NA";

    private static readonly float[] CheckDegrees = [0f, 45f, 270f, 90f, 225f, 135f, 315f, 180f];
    private static readonly float[] CheckRadius = [17f, 14f, 17f, 17f, 17f, 17f, 17f, 17f];
    private const float LineEndRadius = 19f;
    private const float HealerLineEndRadius = 12f;
    private const float StackLineEndRadius = 14f;
    private const float FlankDegrees = 17f;

    private FruP1UtopianSkyState state = null!;

    public void Run(FruP1UtopianSkyState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        Dance(ai, 10.05f, state.CyclonicFire);
        ai.Move(27f, () => ByRole(CheckImage), arrivalTime: 33f);
        ai.Move(36.5f, () => ByRole(SafeLineEnd), arrivalTime: 44.3f);
    }

    private static Vector3 CheckImage(PartyRole role) => Point(CheckDegrees[(int)role], CheckRadius[(int)role]);

    private static bool LightPartyOne(PartyRole role) => ((int)role & 1) == 0;

    private float LightPartyOneEndDegrees => state.SafeAxis switch
    {
        0 => 0f,
        1 => 225f,
        2 => 270f,
        _ => 315f,
    };

    private Vector3 SafeLineEnd(PartyRole role)
    {
        var end = LightPartyOneEndDegrees + (LightPartyOne(role) ? 0f : 180f);
        if (state.UtopianFire) return Point(end, StackLineEndRadius);
        return role switch
        {
            PartyRole.MainTank or PartyRole.OffTank => Point(end, LineEndRadius),
            PartyRole.RegenHealer or PartyRole.ShieldHealer => Point(end, HealerLineEndRadius),
            PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => Point(end + FlankDegrees, LineEndRadius),
            _ => Point(end - FlankDegrees, LineEndRadius),
        };
    }
}
