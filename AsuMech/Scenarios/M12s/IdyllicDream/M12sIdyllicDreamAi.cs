using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Core.UserActions;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.IdyllicDream.M12sIdyllicDreamScenario;
using State = AsuMech.Scenarios.M12s.IdyllicDream.M12sIdyllicDreamState;

namespace AsuMech.Scenarios.M12s.IdyllicDream;

public sealed class M12sIdyllicDreamAi : IScenarioAi<State>
{
    public string Name => "DN";

    private const float NearTheBoss = 4f;
    private const float QuadrantWait = 6f;
    private const float GrabRadius = 9.5f;
    private const float ReplayX = 12f;
    private const float ReplayZ = 3f;
    private const float DefaEdge = 18f;
    private const float FarStack = 16.5f;
    private const float NumberMarker = 8.6f;
    private const float TowerEdge = 2.5f;
    private const float WindLean = 0.8f;
    private const float PortentInner = 6.5f;
    private const float PortentMelee = 9.5f;
    private const float PortentRanged = 14f;
    private const float PortentNorth = -6f;
    private const float PortentWind = -1f;
    private const float PortentSouth = 9f;
    private const float AwayFromTheDefa = 2.5f;
    private const float PlatformCentre = 14f;
    private const float PlatformSafeZ = 7.5f;
    private const float PlatformSafeX = 8.5f;
    private const float TearSafeX = 8f;
    private const float TearSafeZ = -1f;

    private State state = null!;

