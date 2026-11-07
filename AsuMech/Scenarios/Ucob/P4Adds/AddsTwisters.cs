using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob.P4Adds;

// Twintania's Twister: as the cast ends a twister drops under four players; the first player to
// touch one launches everyone near it into the death wall.
public sealed class AddsTwisters(SimWorld world, UcobHelperPool helpers)
{
    public const int Count = 4;
    public const float CastSeconds = 1.7f;
    public const float TriggerRadius = 1.25f;
    public const float BlastRadius = 8f;
    public const float Arms = 0.9f;
    public const float Fades = 5.56f;
    private const float Gone = 6.62f;
    // Bots steer this far around an armed twister.
    private const float AvoidRadius = TriggerRadius + 0.6f;

    private sealed class Twister(Vector2 at, SimEventObject? eobj, CircleObstacle obstacle, float armsAt, float fadesAt)
    {
        public Vector2 At { get; } = at;
        public SimEventObject? EObj { get; } = eobj;
        public CircleObstacle Obstacle { get; } = obstacle;
        public float ArmsAt { get; } = armsAt;
        public float FadesAt { get; } = fadesAt;
        public bool Spent { get; set; }
    }

    private readonly List<Twister> twisters = [];
    private readonly List<SimEventObject> eobjs = [];

    public void Cast(SimEnemy? twintania) => UcobSim.CastSelf(twintania, ActionId.Twister, CastSeconds);

    public void Drop(SimEnemy? twintania, IEnumerable<PartyRole> targets)
    {
        UcobSim.PlayEffect(twintania, ActionId.Twister, 1.1f);
        var now = world.Events.Elapsed;
        foreach (var role in targets)
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            var at = UcobSim.Flat(member.Position);
            var eobj = UcobSim.SpawnEObj(world, EObjId.Twister, at);
            if (eobj != null) eobjs.Add(eobj);
            var twister = new Twister(at, eobj, world.Obstacles.Add(new CircleObstacle(at, AvoidRadius)), now + Arms, now + Fades);
            twisters.Add(twister);
            world.Events.Add(Fades, () =>
            {
                eobj?.FadeOut();
                world.Obstacles.Remove(twister.Obstacle);
            });
            world.Events.Add(Gone, () =>
            {
                eobj?.Despawn();
                twisters.Remove(twister);
            });
        }
    }

    public void Tick(float now)
    {
        foreach (var twister in twisters.Where(t => !t.Spent && now >= t.ArmsAt && now < t.FadesAt).ToList())
        {
            var alive = UcobSim.AliveMembers(world.Party).ToList();
            var toucher = alive.FirstOrDefault(m => Vector2.Distance(UcobSim.Flat(m.Position), twister.At) <= TriggerRadius);
            if (toucher == null) continue;
            twister.Spent = true;
            twister.EObj?.Despawn();
            world.Obstacles.Remove(twister.Obstacle);
            helpers.Play(ActionId.TwisterBlast, UcobSim.ToWorld(twister.At));
            foreach (var hit in alive.Where(m => Vector2.Distance(UcobSim.Flat(m.Position), twister.At) <= BlastRadius))
                hit.Die(ActionId.TwisterBlast, ReferenceEquals(hit, toucher)
                    ? "touched a Twister (move off your spot as the cast ends)"
                    : "launched into the death wall by a Twister someone touched");
        }
    }

    public void Clear()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        foreach (var twister in twisters) world.Obstacles.Remove(twister.Obstacle);
        twisters.Clear();
    }
}
