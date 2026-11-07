using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using State = AsuMech.Scenarios.Ucob.P4Adds.UcobP4AddsState;

namespace AsuMech.Scenarios.Ucob.P4Adds;

// Twintania and Nael return beside Bahamut Prime until both die. Their HP is not simulated: they
// die when a clear killed them, before their second Death Sentence and Megaflare. Each chases
// whichever tank holds her; the tanks trade them on a fixed clock after the busters.
// UNVERIFIED: Nael stands still from a quote until its third move.
public sealed class UcobP4AddsScenario : IScenario
{
    public string Name => "Adds";
    public IPhase Phase => UcobZone.P4;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP4AddsAi()];

    private readonly UcobP4AddsSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public static readonly Vector2 BahamutSpot = new(0f, -24f);
    public static readonly Vector2 TwintaniaSpawn = new(-5.5f, -11f);
    public static readonly Vector2 NaelSpawn = new(5.5f, -11f);

    public const float AddsAppear = 2.47f;
    private const float ReturnAnimations = 2.93f;
    private const float FavorTethers = 4.85f;
    private const float OpeningQuote = 4.94f;
    private const float BahamutsFavor = 9.48f;
    public const float Targetable = 10.91f;
    public static readonly float[] PlummetHits = [19.06f, 75.77f];
    public static readonly float[] ClawStarts = [19.06f, 75.81f];
    public static readonly float[][] LiquidHellDrops =
    [
        [22.36f, 23.52f, 24.68f, 25.84f, 27.00f],
        [84.91f, 86.11f, 87.27f, 88.47f, 89.63f],
    ];
    public static readonly float[] GenerateCasts = [28.20f, 90.84f];
    public static readonly float[] TwisterCasts = [33.37f, 50.49f, 96.01f, 113.14f];
    public static readonly float[] TwisterSnapshots = [35.33f, 52.45f, 97.97f, 115.10f];
    public static readonly float[] Quotes = [36.94f, 99.94f];
    public static readonly float[][] QuoteMoves = [[43.17f, 46.25f, 49.42f], [105.91f, 109.04f, 112.29f]];
    public const float MegaflareCast = 58.56f;
    private const float MegaflareCastSeconds = 4.7f;
    public const float MegaflareHit = 63.55f;
    public const float DeathSentenceCast = 67.65f;
    public const float DeathSentenceHit = 71.62f;
    public const float RavensbeakCast = 67.70f;
    public const float RavensbeakHit = 71.66f;
    public const float AggroSwap = 72.40f;
    // UNVERIFIED what it does: the kill interrupts it.
    private const float WhiteFuryCast = 120.39f;
    private const float WhiteFuryCastSeconds = 2.7f;
    public const float TwintaniaDies = 122.31f;
    private const float WhiteFuryInterrupted = 122.71f;
    private const float ClosingQuote = 122.94f;
    public const float NaelDies = 124.76f;
    public const float End = 131f;

    private const float TwintaniaCorpseFades = 5.6f;
    private const float NaelCorpseFades = 4.5f;
    private const float NaelResumesAfterLastMove = 0.3f;
    private const int HelperPoolSize = 8;

    private static readonly (float At, float Fraction)[] TwintaniaHealth =
    [
        (Targetable, 0.40f), (14.79f, 0.391f), (25.22f, 0.347f), (36.09f, 0.274f), (52.41f, 0.200f), (68.05f, 0.158f),
        (78.62f, 0.132f), (89.45f, 0.077f), (99.58f, 0.031f), (112.92f, 0.007f), (120.26f, 0.001f), (TwintaniaDies, 0f),
    ];
    private static readonly (float At, float Fraction)[] NaelHealth =
    [
        (Targetable, 0.45f), (14.52f, 0.434f), (25.00f, 0.300f), (36.09f, 0.232f), (52.45f, 0.216f), (62.80f, 0.192f),
        (73.40f, 0.130f), (89.72f, 0.065f), (101.23f, 0.041f), (112.29f, 0.018f), (122.53f, 0.003f), (NaelDies, 0f),
    ];

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private State state = null!;
    private SimEnemy? twintania;
    private SimEnemy? nael;
    private SimEnemy? bahamut;
    private readonly List<SimEnemy> spawned = [];
    private readonly List<SimTether> tethers = [];
    private UcobHelperPool helpers = null!;
    private AddsNeurolinks neurolinks = null!;
    private AddsTwisters twisters = null!;
    private UcobLiquidHell liquidHell = null!;
    private AddsHatches hatches = null!;
    private bool naelHolding;

    public static PartyRole TwintaniaHolder(State state, float at) => at < AggroSwap ? state.TwintaniaTank : state.NaelTank;

    public static PartyRole NaelHolder(State state, float at) => at < AggroSwap ? state.NaelTank : state.TwintaniaTank;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new State(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        tethers.Clear();
        helpers = new UcobHelperPool(world, BNpcNameId.Twintania, spawned);
        neurolinks = new AddsNeurolinks(world);
        twisters = new AddsTwisters(world, helpers);
        liquidHell = new UcobLiquidHell(world, damage);
        hatches = new AddsHatches(world, damage, neurolinks, spawned);
        twintania = nael = bahamut = null;
        naelHolding = false;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, () =>
        {
            bahamut = Spawn(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, BahamutSpot, EnemyListMode.Never);
            bahamut?.PlayActionTimeline(TimelineId.BahamutPrimeLands);
        });
        world.Events.Add(0f, () => helpers.Fill(HelperPoolSize));
        world.Events.Add(0f, DropNeurolinks);
        world.Events.Add(AddsAppear, SpawnAdds);
        world.Events.Add(ReturnAnimations, PlayReturnAnimations);
        world.Events.Add(FavorTethers, TetherAddsToBahamut);
        world.Events.Add(OpeningQuote, () => world.Announce("Nael deus Darnus: O Bahamut! We shall stand guard as you make ready your divine judgment!"));
        world.Events.Add(BahamutsFavor, GrantBahamutsFavor);
        world.Events.Add(Targetable, EngageAdds);

        foreach (var at in PlummetHits)
            world.Events.Add(at, () => UcobTwintaniaAttacks.Plummet(party, damage, twintania, TwintaniaTank()));
        foreach (var start in ClawStarts)
            for (var hit = 0; hit < UcobNaelAttacks.ClawHits; hit++)
                world.Events.Add(start + hit * UcobNaelAttacks.ClawInterval, () => UcobNaelAttacks.BahamutsClaw(damage, nael, NaelTank()));
        foreach (var drops in LiquidHellDrops)
            foreach (var at in drops)
                world.Events.Add(at, () => liquidHell.Drop(twintania));
        for (var i = 0; i < GenerateCasts.Length; i++)
        {
            var targets = state.HatchTargets[i];
            world.Events.Add(GenerateCasts[i] - AddsHatches.MarkerLead, () => hatches.Generate(twintania, targets));
            world.Events.Add(GenerateCasts[i] + AddsHatches.CastSeconds + AddsHatches.ChaseAfterCast, hatches.Chase);
        }
        for (var i = 0; i < TwisterCasts.Length; i++)
        {
            var targets = state.TwisterTargets[i];
            world.Events.Add(TwisterCasts[i], () => twisters.Cast(twintania));
            world.Events.Add(TwisterSnapshots[i], () => twisters.Drop(twintania, targets));
        }
        for (var q = 0; q < Quotes.Length; q++)
        {
            var quote = state.NaelQuotes[q];
            var (dive, beam) = (state.RavenDiveTargets[q], state.BeamTargets[q]);
            world.Events.Add(Quotes[q], () => HoldNael(quote));
            foreach (var (move, at) in quote.Moves.Zip(QuoteMoves[q]))
                world.Events.Add(at, () => NaelMoveHit(move, dive, beam));
            world.Events.Add(QuoteMoves[q][^1] + NaelResumesAfterLastMove, ReleaseNael);
        }

        world.Events.Add(MegaflareCast, () => UcobSim.CastSelf(nael, ActionId.Megaflare, MegaflareCastSeconds));
        world.Events.Add(MegaflareHit, Megaflare);
        world.Events.Add(DeathSentenceCast, () => UcobTwintaniaAttacks.CastDeathSentence(twintania, TwintaniaTank()));
        world.Events.Add(RavensbeakCast, () => UcobNaelAttacks.CastRavensbeak(nael, NaelTank()));
        world.Events.Add(DeathSentenceHit, () => UcobTwintaniaAttacks.DeathSentence(damage, twintania, TwintaniaTank()));
        world.Events.Add(RavensbeakHit, () => UcobNaelAttacks.Ravensbeak(damage, nael, NaelTank()));
        world.Events.Add(AggroSwap, ChaseHolders);

        world.Events.Add(WhiteFuryCast, () => nael?.Cast(ActionId.WhiteFury));
        world.Events.Add(TwintaniaDies, () => Kill(twintania, TwintaniaCorpseFades));
        world.Events.Add(WhiteFuryInterrupted, () => nael?.CancelCast(CastCancelReason.Interrupted));
        world.Events.Add(ClosingQuote, () => world.Announce("Nael deus Darnus: Ugh... None shall defy Lord Bahamut's will! On your knees, vermin!"));
        world.Events.Add(NaelDies, () => Kill(nael, NaelCorpseFades));
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        var now = world.Events.Elapsed;
        if (twintania is { Dead: false } twin) twin.SetHealth(0, UcobSim.Interpolate(TwintaniaHealth, now));
        if (nael is { Dead: false } boss) boss.SetHealth(0, UcobSim.Interpolate(NaelHealth, now));
        neurolinks.Tick();
        twisters.Tick(now);
        liquidHell.Tick(now);
        hatches.Tick();
    }

    private SimCharacter? TwintaniaTank() => party.Get(TwintaniaHolder(state, world.Events.Elapsed));

    private SimCharacter? NaelTank() => party.Get(NaelHolder(state, world.Events.Elapsed));

    private void SpawnAdds()
    {
        twintania = Spawn(BNpcBaseId.Twintania, BNpcNameId.Twintania, TwintaniaSpawn, EnemyListMode.Always);
        nael = Spawn(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, NaelSpawn, EnemyListMode.Always);
    }

    private SimEnemy? Spawn(uint baseId, uint nameId, Vector2 at, EnemyListMode enemyList)
    {
        var enemy = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: enemyList,
            Placement: new Placement(UcobSim.ToWorld(at), 0f)));
        if (enemy != null) spawned.Add(enemy);
        return enemy;
    }

    private void DropNeurolinks()
    {
        neurolinks.Drop(AddsNeurolinks.North);
        neurolinks.Drop(AddsNeurolinks.West);
        neurolinks.Drop(AddsNeurolinks.East);
    }

    private void PlayReturnAnimations()
    {
        twintania?.PlayActionTimeline(TimelineId.TwintaniaReturns);
        nael?.PlayActionTimeline(TimelineId.NaelReturns);
    }

    private void TetherAddsToBahamut()
    {
        foreach (var add in new[] { twintania, nael }.OfType<SimEnemy>())
            tethers.Add(world.Tether(add, bahamut, TetherId.BahamutsFavor));
    }

    private void GrantBahamutsFavor()
    {
        foreach (var add in new[] { twintania, nael }.OfType<SimEnemy>())
        {
            UcobSim.PlayEffect(bahamut, ActionId.PrimeBahamutsFavor, 1.1f, target: add.GameObjectId, at: add.Position);
            add.AddStatus(StatusId.DamageUp);
        }
    }

    private void EngageAdds()
    {
        twintania?.SetTargetable(true);
        nael?.SetTargetable(true);
        ChaseHolders();
    }

    private void ChaseHolders()
    {
        twintania?.SetTarget(TwintaniaTank());
        if (naelHolding) nael?.SetTarget(NaelTank(), follow: false);
        else nael?.SetTarget(NaelTank());
    }

    private void HoldNael(NaelTripleQuote quote)
    {
        world.Announce($"Nael deus Darnus: {quote.Text}");
        naelHolding = true;
        nael?.Follow(null);
        nael?.StopMoving();
    }

    private void ReleaseNael()
    {
        naelHolding = false;
        nael?.SetTarget(NaelTank());
    }

    private void NaelMoveHit(NaelMove move, PartyRole dive, PartyRole beam)
    {
        var target = move switch
        {
            NaelMove.ThermionicBeam => party.Get(beam) is { } chosen && chosen.IsAlive() ? chosen : UcobSim.AliveMembers(party).FirstOrDefault(),
            NaelMove.RavenDive => party.Get(dive),
            _ => null,
        };
        UcobNaelAttacks.Move(party, damage, nael, move, target);
    }

    private void Megaflare()
    {
        UcobSim.PlayEffect(nael, ActionId.Megaflare, 1.1f);
        foreach (var member in UcobSim.AliveMembers(party).ToList())
            damage.ApplyDamage(member, 0.4f, ActionId.Megaflare, "Megaflare", lethal: false);
    }

    private void Kill(SimEnemy? add, float corpseFades)
    {
        if (add is null) return;
        foreach (var tether in tethers.Where(t => ReferenceEquals(t.A, add)).ToList())
        {
            tether.Despawn();
            tethers.Remove(tether);
        }
        add.SetTargetable(false);
        add.Defeat(corpseFades);
        if (ReferenceEquals(add, twintania)) hatches.Clear();
    }

    private void DespawnAll()
    {
        foreach (var tether in tethers) tether.Despawn();
        tethers.Clear();
        neurolinks.Clear();
        twisters.Clear();
        liquidHell.Clear();
        hatches.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        helpers.Clear();
    }
}
