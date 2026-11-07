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
using State = AsuMech.Scenarios.Ucob.P2Nael.UcobP2NaelState;

namespace AsuMech.Scenarios.Ucob.P2Nael;

// Nael deus Darnus from Heavensfall to the Ravensbeak that ends the dragons' elements. Pushed, she
// leaves with her dragons, as a clear does; not pushed, the divebombs follow. Her HP is not
// simulated. The main tank holds her throughout.
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
    private const float ClawInterval = 0.805f;
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
    private const float RavensbeakCastSeconds = 3.7f;
    public const float NaelUntargetable = 136.41f;
    public const float LeaveAnimations = 136.50f;
    public const float DragonsLeave = 138.59f;
    public const float End = 139f;

    public const float CauterizeCastSeconds = 4f;
    public const float HypernovaPuddleAppears = 1.08f;
    public const float HypernovaPuddleLasts = 11.7f;

    // No log reaches the divebombs: every time below is UNVERIFIED.
    public const float DivebombQuoteAt = 136f;
    public static readonly float[] DivebombMarkers = [137f, 143f, 149f];
    public static readonly float[] DivebombLocks = [143f, 149f, 155f];
    public static readonly float[] Hypernovas = [143f, 146f, 149f, 152f];
    public const float NaelLeavesForTheDive = 153.5f;
    private const float NaelGone = 155.5f;
    public static readonly float[] DivebombQuoteMoves = [161f, 165f];
    public const float FinalRavensbeakCast = 167f;
    public const float DivebombsEnd = 172f;

    // Checked just before the status itself runs out and is gone.
    private const float DoomCheckLead = 0.05f;
    private const float EffectCarrierLinger = 3f;
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
    private readonly Queue<SimEnemy> helperPool = [];
    private readonly List<SimEventObject> eobjs = [];
    private readonly List<(Vector2 At, SimEventObject? EObj, float Until)> cleanses = [];
    private readonly List<(Vector2 At, float Until)> hypernovaPuddles = [];
    private readonly List<SimEnemy?> dragons = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new State(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        helperPool.Clear();
        eobjs.Clear();
        cleanses.Clear();
        hypernovaPuddles.Clear();
        dragons.Clear();
        nael = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnNael);
        world.Events.Add(0f, FillHelperPool);

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
            for (var hit = 0; hit < 5; hit++)
                world.Events.Add(start + hit * ClawInterval, BahamutsClaw);
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
            foreach (var (move, at) in new[] { quote.First, quote.Second }.Zip(QuoteMoves[i]))
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

        world.Events.Add(RavensbeakCast, CastRavensbeak);
        world.Events.Add(RavensbeakCast + RavensbeakCastSeconds, Ravensbeak);
        if (state.Push == NaelPush.NoPush)
        {
            ScheduleDivebombs();
            return;
        }
        world.Events.Add(NaelUntargetable, () => nael?.SetTargetable(false));
        world.Events.Add(LeaveAnimations, PlayLeaveAnimations);
        world.Events.Add(DragonsLeave, DespawnDragons);
        world.Events.Add(End, DespawnAll);
    }

    private void ScheduleDivebombs()
    {
        world.Events.Add(DivebombQuoteAt, () => world.Announce($"Nael deus Darnus: {state.DivebombQuote.Text}"));
        for (var i = 0; i < State.Divebombs; i++)
        {
            var target = state.DivebombTargets[i];
            var slots = state.DiveDragons[i];
            List<(int Slot, Vector2 From, Vector2 Along)> aims = [];
            world.Events.Add(DivebombMarkers[i], () => { if (party.Get(target) is { } member && member.IsAlive()) member.AttachLockonVfx(LockonId.Divebomb); });
            world.Events.Add(DivebombLocks[i], () => aims = LockCauterize(slots, target));
            world.Events.Add(DivebombLocks[i] + CauterizeCastSeconds, () => Cauterize(aims));
        }
        foreach (var at in Hypernovas)
            world.Events.Add(at, Hypernova);
        world.Events.Add(NaelLeavesForTheDive, () =>
        {
            nael?.SetTargetable(false);
            nael?.PlayActionTimeline(TimelineId.NaelLeaves);
        });
        world.Events.Add(NaelGone, () => nael?.SetVisible(false));
        var quote = state.DivebombQuote;
        world.Events.Add(DivebombQuoteMoves[0], () => DivebombQuoteMove(quote.First));
        world.Events.Add(DivebombQuoteMoves[1], () => DivebombQuoteMove(quote.Second));
        world.Events.Add(FinalRavensbeakCast, CastRavensbeak);
        world.Events.Add(FinalRavensbeakCast + RavensbeakCastSeconds, Ravensbeak);
        world.Events.Add(DivebombsEnd, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        if (nael is { IsCasting: false, IsMoving: false } boss && party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive())
            boss.Face(tank);
        CleanseDooms(world.Events.Elapsed);
        BurnInHypernovaPuddles(world.Events.Elapsed);
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
        PlayFromHelper(ActionId.Heavensfall, Vector3.Zero);
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
            if (TakeHelper(new Placement(Vector3.Zero, rotation)) is not { } helper) continue;
            helper.NativeCast(ActionId.ThermionicBurst, ActionType.Action, 0f, 2.7f, false, rotation: rotation, position: Vector3.Zero);
            burstCasters.Add(helper);
        }
    }

    private void ThermionicBursts(float offset)
    {
        foreach (var helper in burstCasters)
        {
            PlayEffect(helper, ActionId.ThermionicBurst, 1.1f, helper.Rotation, at: Vector3.Zero);
            var used = helper;
            world.Events.Add(EffectCarrierLinger, () => helperPool.Enqueue(used));
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
            PlayFromHelper(ActionId.MeteorStream, ToWorld(at), party.Get(role)?.GameObjectId);
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

    private void BahamutsClaw()
    {
        if (party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(nael, ActionId.BahamutsClaw, 0.6f, target: tank.GameObjectId);
        damage.ApplyDamage(tank, 0.08f, ActionId.BahamutsClaw, "Bahamut's Claw", lethal: false);
    }

    private void SpawnDragons()
    {
        var kinds = new (uint Base, uint Name)[]
        {
            (BNpcBaseId.Firehorn, BNpcNameId.Firehorn), (BNpcBaseId.Iceclaw, BNpcNameId.Iceclaw),
            (BNpcBaseId.Thunderwing, BNpcNameId.Thunderwing), (BNpcBaseId.TailOfDarkness, BNpcNameId.TailOfDarkness),
            (BNpcBaseId.FangOfLight, BNpcNameId.FangOfLight),
        };
        dragons.Clear();
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
            PlayFromHelper(ActionId.ChainLightningSplash, holder.Position, holder.GameObjectId);
            damage.ApplyDamage(holder, 0.3f, ActionId.ChainLightningSplash, "Thunderstruck", lethal: false);
            foreach (var hit in AliveMembers().Where(m => !holders.Contains(m) && Vector2.Distance(Flat(m.Position), Flat(holder.Position)) <= State.ThunderstruckRadius).ToList())
                hit.Die(ActionId.ChainLightningSplash, "caught in a Thunderstruck splash (Thunderstruck players move away from everyone)");
        }
    }

    private void NaelMoveHit(NaelMove move, int beam)
    {
        if (nael is not { } boss) return;
        switch (move)
        {
            case NaelMove.LunarDynamo:
                PlayEffect(boss, ActionId.LunarDynamo, 1.1f);
                foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(boss.Position)) > State.LunarDynamoInner).ToList())
                    hit.Die(ActionId.LunarDynamo, "outside Nael's melee range for Lunar Dynamo (\"hallowed moon\": get in)");
                break;
            case NaelMove.IronChariot:
                PlayEffect(boss, ActionId.IronChariot, 1.1f);
                foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(boss.Position)) <= State.IronChariotRadius).ToList())
                    hit.Die(ActionId.IronChariot, "inside Iron Chariot (\"iron path\": get out)");
                break;
            case NaelMove.ThermionicBeam:
                ThermionicBeam(state.BeamTargets[beam]);
                break;
            case NaelMove.RavenDive:
                RavenDive();
                break;
        }
    }

    private void ThermionicBeam(PartyRole role)
    {
        var target = party.Get(role) is { } chosen && chosen.IsAlive() ? chosen : AliveMembers().FirstOrDefault();
        if (target == null) return;
        PlayEffect(nael, ActionId.ThermionicBeam, 1.1f, target: target.GameObjectId, at: target.Position);
        var hits = AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(target.Position)) <= State.ThermionicBeamRadius).ToList();
        foreach (var hit in hits)
        {
            if (hits.Count < State.ThermionicBeamMinimumStack) hit.Die(ActionId.ThermionicBeam, "Thermionic Beam wasn't shared (\"take fire\" / \"blazing path\": stack)");
            else damage.ApplyDamage(hit, 0.5f, ActionId.ThermionicBeam, "Thermionic Beam", lethal: false);
        }
    }

    // UNVERIFIED: whoever else it lands on is knocked back like Heavensfall.
    private void RavenDive()
    {
        if (nael is not { } boss || party.Get(state.RavenDiveTarget) is not { } target || !target.IsAlive()) return;
        boss.SetPosition(new Placement(target.Position, boss.Rotation));
        PlayEffect(boss, ActionId.RavenDive, 1.1f, target: target.GameObjectId, at: target.Position);
        damage.ApplyDamage(target, 0.2f, ActionId.RavenDive, "Raven Dive", lethal: false);
        foreach (var hit in AliveMembers().Where(m => !ReferenceEquals(m, target) && Vector2.Distance(Flat(m.Position), Flat(target.Position)) <= State.RavenDiveRadius).ToList())
            hit.Knockback(target.Position, HeavensfallKnockback);
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
        if (TakeHelper(new Placement(ToWorld(at), 0f)) is { } helper)
        {
            helper.NativeCast(ActionId.WingsOfSalvation, ActionType.Action, 0f, 2.7f, false, position: ToWorld(at));
            world.Events.Add(WingsCastSeconds + EffectCarrierLinger, () => helperPool.Enqueue(helper));
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

    private void CastRavensbeak()
    {
        if (party.Get(PartyRole.MainTank) is not { } tank) return;
        nael?.NativeCast(ActionId.Ravensbeak, ActionType.Action, 0f, RavensbeakCastSeconds, false, targetId: tank.GameObjectId);
    }

    private void Ravensbeak()
    {
        if (party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(nael, ActionId.Ravensbeak, 1.1f, target: tank.GameObjectId);
        damage.ApplyDamage(tank, 0.5f, ActionId.Ravensbeak, "Ravensbeak", lethal: false);
    }

    private static readonly uint[] CauterizeIds =
    [
        ActionId.CauterizeFirehorn, ActionId.CauterizeIceclaw, ActionId.CauterizeThunderwing,
        ActionId.CauterizeTailOfDarkness, ActionId.CauterizeFangOfLight,
    ];

    // UNVERIFIED: each dragon locks its line on the marked player as the marker goes, then dives
    // along it and is gone.
    private List<(int Slot, Vector2 From, Vector2 Along)> LockCauterize(IReadOnlyList<int> slots, PartyRole role)
    {
        var target = party.Get(role) is { } chosen && chosen.IsAlive() ? chosen : AliveMembers().FirstOrDefault();
        var aims = new List<(int Slot, Vector2 From, Vector2 Along)>();
        if (target == null) return aims;
        foreach (var slot in slots)
        {
            var index = state.DragonSlots.ToList().IndexOf(slot);
            var from = State.DragonPosition(slot);
            var toward = Flat(target.Position) - from;
            var along = toward.LengthSquared() < 0.01f ? Vector2.Normalize(-from) : Vector2.Normalize(toward);
            var rotation = MathF.Atan2(along.X, along.Y);
            if (dragons[index] is { } dragon)
            {
                dragon.SetPosition(new Placement(ToWorld(from), rotation));
                dragon.NativeCast(CauterizeIds[index], ActionType.Action, 0f, CauterizeCastSeconds, false, rotation: rotation, position: ToWorld(from));
            }
            aims.Add((slot, from, along));
        }
        return aims;
    }

    private void Cauterize(IReadOnlyList<(int Slot, Vector2 From, Vector2 Along)> aims)
    {
        foreach (var (slot, from, along) in aims)
        {
            var index = state.DragonSlots.ToList().IndexOf(slot);
            var dragon = dragons[index];
            PlayEffect(dragon, CauterizeIds[index], 1.1f, rotation: MathF.Atan2(along.X, along.Y), at: ToWorld(from));
            foreach (var hit in AliveMembers().Where(m => InCauterize(from, along, Flat(m.Position))).ToList())
                hit.Die(CauterizeIds[index], "stood in a dragon's divebomb path (step out of its line once it locks on)");
            if (dragon == null) continue;
            world.Events.Add(EffectCarrierLinger, () =>
            {
                dragon.Despawn();
                spawned.Remove(dragon);
            });
            dragons[index] = null;
        }
    }

    public static bool InCauterize(Vector2 from, Vector2 along, Vector2 at)
    {
        var offset = at - from;
        var ahead = Vector2.Dot(offset, along);
        var aside = MathF.Abs(along.X * offset.Y - along.Y * offset.X);
        return ahead >= 0f && ahead <= State.CauterizeLength && aside <= State.CauterizeHalfWidth;
    }

    // UNVERIFIED: each lands on whoever stands farthest from the centre.
    private void Hypernova()
    {
        if (AliveMembers().OrderByDescending(m => Flat(m.Position).Length()).FirstOrDefault() is not { } target) return;
        var at = Flat(target.Position);
        PlayEffect(nael, ActionId.Hypernova, 1.1f, at: ToWorld(at));
        foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), at) <= State.HypernovaRadius).ToList())
            damage.ApplyDamage(hit, 0.3f, ActionId.Hypernova, "Hypernova", lethal: false);
        world.Events.Add(HypernovaPuddleAppears, () =>
        {
            var eobj = SpawnEObj(EObjId.HypernovaPuddle, at);
            var now = world.Events.Elapsed;
            hypernovaPuddles.Add((at, now + HypernovaPuddleLasts));
            world.Events.Add(HypernovaPuddleLasts, () => eobj?.Despawn());
        });
    }

    // UNVERIFIED: standing in a puddle once it has formed is lethal.
    private void BurnInHypernovaPuddles(float now)
    {
        hypernovaPuddles.RemoveAll(p => now >= p.Until);
        foreach (var member in AliveMembers().ToList())
            if (hypernovaPuddles.Any(p => Vector2.Distance(Flat(member.Position), p.At) <= State.HypernovaPuddleRadius))
                member.Die(ActionId.Hypernova, "stood in a Hypernova puddle (keep moving once one lands on you)");
    }

    private void DivebombQuoteMove(NaelMove move)
    {
        switch (move)
        {
            case NaelMove.MeteorStream:
                MeteorStream(state.DivebombSpreads);
                break;
            case NaelMove.DalamudDive:
                DalamudDiveHit();
                break;
            case NaelMove.ThermionicBeam:
                ThermionicBeam(state.DivebombStackTarget);
                break;
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
        hypernovaPuddles.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        helperPool.Clear();
    }

    private SimEventObject? SpawnEObj(uint eobjId, Vector2 at)
    {
        var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = eobjId,
            Placement = new Placement(ToWorld(at), 0f),
            TimelineState = 1,
        });
        if (eobj != null) eobjs.Add(eobj);
        return eobj;
    }

    // A helper spawned in the frame it acts drops the effect or cast, so they wait in a pool.
    private void FillHelperPool()
    {
        for (var i = 0; i < HelperPoolSize; i++)
            if (SpawnHelper(new Placement(Vector3.Zero, 0f)) is { } helper) helperPool.Enqueue(helper);
    }

    private SimEnemy? TakeHelper(Placement placement)
    {
        var helper = helperPool.TryDequeue(out var pooled) ? pooled : SpawnHelper(placement);
        helper?.SetPosition(placement);
        return helper;
    }

    private SimEnemy? SpawnHelper(Placement placement)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: BNpcNameId.NaelDeusDarnus,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SimEnemy.LegacyVisibility(true, true),
            Placement: placement));
        if (helper != null) spawned.Add(helper);
        return helper;
    }

    private void PlayFromHelper(uint actionId, Vector3 at, GameObjectId? target = null)
    {
        if (TakeHelper(new Placement(at, 0f)) is not { } helper) return;
        PlayEffect(helper, actionId, 1.1f, target: target, at: at);
        world.Events.Add(EffectCarrierLinger, () => helperPool.Enqueue(helper));
    }

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static bool IsTank(SimCharacter member) => member is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank };

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);
}
