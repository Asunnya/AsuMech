using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;
using State = AsuMech.Scenarios.Ucob.P3Fellruin.UcobP3FellruinState;

namespace AsuMech.Scenarios.Ucob.P3Fellruin;

// Bahamut Prime's Fellruin Trio, through the Flatten that follows it. Twintania's P1 neurolinks are
// still down; the bosses' HP is not simulated.
public sealed class UcobP3FellruinScenario : IScenario
{
    public string Name => "Fellruin Trio";
    public IPhase Phase => UcobZone.P3;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP3FellruinAi()];

    private readonly UcobP3FellruinSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public const float FellruinTrio = 4.00f;
    private const float NaelAndTwintaniaShow = 4.05f;
    private const float BahamutUntargetable = 10.02f;
    private const float TrioLeave = 10.06f;
    private const float TrioMove = 10.19f;
    private const float TrioLand = 11.18f;
    public const float Quote = 11.31f;
    public const float TempestWingTethers = 17.33f;
    public static readonly float[] QuoteMoves = [17.42f, 20.58f];
    public const float AethericProfusion = 19.38f;
    public const float AethericProfusionResolves = 24.38f;
    public const float TempestWing = 23.43f;
    private const float NaelLeaves = 23.65f;
    public const float Gigaflare = 26.55f;
    private const float GeminiAppear = 27.68f;
    public const float MeteorStreams = 27.75f;
    private const float TwintaniaLeaves = 28.51f;
    private const float GeminiVanish = 29.14f;
    public const float Flatten = 43.03f;
    public const float End = 48f;

    private static readonly Vector2 NaelShowsAt = new(-5.86f, -3.35f);
    private static readonly Vector2 TwintaniaShowsAt = new(-4.05f, 7.08f);

    private SimWorld world = null!;
    private SimParty party = null!;
    private State state = null!;
    private SimEnemy? bahamut;
    private SimEnemy? nael;
    private SimEnemy? twintania;
    private readonly List<SimEnemy> gemini = [];
    private readonly UcobNeurolinks neurolinks = new();
    private readonly List<SimTether> tempestTethers = [];
    private bool tethersFollow;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new State(world.Rng, settingsWindow.Overrides);
        neurolinks.Clear();
        tempestTethers.Clear();
        tethersFollow = false;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnBosses);
        world.Events.Add(0f, () => { foreach (var at in Geometry.P3Neurolinks) neurolinks.Drop(world, at); });

        world.Events.Add(FellruinTrio, () => bahamut?.Cast(ActionId.FellruinTrio));
        world.Events.Add(NaelAndTwintaniaShow, ShowNaelAndTwintania);
        world.Events.Add(BahamutUntargetable, () => bahamut?.SetTargetable(false));
        world.Events.Add(TrioLeave, PlayTrioLeave);
        world.Events.Add(TrioMove, MoveTrioToTheirSpots);
        world.Events.Add(TrioLand, PlayTrioLand);
        world.Events.Add(Quote, () => UcobNaelQuotes.Say(world, nael, state.Quote.Text));
        world.Events.Add(TempestWingTethers, TetherTempestWing);
        world.Events.Add(QuoteMoves[0], () => ResolveNaelMove(state.Quote.First));
        world.Events.Add(AethericProfusion, () => twintania?.Cast(Actions.AethericProfusion));
        world.Events.Add(QuoteMoves[1], () => ResolveNaelMove(state.Quote.Second));
        world.Events.Add(TempestWing, ResolveTempestWing);
        world.Events.Add(NaelLeaves, () => nael?.PlayActionTimeline(TimelineId.TrioLeave));
        world.Events.Add(Gigaflare, () =>
        {
            bahamut?.SetTargetable(true);
            bahamut?.Cast(Actions.Gigaflare);
        });
        world.Events.Add(GeminiAppear, GeminiLandOnTheirTargets);
        world.Events.Add(MeteorStreams, GeminiStrike);
        world.Events.Add(TwintaniaLeaves, () => twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLeave));
        world.Events.Add(GeminiVanish, () => { foreach (var geminus in gemini) geminus.SetVisible(false); });
        world.Events.Add(Flatten, () => bahamut?.Cast(Actions.Flatten, party.Get(PartyRole.OffTank)));
        world.Events.Add(End, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        neurolinks.Tick(party);
        if (tethersFollow) FollowTheNearestTwo();
    }

    private void SpawnBosses()
    {
        bahamut = Spawn(BNpcBaseId.BahamutPrime, BNpcNameId.BahamutPrime, Vector2.Zero, MathF.PI, true, EnemyListMode.Always, SpawnVisibility.Visible);
        bahamut?.PlayActionTimeline(TimelineId.BahamutPrimeLands);
        nael = Spawn(BNpcBaseId.NaelDeusDarnus, BNpcNameId.NaelDeusDarnus, NaelShowsAt, 0f, false, EnemyListMode.Never, SpawnVisibility.HiddenUntilShown);
        twintania = Spawn(BNpcBaseId.Twintania, BNpcNameId.Twintania, TwintaniaShowsAt, 0f, false, EnemyListMode.Never, SpawnVisibility.HiddenUntilShown);
        bahamut?.KeepFacing(party.Get(PartyRole.MainTank));
        gemini.Clear();
        for (var i = 0; i < State.MeteorStreams; i++)
            if (Spawn(BNpcBaseId.NaelGeminus, BNpcNameId.NaelGeminus, Vector2.Zero, 0f, false, EnemyListMode.Never, SpawnVisibility.HiddenUntilShown) is { } geminus)
                gemini.Add(geminus);
    }

    private SimEnemy? Spawn(uint baseId, uint nameId, Vector2 at, float rotation, bool targetable, EnemyListMode list, SpawnVisibility visibility)
        => world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: targetable,
            EnemyList: list,
            Visibility: visibility,
            Placement: new Placement(ToWorld(at), rotation)));

    private void ShowNaelAndTwintania()
    {
        nael?.SetVisible(true);
        twintania?.SetVisible(true);
        nael?.PlayActionTimeline(TimelineId.TrioLand);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLand);
    }

    private void PlayTrioLeave()
    {
        bahamut?.PlayActionTimeline(TimelineId.TrioLeave);
        nael?.PlayActionTimeline(TimelineId.TrioLeave);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLeave);
    }

    private void MoveTrioToTheirSpots()
    {
        bahamut?.SetPosition(new Placement(ToWorld(state.Bahamut), State.FacingOf(state.Bahamut, Vector2.Zero)));
        nael?.SetPosition(new Placement(ToWorld(state.Nael), State.FacingOf(state.Nael, Vector2.Zero)));
        twintania?.SetPosition(new Placement(ToWorld(state.Twintania), State.FacingOf(state.Twintania, Vector2.Zero)));
    }

    private void PlayTrioLand()
    {
        bahamut?.PlayActionTimeline(TimelineId.TrioLand);
        nael?.PlayActionTimeline(TimelineId.TrioLand);
        twintania?.PlayActionTimeline(TimelineId.TwintaniaTrioLand);
    }

    private void ResolveNaelMove(NaelMove move)
    {
        if (move == NaelMove.LunarDynamo) nael?.Cast(Actions.LunarDynamo);
        else if (move == NaelMove.RavenDive && party.Get(state.RavenDiveTarget) is { } target && target.IsAlive())
            nael?.Cast(Actions.RavenDive, target);
    }

    // The two players nearest Bahamut Prime hold them until they go off.
    private void TetherTempestWing()
    {
        tethersFollow = true;
        FollowTheNearestTwo();
    }

    private void FollowTheNearestTwo()
    {
        if (bahamut is not { } boss) return;
        var nearest = AliveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).Take(2).ToList();
        foreach (var tether in tempestTethers.Where(t => t.B is not { } holder || !nearest.Contains(holder)).ToList())
        {
            tether.Despawn();
            tempestTethers.Remove(tether);
        }
        foreach (var member in nearest.Where(m => tempestTethers.All(t => !ReferenceEquals(t.B, m))))
            tempestTethers.Add(world.Tether(boss, member, TetherId.TempestWing));
    }

    private void ResolveTempestWing()
    {
        tethersFollow = false;
        foreach (var tether in tempestTethers)
        {
            if (tether.B is { } holder && holder.IsAlive() && bahamut is { } boss)
            {
                boss.Cast(ActionId.TempestWing, holder);
                boss.CastFromHelper(Actions.TempestWingSplash, holder, holder.Placement());
            }
            tether.Despawn();
        }
        tempestTethers.Clear();
    }

    private void GeminiLandOnTheirTargets()
    {
        foreach (var (geminus, target) in GeminiAndTargets())
        {
            geminus.SetPosition(new Placement(target.Position, State.FacingOf(Flat(target.Position), Vector2.Zero)));
            geminus.SetVisible(true);
        }
    }

    private void GeminiStrike()
    {
        foreach (var (geminus, target) in GeminiAndTargets())
            geminus.Cast(Actions.MeteorStream, target);
    }

    private IEnumerable<(SimEnemy Geminus, SimCharacter Target)> GeminiAndTargets()
    {
        for (var i = 0; i < gemini.Count && i < state.MeteorTargets.Count; i++)
            if (party.Get(state.MeteorTargets[i]) is { } target && target.IsAlive())
                yield return (gemini[i], target);
    }

    private void DespawnAll()
    {
        foreach (var tether in tempestTethers) tether.Despawn();
        tempestTethers.Clear();
        neurolinks.Clear();
        bahamut?.Despawn();
        nael?.Despawn();
        twintania?.Despawn();
        foreach (var geminus in gemini) geminus.Despawn();
        gemini.Clear();
    }

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
