using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.AlleyOopTwo;

public sealed class M10sAlleyOopTwoState
{
    public bool DoubleDip { get; }
    // Pyrotation's stacks follow a healer.
    public PartyRole PyrotationTarget { get; }

    // Where each protean helper aimed its first cone, by helper.
    public List<float> FirstCones { get; } = [];
    public List<Vector3> InfernoPuddles { get; } = [];

    public M10sAlleyOopTwoState(Rng rng, M10sAlleyOopTwoStateOverrides? overrides = null)
    {
        overrides ??= new M10sAlleyOopTwoStateOverrides();
        var doubleDip = rng.NextBool();
        DoubleDip = overrides.DoubleDip ?? doubleDip;
        var healer = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        PyrotationTarget = overrides.PyrotationTarget ?? healer;
    }
}
