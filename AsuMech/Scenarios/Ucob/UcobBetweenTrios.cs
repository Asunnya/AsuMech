using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Ucob;

// Between two trios Bahamut Prime stays in the middle on one tank, with Flare Breaths and Flattens.
// A P3 part carrying straight on from the previous one plays that stretch before its own t=0, so the
// trios keep the fight's pace; played on its own the part starts as it always did. Buster times are
// the part's own, so they are negative before its t=0.
public sealed record UcobBetweenTrios(float Seconds, PartyRole Tank, params (float At, EnemyAction Action)[] Busters)
{
    private const float TankDistance = 4f;
    private const float BehindDistance = 3f;
    private const float BehindSpacing = 0.8f;

    // Last in Run: everything already scheduled keeps its time. `spawn` must not spawn twice, as the
    // part's own t=0 calls it again.
    public void Play(SimWorld world, Action spawn, Func<SimEnemy?> bahamut)
    {
        if (!world.Continued) return;
        world.Events.Delay(Seconds);
        world.Events.Add(0f, () =>
        {
            spawn();
            var tank = world.Party.Get(Tank);
            bahamut()?.SetTarget(tank, follow: false);
            bahamut()?.KeepFacing(tank);
            if (tank is not null) TakeUptimeSpots(world, tank);
        });
        foreach (var (at, action) in Busters)
            world.Events.Add(Seconds + at, () =>
            {
                if (world.Party.Get(Tank) is { } tank && tank.IsAlive()) bahamut()?.Cast(action, tank);
            });
    }

    // The breath's cone points at the tank: the tank turns Bahamut away from the side the party
    // already stands on, and the rest gather behind him.
    private static void TakeUptimeSpots(SimWorld world, SimCharacter tank)
    {
        var ai = new AiManager(world);
        var others = world.Party.ActiveMembers().Where(m => m != tank && m.IsAlive()).ToList();
        var centre = others.Count == 0 ? Vector3.Zero : others.Aggregate(Vector3.Zero, (a, m) => a + m.Position) / others.Count;
        var front = new Vector3(-centre.X, 0f, -centre.Z);
        front = front.LengthSquared() < 1f ? new Vector3(0f, 0f, -1f) : Vector3.Normalize(front);
        var side = new Vector3(-front.Z, 0f, front.X);
        ai.MoveNow(tank, front * TankDistance, AiManager.SprintSpeed);
        for (var i = 0; i < others.Count; i++)
            ai.MoveNow(others[i], -front * BehindDistance + side * ((i - (others.Count - 1) / 2f) * BehindSpacing), AiManager.SprintSpeed);
    }
}
