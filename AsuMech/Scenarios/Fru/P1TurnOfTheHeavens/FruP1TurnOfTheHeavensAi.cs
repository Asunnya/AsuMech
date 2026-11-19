using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruCyclonicBreakDance;
using static AsuMech.Scenarios.Fru.P1TurnOfTheHeavens.FruP1TurnOfTheHeavensState;

namespace AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;

public sealed class FruP1TurnOfTheHeavensAi : IScenarioAi<FruP1TurnOfTheHeavensState>
{
    public string Name => "NA · LesBin";
    public string Group => "NA";

    private static readonly PartyRole[] NorthOfWestStrikeAfterCyclonic =
        [PartyRole.MainTank, PartyRole.CasterDps, PartyRole.RegenHealer, PartyRole.PhysRangedDps];

    private FruP1TurnOfTheHeavensState state = null!;

    public void Run(FruP1TurnOfTheHeavensState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        Dance(ai, 10.6f, state.CyclonicFire);
        ai.Move(16.8f, () => ByRole(role => OutsideBothStrikes(NorthOfWestStrikeAfterCyclonic.Contains(role))), sprint: true);
        ai.Move(20.8f, () => ByRole(role => OutsideBothStrikes(StacksNorth(role))), arrivalTime: 24.8f);
        ai.Move(24.75f, () => ByRole(role => IntoTheNorthStrike(StacksNorth(role))), arrivalTime: 26.4f);
        ai.Move(29.15f, () => ByRole(role => SinsmokeStack(StacksNorth(role))), arrivalTime: 32.6f);
    }

    private float SafeSide => state.SmallHalosWest ? -1f : 1f;

    private Vector3 OutsideBothStrikes(bool north) => new(SafeSide * 7f, 0, north ? -13f : 13f);

    private Vector3 IntoTheNorthStrike(bool north) => new(SafeSide * 1.5f, 0, north ? -7f : 7f);

    private Vector3 SinsmokeStack(bool north) => new(SafeSide * 5.5f, 0, north ? -4.5f : 4.5f);

    private bool StacksNorth(PartyRole role)
    {
        var tetheredSupports = state.Tethered.Where(IsSupport).ToList();
        if (tetheredSupports.Count == 2)
        {
            if (role == tetheredSupports.Max()) return false;
            if (role == PartyRole.MeleeDpsA) return true;
        }
        if (tetheredSupports.Count == 0)
        {
            if (role == state.Tethered.Max()) return true;
            if (role == PartyRole.MainTank) return false;
        }
        return IsSupport(role);
    }
}