    public void Run(State stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => ByRole(StartNearTheBoss), jitter: 0f);
        ai.Move(ReplicationCastAt[1] + 1f, () => ByRole(WaitInYourQuadrant), jitter: 0f);
        ai.Move(BossTethersAt + 0.15f, () => ByRole(GrabYourBossClone), jitter: 0f, sprint: true);
        ai.Move(TethersLockAt + 0.2f, () => ByRole(DodgeTheRememberedCleaves), jitter: 0f, sprint: true);
        ai.Move(DownfallCastAt + 3f, () => ByRole(StandInYourAssignedTower), jitter: 0f);
        ai.Move(ArcanumSpreadAt + 0.5f, () => ByRole(SwapOffATripleIfTheLightHitYou), jitter: 0f);
        ai.Move(RestoreAt[0] + 0.15f, () => ByRole(role => TetherSlotSpot(0, role)), jitter: 0f, sprint: true);
        for (var s = 1; s < SlotAt.Length; s++)
        {
            var slot = s;
            ai.Move(SlotAt[slot - 1] + DefaHitDelay + 0.13f, () => ByRole(role => TetherSlotSpot(slot, role)), jitter: 0f, sprint: true);
        }
        ai.Move(SlotAt[^1] + DefaHitDelay + 0.13f, () => ByRole(SoakYourTowerByItsKind), jitter: 0f, sprint: true);
        ai.Move(TowerStatusesAt + 0.15f, () => ByRole(EarthSoakerStepsToThePlatformCentre), jitter: 0f, sprint: true);
        ai.Move(WindLandsAt + 0.2f, () => ByRole(WindSoakerTakesItsPortentSpot), jitter: 0f, sprint: true);
        ai.UseAction(141f, RegenHealer, Esuna.ActionId, () => Doomed(world, 0));
        ai.UseAction(141.1f, ShieldHealer, Esuna.ActionId, () => Doomed(world, 1));
        ai.Move(143.2f, () => ByRole(TakeYourPortentSpot), jitter: 0f, sprint: true);
        ai.Move(RestoreAt[1] + 0.15f, () => ByRole(role => ReenactedStackSpot(0, role)), jitter: 0f, sprint: true);
        ai.Move(ReenactAt[0] + ReenactedHeavySlamDelay + 0.2f, () => ByRole(GoToThePlatformTheKickSpares), jitter: 0f, sprint: true);
        ai.Move(RestoreAt[2] + 0.15f, () => ByRole(role => ReenactedStackSpot(1, role)), jitter: 0f, sprint: true);
        ai.Move(ReenactAt[1] + ReenactedHeavySlamDelay + 0.2f, () => ByRole(DodgeTheTearClone), jitter: 0f, sprint: true);
    }

    private static int Side(PartyRole role) => M12sConstants.LightParties[0].Contains(role) ? -1 : 1;

    private static bool Inner(PartyRole role) => role is MainTank or OffTank or MeleeDpsA or MeleeDpsB;

    private static int ZSign(PartyRole role) => role.IsDps() ? -Side(role) : Side(role);

    private static Vector2 Offset(PartyRole role)
    {
        var angle = (int)role * MathF.PI / 4f;
        return new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * 1.1f;
    }

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);

    private static Vector2? StartNearTheBoss(PartyRole role) => Offset(role) / 1.1f * NearTheBoss;

    private bool TakesStack(PartyRole role) => state.CloneDirection[role] < 4;

    private int Number(PartyRole role) => state.CloneDirection[role] % 4;

    private bool InGroupA(PartyRole role) => Number(role) < 2;

    private int BossCloneOf(PartyRole role)
    {
        var quadrant = Number(role);
        return new[] { 2 * quadrant, 2 * quadrant + 1 }.First(d => state.IsDefaClone(d) != TakesStack(role));
    }

    private Vector2? WaitInYourQuadrant(PartyRole role)
    {
        var angle = (2 * Number(role) + 0.5f) * MathF.PI / 4f;
        var spot = new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * QuadrantWait;
        var aside = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (TakesStack(role) ? 0.7f : -0.7f);
        return spot + aside;
    }

    private Vector2? GrabYourBossClone(PartyRole role) => Flat(State.OnRing(BossCloneOf(role), GrabRadius));

    private Vector2? DodgeTheRememberedCleaves(PartyRole role)
    {
        var z = -MathF.Sign(state.EastWestConesZ) * ReplayZ;
        var outer = Inner(role) ? 0f : 1.5f;
        var row = role.IsDps() ? 0.8f : -0.8f;
        return new Vector2(Side(role) * (ReplayX - 0.5f + outer), z + row);
    }

    private DreamTower AssignedTower(PartyRole role) => state.TowerAt(Side(role), Inner(role), ZSign(role));

    private DreamTower FinalTower(PartyRole role)
    {
        var tower = AssignedTower(role);
        return state.IsSpreadRole(role) == tower.Triple ? state.TowerAt(tower.Side, tower.Inner, -tower.ZSign) : tower;
    }

    private Vector2? StandInYourAssignedTower(PartyRole role) => Flat(AssignedTower(role).Centre);

    private Vector2? SwapOffATripleIfTheLightHitYou(PartyRole role) => Flat(FinalTower(role).Centre);

    private Vector2? TetherSlotSpot(int slot, PartyRole role)
    {
        var north = InGroupA(role) ? -1f : 1f;
        var mine = BossCloneOf(role) % 4 == slot;
        if (state.IsDefaClone(slot))
            return mine ? new Vector2(-north * DefaEdge, 0f) : new Vector2(0f, north * FarStack) + Offset(role);
        return new Vector2(0f, north * NumberMarker) + Offset(role) * 0.8f;
    }

    private Vector2? SoakYourTowerByItsKind(PartyRole role)
    {
        var tower = FinalTower(role);
        var centre = Flat(tower.Centre);
        return tower.Kind switch
        {
            TowerKind.Doom => centre + new Vector2(0f, tower.ZSign * TowerEdge),
            TowerKind.Wind => centre - new Vector2(tower.Side * TowerEdge, tower.ZSign * WindLean),
            _ => centre,
        };
    }

    private Vector2? EarthSoakerStepsToThePlatformCentre(PartyRole role)
    {
        var tower = FinalTower(role);
        return tower.Kind == TowerKind.Earth ? new Vector2(tower.Side * PlatformCentre, 0f) : null;
    }

    private Vector2? WindSoakerTakesItsPortentSpot(PartyRole role) => FinalTower(role).Kind == TowerKind.Wind ? TakeYourPortentSpot(role) : null;

    private Vector2? TakeYourPortentSpot(PartyRole role)
    {
        var tower = FinalTower(role);
        var s = tower.Side;
        return tower.Kind switch
        {
            TowerKind.Doom => new Vector2(s * PortentInner, PortentNorth),
            TowerKind.Wind => new Vector2(-s * PortentInner, PortentWind),
            _ when tower.Inner => new Vector2(s * PortentMelee, PortentNorth),
            _ => new Vector2(s * PortentRanged, PortentSouth),
        };
    }

    private static SimCharacter? Doomed(SimWorld world, int index)
        => Enum.GetValues<PartyRole>().Select(r => world.Party.Get(r)).OfType<SimCharacter>()
            .Where(m => m.IsAlive() && m.HasStatus(M12sConstants.StatusId.Doom)).Skip(index).FirstOrDefault();

    private Vector2? ReenactedStackSpot(int set, PartyRole role)
    {
        var first = set == 0 ? state.CardinalClonesFirst : !state.CardinalClonesFirst;
        var stacks = Enumerable.Range(0, 4).Where(d => State.IsCardinal(d) == first).ToArray();
        var defas = stacks.Select(d => d + 4).ToArray();
        var stack = role.IsTank() ? stacks[1] : stacks[0];
        var clone = Flat(State.PlayerCloneSpot(stack));
        var nearestDefa = defas.Select(d => Flat(State.PlayerCloneSpot(d))).OrderBy(p => Vector2.Distance(p, clone)).First();
        return clone + Vector2.Normalize(clone - nearestDefa) * AwayFromTheDefa + Offset(role) * 0.5f;
    }

    private Vector2? GoToThePlatformTheKickSpares(PartyRole role)
    {
        var side = -state.KickPlatform;
        var spot = state.PlatformConesEastWest
            ? new Vector2(side * PlatformCentre, role.IsTank() ? PlatformSafeZ : -PlatformSafeZ)
            : new Vector2(side * PlatformSafeX, 0f);
        return spot + Offset(role);
    }

    private Vector2? DodgeTheTearClone(PartyRole role)
        => (state.TearTakesEastWestClone
            ? new Vector2(0f, TearSafeZ)
            : new Vector2(Side(role) * TearSafeX, State.TearCloneZ)) + Offset(role);

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
