using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Replication2.M12sReplication2Scenario;

namespace AsuMech.Scenarios.M12s.Replication2;

public sealed class M12sReplication2Ai : IScenarioAi<M12sReplication2State>
{
    public string Name => "Clone Zone";

    private enum Job { Boss, ConeEast, StackEast, DefamationEast, Nothing, DefamationWest, StackWest, ConeWest }

    private static readonly int[] WestPartySearch = [5, 4, 3, 2, 1, 0];
    private static readonly int[] EastPartySearch = [0, 1, 2, 3, 4, 5];
    private const float PickupRadius = 8.5f;

    private static readonly Vector2 WaitSouthOfTheBoss = new(0f, 4f);
    private static readonly Vector2 BossTetherOnTheBoss = new(0f, -1.5f);
    private static readonly Vector2 BossLandsNorth = new(0f, -11f);
    private static readonly Vector2 InnerCleaveWest = new(-2.5f, -19.3f);
    private static readonly Vector2 OuterCleaveWest = new(-6f, -17.5f);
    private static readonly Vector2 DefamationWest = new(-18.5f, 2f);
    private static readonly Vector2 DefamationSouth = new(0f, 18.5f);
    private static readonly Vector2 LightPartyStackWest = new(-7f, -5f);
    private static readonly Vector2 ConeBesideTheStackWest = new(-8.5f, -3.5f);
    private static readonly Vector2 BehindTheBoss = new(0f, -15.5f);
    private static readonly Vector2 PartnersWaitWest = new(-5.5f, -9.5f);
    private static readonly Vector2 NearDefamationWest = new(-4f, -9.5f);
    private static readonly Vector2 NearPartnerWest = new(-7.5f, -9.5f);
    private static readonly Vector2 FarDefamationWest = new(-15f, -9.5f);
    private static readonly Vector2 FarPartnerWest = new(-11.5f, -9.5f);
    private static readonly Vector2 MeleeNortheast = new(13f, -9.2f);
    private static readonly Vector2 RangedNorthwest = new(-12.5f, -12.5f);
    private static readonly Vector2 MeleeEastStack = new(13.5f, 0f);
    private static readonly Vector2 RangedWestStack = new(-13.5f, 0f);
    private static readonly Vector2 MeleeDodgeNortheast = new(11.4f, -11.5f);

    private M12sReplication2State state = null!;
    private SimWorld world = null!;

    public void Run(M12sReplication2State stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => WaitSouthOfTheBoss + Spread(role, 1.5f)));
        ai.Move(BossClonesShowAt + 0.1f, () => ByRole(TakeYourTether), jitter: 0f, sprint: true);
        ai.Move(TethersLockAt + 0.2f, () => ByRole(LineUpForFirefall), arrivalTime: JumpAt - 0.4f);
        ai.Move(ManaBurstAt + 0.2f, () => ByRole(StackWithYourLightParty));
        world.Events.Add(ProjectionAt - 0.5f, TurnTheConesAway);
        ai.Move(ProjectionAt + 0.15f, () => ByRole(role => BehindTheBoss + Spread(role, 1f)), sprint: true);
        ai.Move(SnakingKickCastAt + SnakingKickResolves + 0.3f, () => ByRole(role => WaitForNetherwrath(role, null)));
        ai.Move(NetherwrathCastAt + 0.2f, () => ByRole(role => WaitForNetherwrath(role, state.NetherwrathFar)), jitter: 0.2f);
        ai.Move(TimelessSpiteAt + 0.3f, () => ByRole(role => IsMelee(role) ? MeleeNortheast : RangedNorthwest), arrivalTime: ReplayAt[1] + ReplayDefamationAfter - 0.15f);
        ai.Move(ReplayAt[1] + ReplayConeAfter + 0.1f, () => ByRole(role => IsMelee(role) ? MeleeEastStack : RangedWestStack));
        ai.Move(ReplayAt[2] + ReplayStackAfter + 0.15f, () => ByRole(role => IsMelee(role) ? MeleeDodgeNortheast : null), sprint: true);
        ai.Move(ReplayAt[3] + ReplayConeAfter + 0.5f, () => ByRole(role => WaitSouthOfTheBoss + Spread(role, 1.5f)));
    }

    private Job JobOf(PartyRole role) => (Job)state.CloneSpot[role];

    private static bool InTheWestLightParty(Job job) => job is Job.Nothing or Job.DefamationWest or Job.StackWest or Job.ConeWest;

    private static CloneMechanic? MechanicOf(Job job) => job switch
    {
        Job.ConeEast or Job.ConeWest => CloneMechanic.Cone,
        Job.StackEast or Job.StackWest => CloneMechanic.Stack,
        Job.DefamationEast or Job.DefamationWest => CloneMechanic.Defamation,
        _ => null,
    };

    private static bool IsMelee(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;

    private static Vector2 OnYourSide(Vector2 west, Job job) => InTheWestLightParty(job) ? west : new Vector2(-west.X, west.Y);

    private Vector2? TakeYourTether(PartyRole role)
    {
        var job = JobOf(role);
        if (job == Job.Boss) return BossTetherOnTheBoss;
        if (job == Job.Nothing) return DefamationSouth;
        var mechanic = MechanicOf(job)!.Value;
        var search = InTheWestLightParty(job) ? WestPartySearch : EastPartySearch;
        var clone = search.First(i => state.CloneMechanics[i] == mechanic);
        var at = M12sReplication2State.BossClonePosition(clone) * (PickupRadius / M12sReplication2State.BossCloneRadius);
        return new Vector2(at.X, at.Z);
    }

    private Vector2? LineUpForFirefall(PartyRole role)
    {
        var job = JobOf(role);
        return job switch
        {
            Job.Boss => BossLandsNorth,
            Job.Nothing => DefamationSouth,
            Job.ConeEast or Job.ConeWest => OnYourSide(InnerCleaveWest, job),
            Job.StackEast or Job.StackWest => OnYourSide(OuterCleaveWest, job),
            _ => OnYourSide(DefamationWest, job),
        };
    }

    private Vector2? StackWithYourLightParty(PartyRole role)
    {
        var job = JobOf(role);
        return job is Job.ConeEast or Job.ConeWest ? OnYourSide(ConeBesideTheStackWest, job) : OnYourSide(LightPartyStackWest, job);
    }

    private void TurnTheConesAway()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var job = JobOf(role);
            if (job is not (Job.ConeEast or Job.ConeWest) || world.Party.Get(role) is not { } member || !world.Party.IsBotDriven(member)) continue;
            var away = OnYourSide(new Vector2(-5f, 5f), job);
            member.Face(member.Position + new Vector3(away.X, 0f, away.Y));
        }
    }

    private Vector2? WaitForNetherwrath(PartyRole role, bool? far)
    {
        var job = JobOf(role);
        return job switch
        {
            Job.ConeEast or Job.ConeWest => OnYourSide(InnerCleaveWest, job),
            Job.StackEast or Job.StackWest => OnYourSide(OuterCleaveWest, job),
            Job.DefamationEast or Job.DefamationWest => OnYourSide(far switch { null => PartnersWaitWest, true => FarDefamationWest, false => NearDefamationWest }, job),
            _ => OnYourSide(far switch { null => PartnersWaitWest, true => FarPartnerWest, false => NearPartnerWest }, job),
        };
    }

    private static Vector2 Spread(PartyRole role, float radius)
    {
        var angle = (int)role * MathF.PI / 4f;
        return new Vector2(radius * MathF.Sin(angle), -radius * MathF.Cos(angle));
    }

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
