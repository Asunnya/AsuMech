using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using Scenario = AsuMech.Scenarios.Ucob.P3Blackfire.UcobP3BlackfireScenario;
using State = AsuMech.Scenarios.Ucob.P3Blackfire.UcobP3BlackfireState;

namespace AsuMech.Scenarios.Ucob.P3Blackfire;

public sealed class UcobP3BlackfireAi : IScenarioAi<UcobP3BlackfireState>
{
    public string Name => "Party toward Nael, DPS counterclockwise, supports clockwise";

    private static readonly PartyRole[] Roles = Enum.GetValues<PartyRole>();

    private static readonly Vector2 MainTankOnBahamut = new(0f, -1.5f);
    private static readonly Vector2 PartyUnderBahamut = new(0f, 0f);

    private const float PackTowardNael = 19.5f;
    private const float LiquidHellLeadTowardNael = 21.5f;
    private const float RimTurnBearing = 28f;
    private const float RimTurnDistance = 19.5f;
    private const float NearTowerBearing = 45f;
    private const float FarTowerBearing = 135f;
    private const float TowerDistance = 14f;
    private const float StackDetourDistance = 9f;
    private const float StackBearing = 180f;
    private const float StackDistance = 7f;
    private const float GigaflareStackDistance = 3f;
    private const float MainTankGigaflareDistance = 2.5f;

    private const float StackUnderBahamut = 0.3f;
    private const float LeadTheLiquidHellsTowardNael = 13.02f;
    private const float SplitAlongTheRim = 17.9f;
    private const float TakeTowersOrDetourToTheStack = 19.15f;
    private const float StackOppositeNael = 22f;
    private const float RegroupForGigaflare = 25.9f;

    private State state = null!;
    private SimWorld world = null!;

    public void Run(UcobP3BlackfireState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(StackUnderBahamut, () => Spots(r => r == MainTank ? MainTankOnBahamut : PartyUnderBahamut), jitter: 0f);
        ai.Move(LeadTheLiquidHellsTowardNael, () => Spots(r => AroundNael(0f, r == state.LiquidHellTarget ? LiquidHellLeadTowardNael : PackTowardNael)), jitter: 0f);
        ai.Move(SplitAlongTheRim, () => Spots(r => AroundNael(Side(r) * RimTurnBearing, RimTurnDistance)), jitter: 0f, sprint: true);
        ai.Move(TakeTowersOrDetourToTheStack, () => Spots(TowerOrStackDetour), jitter: 0f, sprint: true);
        ai.Move(StackOppositeNael, () => Spots(r => StackTargeted(r) ? AroundNael(StackBearing, StackDistance) : TowerOrStackDetour(r)), jitter: 0f, sprint: true);
        ai.Move(RegroupForGigaflare, () => Spots(r => r == MainTank ? AroundNael(0f, MainTankGigaflareDistance) : AroundNael(StackBearing, GigaflareStackDistance)), jitter: 0f, sprint: true);
    }

    private static float Side(PartyRole role) => State.Supports.Contains(role) ? 1f : -1f;

    private bool StackTargeted(PartyRole role) => state.StackTargets.Contains(role);

    private Vector2 TowerOrStackDetour(PartyRole role)
    {
        if (StackTargeted(role)) return AroundNael(Side(role) * FarTowerBearing, StackDetourDistance);
        var group = State.Supports.Contains(role) ? State.Supports : State.Dps;
        var soakers = group.Where(r => !StackTargeted(r)).OrderBy(r => (int)r).ToList();
        var bearing = soakers.IndexOf(role) == 0 ? NearTowerBearing : FarTowerBearing;
        return AroundNael(Side(role) * bearing, TowerDistance);
    }

    private Vector2 AroundNael(float bearing, float distance)
    {
        var at = Scenario.AroundNael(state.NaelFrom, bearing, distance);
        return new Vector2(at.X, at.Z);
    }

    private static IAiMove Spots(Func<PartyRole, Vector2> spot)
    {
        var coords = new Vector2?[Roles.Length];
        foreach (var role in Roles) coords[(int)role] = spot(role);
        return AiMove.Create(coords).NaturalOrder();
    }
}
