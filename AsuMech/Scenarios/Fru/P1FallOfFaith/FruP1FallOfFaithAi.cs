using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruCyclonicBreakDance;

namespace AsuMech.Scenarios.Fru.P1FallOfFaith;

public sealed class FruP1FallOfFaithAi : IScenarioAi<FruP1FallOfFaithState>
{
    public string Name => "NA · LesBin";
    public string Group => "NA";

    private static readonly PartyRole[] CongaOrder =
    [
        PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MainTank, PartyRole.OffTank,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];
    private static readonly PartyRole[] FlexSoakers = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps];
    private static readonly float[] TowerZ = [-9.18f, 0f, 9.18f];
    private const float HolderX = 5.5f;

    private FruP1FallOfFaithState state = null!;

    public void Run(FruP1FallOfFaithState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        ai.Move(0.5f, () => ByRole(role => FaithSpot(role, OnWest(role) ? 0 : 1)), jitter: 0f, arrivalTime: 11.5f);
        ai.Move(16.45f, () => ByRole(role => OnWest(role) ? FaithSpot(role, 2) : null), jitter: 0f, arrivalTime: 20.5f);
        ai.Move(19.45f, () => ByRole(role => OnWest(role) ? null : FaithSpot(role, 3)), jitter: 0f, arrivalTime: 23f);
        ai.Move(24.5f, () => ByRole(UnderFatebreaker), arrivalTime: 30f);
        ai.Move(45.4f, () => ByRole(OutsideTheCentreLine), arrivalTime: 51.3f);
        if (state.TowersFire) ai.Move(51.6f, () => ByRole(IntoTheCentreLine), jitter: 0.2f, arrivalTime: 53.2f);
        ai.Move(57.2f, () => ByRole(UnderFatebreaker), arrivalTime: 61f);
    }

    private IReadOnlyList<PartyRole> Untethered => CongaOrder.Where(r => state.FaithIndex(r) < 0).ToList();

    private bool OnWest(PartyRole role)
    {
        var faith = state.FaithIndex(role);
        return faith >= 0 ? faith % 2 == 0 : Untethered.Take(2).Contains(role);
    }

    private Vector3 FaithSpot(PartyRole role, int faith)
    {
        var side = faith % 2 == 0 ? -1f : 1f;
        if (state.Tethered[faith] == role) return new(side * HolderX, 0, 0);
        var sideUntethered = Untethered.Skip(faith % 2 == 0 ? 0 : 2).Take(2).ToList();
        var first = sideUntethered.IndexOf(role) == 0;
        var otherTether = sideUntethered.IndexOf(role) < 0;
        if (state.FaithFire[faith])
            return otherTether ? new(side * 7f, 0, 0) : new(side * 7.6f, 0, first ? -0.8f : 0.8f);
        return otherTether ? new(side * 7.5f, 0, 0) : new(side * HolderX, 0, first ? -2.5f : 2f);
    }

    private static Vector3 UnderFatebreaker(PartyRole role) => Spoke(role) * (4f / Spoke(role).Length());

    private int? TowerOf(PartyRole role)
    {
        switch (role)
        {
            case PartyRole.RegenHealer: return 0;
            case PartyRole.ShieldHealer: return 1;
            case PartyRole.CasterDps: return 2;
            case PartyRole.MainTank or PartyRole.OffTank: return null;
        }
        var need = state.TowerSoakers.Select(n => n - 1).ToArray();
        foreach (var flex in FlexSoakers)
        {
            var tower = Array.FindIndex(need, n => n > 0);
            need[tower]--;
            if (flex == role) return tower;
        }
        return null;
    }

    private static bool IsTank(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank;

    private Vector3 OutsideTheCentreLine(PartyRole role)
    {
        var side = state.TowerSide;
        if (IsTank(role))
            return new(-side * (state.TowersFire ? 7f : 13f), 0, role == PartyRole.MainTank ? -5.5f : 5.5f);
        var tower = TowerOf(role)!.Value;
        if (state.TowersFire) return new(side * 7f, 0, TowerZ[tower]);
        return tower == 1 ? new(side * 15.5f, 0, 0) : new(side * 14f, 0, TowerZ[tower]);
    }

    private Vector3 IntoTheCentreLine(PartyRole role)
    {
        var side = IsTank(role) ? -state.TowerSide : state.TowerSide;
        return new(side * 0.5f, 0, IsTank(role) ? (role == PartyRole.MainTank ? -5.5f : 5.5f) : TowerZ[TowerOf(role)!.Value]);
    }
}
