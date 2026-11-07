using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Scenarios.Ucob.UcobSim;
using State = AsuMech.Scenarios.Ucob.P2Nael.UcobP2NaelState;

namespace AsuMech.Scenarios.Ucob.P2Nael;

// Nael deus Darnus from Heavensfall to the Ravensbeak that ends the dragons' elements, then the
// party pushes her and she leaves with her dragons, as a clear does. Her HP is not simulated. The
// main tank holds her throughout.
public sealed class UcobP2NaelScenario : IScenario
{
    public string Name => "Nael deus Darnus";
    public IPhase Phase => UcobZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP2NaelAi()];

    private readonly UcobP2NaelSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public const float Heavensfall = 8.41f;
    public static readonly float[] ThermionicBurstCasts = [12.39f, 16.55f];
    public const float ThermionicBurstCastSeconds = 3f;
    public static readonly float[] MeteorStreams = [13.95f, 17.18f];
    public const float DalamudDive = 19.95f;
    public static readonly float[] ClawStarts = [22.10f, 58.68f, 94.96f];
    public const float DragonsArrive = 28.04f;
    private const float BahamutsFavor = 28.18f;
    public static readonly float[] ThunderMarks = [38.18f, 63.33f, 82.48f, 109.62f];
    public const float ThunderstruckAfterMark = 1.03f;
    public const float ThunderstruckSeconds = 5.09f;
    public static readonly float[] Quotes = [38.81f, 66.81f, 99.81f];
    public static readonly float[][] QuoteMoves = [[44.43f, 47.52f], [72.82f, 75.86f], [106.17f, 109.22f]];
    public static readonly float[] IceHits =
    [
        41.22f, 43.27f, 45.32f, 47.38f, 56.53f, 58.59f, 60.64f, 62.70f,
        82.84f, 84.90f, 86.96f, 89.02f, 104.16f, 106.22f, 108.28f, 110.34f,
        118.47f, 119.59f, 120.70f, 121.82f, 122.94f, 124.06f, 125.17f, 126.29f,
    ];
    public static readonly float[] FireTethers = [45.10f, 60.24f, 86.38f, 107.52f];
    public const float FireballAfterTether = 5.10f;
    public static readonly float[][] WingsCasts = [[48.19f, 52.34f], [76.49f, 80.65f, 84.81f], [112.93f, 117.09f, 121.24f]];
    public const float WingsCastSeconds = 2.99f;
    public const float CleanseAppears = 0.5f;
    private const float CleanseLasts = 11.3f;
    public static readonly float[] Deathstorms = [48.19f, 77.34f, 113.47f];
    public const float DoomAfterDeathstorm = 1.03f;
    public const float RavensbeakCast = 130.36f;
    public const float NaelUntargetable = 136.41f;
    public const float LeaveAnimations = 136.50f;
    public const float DragonsLeave = 138.59f;
    public const float End = 139f;

    // Checked just before the status itself runs out and is gone.
    private const float DoomCheckLead = 0.05f;
    private const int HelperPoolSize = 24;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private State state = null!;
    private SimEnemy? nael;
    private SimEnemy? firehorn;
    private SimEnemy? thunderwing;
    private SimEnemy? tail;
    private readonly List<SimEnemy> spawned = [];
    private UcobHelperPool helpers = null!;
    private readonly List<SimEventObject> eobjs = [];
    private readonly List<(Vector2 At, SimEventObject? EObj, float Until)> cleanses = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new State(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        helpers = new UcobHelperPool(world, BNpcNameId.NaelDeusDarnus, spawned);
        eobjs.Clear();
        cleanses.Clear();
        nael = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnNael);
        world.Events.Add(0f, () => helpers.Fill(HelperPoolSize));

        world.Events.Add(Heavensfall, HeavensfallHit);
        for (var i = 0; i < ThermionicBurstCasts.Length; i++)
        {
            var offset = i * MathF.PI / 8f;
            world.Events.Add(ThermionicBurstCasts[i], () => CastThermionicBursts(offset));
            world.Events.Add(ThermionicBurstCasts[i] + ThermionicBurstCastSeconds, () => ThermionicBursts(offset));
        }
        for (var i = 0; i < MeteorStreams.Length; i++)
        {
            var targets = state.MeteorOrder.Skip(4 * i).Take(4).ToList();
            world.Events.Add(MeteorStreams[i], () => MeteorStream(targets));
        }
        world.Events.Add(DalamudDive, DalamudDiveHit);
        foreach (var start in ClawStarts)
            for (var hit = 0; hit < UcobNaelAttacks.ClawHits; hit++)
                world.Events.Add(start + hit * UcobNaelAttacks.ClawInterval, BahamutsClaw);
        world.Events.Add(DragonsArrive, SpawnDragons);
        world.Events.Add(BahamutsFavor, () => CastSelf(nael, ActionId.BahamutsFavor, 2.7f));

        for (var i = 0; i < ThunderMarks.Length; i++)
        {
            var targets = state.ThunderTargets[i];
            world.Events.Add(ThunderMarks[i], () => MarkThunderstruck(targets));
            world.Events.Add(ThunderMarks[i] + ThunderstruckAfterMark + ThunderstruckSeconds, () => ThunderstruckSplash(targets));
        }

        var beams = 0;
        for (var i = 0; i < Quotes.Length; i++)
        {
            var quote = state.Quotes[i];
            world.Events.Add(Quotes[i], () => world.Announce($"Nael deus Darnus: {quote.Text}"));
            foreach (var (move, at) in quote.Moves.Zip(QuoteMoves[i]))
            {
                var beam = move == NaelMove.ThermionicBeam ? beams++ : -1;
                world.Events.Add(at, () => NaelMoveHit(move, beam));
            }
        }

        for (var i = 0; i < IceHits.Length; i++)
        {
            var target = state.IceOrder[i % state.IceOrder.Count];
            world.Events.Add(IceHits[i], () => Iceball(target));
        }
        for (var i = 0; i < FireTethers.Length; i++)
        {
            var target = state.FireTargets[i];
            SimTether? tether = null;
            world.Events.Add(FireTethers[i], () => tether = TetherFire(target));
            world.Events.Add(FireTethers[i] + FireballAfterTether, () =>
            {
                tether?.Despawn();
                DragonFireball(target);
            });
        }
        for (var set = 0; set < WingsCasts.Length; set++)
        {
            for (var i = 0; i < WingsCasts[set].Length; i++)
            {
                var target = state.WingsTargets[set][i];
                Vector2? at = null;
                world.Events.Add(WingsCasts[set][i], () => at = CastWings(target));
                world.Events.Add(WingsCasts[set][i] + WingsCastSeconds, () => { if (at is { } spot) Wings(spot); });
            }
            var dooms = state.DoomTargets[set];
            var seconds = State.DoomSeconds[set];
            world.Events.Add(Deathstorms[set], () => Deathstorm(dooms));
            world.Events.Add(Deathstorms[set] + DoomAfterDeathstorm, () => Doom(dooms, seconds));
        }

        world.Events.Add(RavensbeakCast, () => UcobNaelAttacks.CastRavensbeak(nael, party.Get(PartyRole.MainTank)));
        world.Events.Add(RavensbeakCast + UcobNaelAttacks.RavensbeakCastSeconds, () => UcobNaelAttacks.Ravensbeak(damage, nael, party.Get(PartyRole.MainTank)));
        world.Events.Add(NaelUntargetable, () => nael?.SetTargetable(false));
        world.Events.Add(LeaveAnimations, PlayLeaveAnimations);
        world.Events.Add(DragonsLeave, DespawnDragons);
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        if (nael is { IsCasting: false, IsMoving: false } boss && party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive())
            boss.Face(tank);
        CleanseDooms(world.Events.Elapsed);
    }

    private void SpawnNael()
    {
        nael = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.NaelDeusDarnus,
            NameId: BNpcNameId.NaelDeusDarnus,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Visibility: SpawnVisibility.HiddenUntilShown,
            Placement: new Placement(new Vector3(0f, 0f, 9f), MathF.PI)));
        if (nael != null) spawned.Add(nael);
    }

    private void HeavensfallHit()
    {
        helpers.Play(ActionId.Heavensfall, Vector3.Zero);
        foreach (var member in AliveMembers().ToList())
        {
            damage.ApplyDamage(member, 0.3f, ActionId.Heavensfall, "Heavensfall", lethal: false);
            member.Knockback(Vector3.Zero, HeavensfallKnockback);
        }
    }

    private readonly List<SimEnemy> burstCasters = [];

    private void CastThermionicBursts(float offset)
    {
        burstCasters.Clear();
        for (var k = 0; k < 8; k++)
        {
            var rotation = offset + k * MathF.PI / 4f;
            if (helpers.Take(new Placement(Vector3.Zero, rotation)) is not { } helper) continue;
            helper.NativeCast(ActionId.ThermionicBurst, ActionType.Action, 0f, 2.7f, false, rotation: rotation, position: Vector3.Zero);
            burstCasters.Add(helper);
        }
    }

    private void ThermionicBursts(float offset)
    {
        foreach (var helper in burstCasters)
        {
            PlayEffect(helper, ActionId.ThermionicBurst, 1.1f, helper.Rotation, at: Vector3.Zero);
            helpers.Release(helper);
        }
        burstCasters.Clear();
        foreach (var member in AliveMembers().ToList())
        {
            var at = Flat(member.Position);
            if (at.Length() > State.ThermionicBurstRange || at.Length() < 0.01f) continue;
            var bearing = MathF.Atan2(at.X, at.Y) - offset;
            var fromWedge = MathF.Abs(MathF.IEEERemainder(bearing, MathF.PI / 4f));
            if (fromWedge <= State.ThermionicBurstHalfAngle)
                member.Die(ActionId.ThermionicBurst, "stood in a Thermionic Burst slice (stand between the glowing slices)");
        }
    }

    // Anyone two Meteor Streams land on dies; one is survivable.
    private void MeteorStream(IReadOnlyList<PartyRole> targets)
    {
        var landed = targets.Select(r => party.Get(r)).Where(m => m != null && m.IsAlive()).Select(m => Flat(m!.Position)).ToList();
        foreach (var (role, at) in targets.Zip(landed))
            helpers.Play(ActionId.MeteorStream, ToWorld(at), party.Get(role)?.GameObjectId);
        foreach (var member in AliveMembers().ToList())
        {
            var hits = landed.Count(at => Vector2.Distance(Flat(member.Position), at) <= State.MeteorStreamRadius);
            if (hits >= 2) member.Die(ActionId.MeteorStream, "took two Meteor Streams (spread out for them)");
            else if (hits == 1) damage.ApplyDamage(member, 0.4f, ActionId.MeteorStream, "Meteor Stream", lethal: false);
        }
    }

    private void DalamudDiveHit()
    {
        if (nael is not { } boss || party.Get(PartyRole.MainTank) is not { } tank) return;
        boss.SetPosition(new Placement(tank.Position, boss.Rotation));
        boss.SetVisible(true);
        boss.SetTargetable(true);
        PlayEffect(boss, ActionId.DalamudDive, 1.1f, target: tank.GameObjectId, at: tank.Position);
        foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(tank.Position)) <= State.DalamudDiveRadius).ToList())
        {
            if (IsTank(hit)) damage.ApplyDamage(hit, 0.4f, ActionId.DalamudDive, "Dalamud Dive", lethal: false);
            else hit.Die(ActionId.DalamudDive, "stood near the tank Nael dove onto (stay away from Dalamud Dive)");
        }
    }

    private void BahamutsClaw() => UcobNaelAttacks.BahamutsClaw(damage, nael, party.Get(PartyRole.MainTank));

    private void SpawnDragons()
    {
        var kinds = new (uint Base, uint Name)[]
        {
            (BNpcBaseId.Firehorn, BNpcNameId.Firehorn), (BNpcBaseId.Iceclaw, BNpcNameId.Iceclaw),
            (BNpcBaseId.Thunderwing, BNpcNameId.Thunderwing), (BNpcBaseId.TailOfDarkness, BNpcNameId.TailOfDarkness),
            (BNpcBaseId.FangOfLight, BNpcNameId.FangOfLight),
        };
        var dragons = new List<SimEnemy?>();
        for (var i = 0; i < kinds.Length; i++)
        {
            var at = State.DragonPosition(state.DragonSlots[i]);
            var dragon = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: kinds[i].Base,
                NameId: kinds[i].Name,
                Level: Level,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(ToWorld(at), MathF.Atan2(-at.X, -at.Y))));
            if (dragon != null) spawned.Add(dragon);
            dragons.Add(dragon);
        }
        firehorn = dragons[0];
        iceclaw = dragons[1];
        thunderwing = dragons[2];
        tail = dragons[3];
        fang = dragons[4];
    }

    private SimEnemy? iceclaw;
    private SimEnemy? fang;

    private void MarkThunderstruck(IReadOnlyList<PartyRole> targets)
    {
        foreach (var role in targets)
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            PlayEffect(thunderwing, ActionId.ChainLightning, 1.1f, target: member.GameObjectId, at: member.Position);
            world.Events.Add(ThunderstruckAfterMark, () => { if (member.IsAlive()) member.AddStatus(StatusId.Thunderstruck, ThunderstruckSeconds); });
        }
    }

    // UNVERIFIED: the holders' splashes spare each other; everyone else in one dies.
    private void ThunderstruckSplash(IReadOnlyList<PartyRole> targets)
    {
        var holders = targets.Select(r => party.Get(r)).Where(m => m != null && m.IsAlive()).Select(m => m!).ToList();
        foreach (var holder in holders)
        {
            holder.RemoveStatus(StatusId.Thunderstruck);
            helpers.Play(ActionId.ChainLightningSplash, holder.Position, holder.GameObjectId);
            damage.ApplyDamage(holder, 0.3f, ActionId.ChainLightningSplash, "Thunderstruck", lethal: false);
            foreach (var hit in AliveMembers().Where(m => !holders.Contains(m) && Vector2.Distance(Flat(m.Position), Flat(holder.Position)) <= State.ThunderstruckRadius).ToList())
                hit.Die(ActionId.ChainLightningSplash, "caught in a Thunderstruck splash (Thunderstruck players move away from everyone)");
        }
    }

    private void NaelMoveHit(NaelMove move, int beam)
    {
        var target = move switch
        {
            NaelMove.ThermionicBeam => party.Get(state.BeamTargets[beam]) is { } chosen && chosen.IsAlive() ? chosen : AliveMembers().FirstOrDefault(),
            NaelMove.RavenDive => party.Get(state.RavenDiveTarget),
            _ => null,
        };
        UcobNaelAttacks.Move(party, damage, nael, move, target);
    }

    private void Iceball(PartyRole role)
    {
        if (party.Get(role) is not { } target || !target.IsAlive()) return;
        PlayEffect(iceclaw, ActionId.Iceball, 1.1f, target: target.GameObjectId, at: target.Position);
        if (target.HasStatus(StatusId.Firescorched)) target.RemoveStatus(StatusId.Firescorched);
        else if (target.HasStatus(StatusId.Icebitten)) target.Die(ActionId.Iceball, "took an Iceball while Icebitten (a shared Fireball thaws it)");
        else target.AddStatus(StatusId.Icebitten, 60f);
    }

    private SimTether? TetherFire(PartyRole role)
        => party.Get(role) is { } target && target.IsAlive() && firehorn != null ? world.Tether(firehorn, target, TetherId.Fireball, FireballAfterTether) : null;

    private void DragonFireball(PartyRole role)
    {
        if (party.Get(role) is not { } target || !target.IsAlive()) return;
        PlayEffect(firehorn, ActionId.DragonFireball, 1.1f, target: target.GameObjectId, at: target.Position);
        foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(target.Position)) <= State.DragonFireballRadius).ToList())
        {
            if (hit.HasStatus(StatusId.Firescorched)) hit.Die(ActionId.DragonFireball, "took a Fireball while Firescorched (an Iceball cools it first)");
            else if (hit.HasStatus(StatusId.Icebitten)) hit.RemoveStatus(StatusId.Icebitten);
            else hit.AddStatus(StatusId.Firescorched, 60f);
        }
    }

    private Vector2? CastWings(PartyRole role)
    {
        var target = party.Get(role) is { } chosen && chosen.IsAlive() ? chosen : AliveMembers().FirstOrDefault();
        if (target == null) return null;
        var at = Flat(target.Position);
        if (helpers.Take(new Placement(ToWorld(at), 0f)) is { } helper)
        {
            helper.NativeCast(ActionId.WingsOfSalvation, ActionType.Action, 0f, 2.7f, false, position: ToWorld(at));
            helpers.Release(helper, WingsCastSeconds);
        }
        return at;
    }

    private void Wings(Vector2 at)
    {
        PlayEffect(fang, ActionId.WingsOfSalvation, 1.1f, at: ToWorld(at));
        foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), at) <= State.WingsRadius).ToList())
            hit.Die(ActionId.WingsOfSalvation, "stood in a Wings of Salvation puddle as it landed");
        world.Events.Add(CleanseAppears, () =>
        {
            var eobj = SpawnEObj(EObjId.WingsOfSalvationCleanse, at);
            cleanses.Add((at, eobj, world.Events.Elapsed + CleanseLasts));
            world.Events.Add(CleanseLasts, () => eobj?.Despawn());
        });
    }

    private void Deathstorm(IReadOnlyList<PartyRole> targets)
    {
        foreach (var role in targets)
            if (party.Get(role) is { } member && member.IsAlive())
                PlayEffect(tail, ActionId.Deathstorm, 1.1f, target: member.GameObjectId, at: member.Position);
    }

    private void Doom(IReadOnlyList<PartyRole> targets, float[] seconds)
    {
        for (var i = 0; i < targets.Count; i++)
        {
            if (party.Get(targets[i]) is not { } member || !member.IsAlive()) continue;
            var duration = seconds[i];
            member.AddStatus(StatusId.Doom, duration);
            world.Events.Add(duration - DoomCheckLead, () =>
            {
                if (member.IsAlive() && member.HasStatus(StatusId.Doom))
                    member.Die(ActionId.Deathstorm, "Doom ran out (walk into a white circle Wings of Salvation leaves)");
            });
        }
    }

    private void CleanseDooms(float now)
    {
        cleanses.RemoveAll(c => now >= c.Until);
        foreach (var member in AliveMembers().Where(m => m.HasStatus(StatusId.Doom)).ToList())
        {
            var index = cleanses.FindIndex(c => Vector2.Distance(Flat(member.Position), c.At) <= State.CleanseRadius);
            if (index < 0) continue;
            member.RemoveStatus(StatusId.Doom);
            cleanses[index].EObj?.Despawn();
            cleanses.RemoveAt(index);
        }
    }

    private IEnumerable<SimEnemy> Dragons() => new[] { firehorn, iceclaw, thunderwing, tail, fang }.OfType<SimEnemy>();

    private void PlayLeaveAnimations()
    {
        nael?.PlayActionTimeline(TimelineId.NaelLeaves);
        foreach (var dragon in Dragons()) dragon.PlayActionTimeline(TimelineId.DragonLeaves);
    }

    private void DespawnDragons()
    {
        foreach (var dragon in Dragons().ToList())
        {
            dragon.Despawn();
            spawned.Remove(dragon);
        }
    }

    private void DespawnAll()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        cleanses.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        helpers.Clear();
    }

    private SimEventObject? SpawnEObj(uint eobjId, Vector2 at)
    {
        var eobj = UcobSim.SpawnEObj(world, eobjId, at);
        if (eobj != null) eobjs.Add(eobj);
        return eobj;
    }

    private IEnumerable<SimCharacter> AliveMembers() => UcobSim.AliveMembers(party);
}
