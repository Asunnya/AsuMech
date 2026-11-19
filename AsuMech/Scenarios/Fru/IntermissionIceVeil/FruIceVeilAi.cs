using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.IntermissionIceVeil.FruIceVeilState;

namespace AsuMech.Scenarios.Fru.IntermissionIceVeil;

public sealed class FruIceVeilAi : IScenarioAi<FruIceVeilState>
{
    public string Name => "NA · LesBin";
    public string Group => "NA";

    private const float MeleeRadius = 17f;
    private const float StormSidestep = 4f;
    private const float BehindDarkRadius = 11.3f;
    private const float VeilRadius = 7f;

    private FruIceVeilState state = null!;

    public void Run(FruIceVeilState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        ai.Move(0.5f, () => ByRole(role => Point(Octant(role), 2)), arrivalTime: 3f);
        ai.Move(31.8f, () => ByRole(role => Light(role) is { } light ? BesideLight(light, 1) : BehindDark(role)), jitter: 0f, arrivalTime: 36f);
        for (var wave = 0; wave < StormsDodgedBesideTheLight; wave++)
        {
            var w = wave;
            var side = w % 2 == 0 ? -1 : 1;
            ai.Move(StormAt[w] + 0.35f, () => ByRole(role => Light(role) is { } light && state.LightFallsAt(light) > StormAt[w] ? BesideLight(light, side) : null),
                jitter: 0f, arrivalTime: StormAt[w] + 1.8f);
        }
        ai.Move(HiemalRayAt[1] + LeaveTheSecondRayPuddle, () => ByRole(role => Light(role) is { } light ? Point(light * 2, VeilRadius) : null), jitter: 0f);
        ai.Move(StormAt[^1] + 0.35f, () => ByRole(role => Light(role) is { } light && state.LightFallsAt(light) > StormAt[^1] ? PastTheCentre(light) : null), jitter: 0f);
        ai.Move(StormAt[^1] + StormResolves + 0.3f, () => ByRole(role => Light(role) is { } light ? Point(light * 2, VeilRadius) : null), jitter: 0f);
    }

    private const int StormsDodgedBesideTheLight = 4;
    private const float LeaveTheSecondRayPuddle = 1.28f;
    private const float StormResolves = 3.3f;

    private static Vector3 PastTheCentre(int light) => Point(light * 2 + 4, 1);

    private static int? Light(PartyRole role) => role switch
    {
        PartyRole.MainTank => 0,
        PartyRole.MeleeDpsA => 1,
        PartyRole.OffTank => 2,
        PartyRole.MeleeDpsB => 3,
        _ => null,
    };

    private static int Dark(PartyRole role) => role switch
    {
        PartyRole.ShieldHealer => 0,
        PartyRole.CasterDps => 1,
        PartyRole.RegenHealer => 2,
        _ => 3,
    };

    private static float Octant(PartyRole role) => Light(role) is { } light ? light * 2 : Dark(role) * 2 + 1;

    private static Vector3 BesideLight(int light, int side)
    {
        var tangent = Point(light * 2 + 2, 1);
        return Point(light * 2, MeleeRadius) + side * StormSidestep * tangent;
    }

    private static Vector3 BehindDark(PartyRole role) => Point(Dark(role) * 2 + 1, BehindDarkRadius);

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
