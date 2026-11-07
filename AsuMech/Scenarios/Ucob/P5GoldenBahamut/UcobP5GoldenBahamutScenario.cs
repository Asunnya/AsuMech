using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Actions = AsuMech.Scenarios.Ucob.UcobActions;
using State = AsuMech.Scenarios.Ucob.P5GoldenBahamut.UcobP5GoldenBahamutState;

namespace AsuMech.Scenarios.Ucob.P5GoldenBahamut;

// Golden Bahamut from his landing to the clear: Morn Afah stacks, Akh Morn tank shares,
// four Exaflare sets, and the enrage Morn Afah he dies casting. The Teraflare that wipes the party
// before the revive is skipped, and his HP is not simulated: he stays in the middle facing the
// tank who holds him, and falls when the clear did.
public sealed class UcobP5GoldenBahamutScenario : IScenario
{
    public string Name => "Golden Bahamut";
    public IPhase Phase => UcobZone.P5;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP5GoldenBahamutAi()];

    private readonly UcobP5GoldenBahamutSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public const float LohsDaih = 27.31f;
    public const float InnerRage = 30.95f;
    public const float InnerRageAnimation = 31.85f;
    public const float Targetable = 34.92f;
    // The Phoenix transition before this is its own scenario.
    public const float StartsAt = 26.3f;
    public static readonly float[] MornAfahCasts = [35.14f, 90.47f, 123.91f, 173.76f, 224.64f];
    public static readonly float[] AkhMornCasts = [43.43f, 75.03f, 138.04f, 187.89f];
    public static readonly int[] AkhMornFollowUps = [2, 3, 4, 5];
    public static readonly float[] ExaflareCasts = [55.91f, 104.58f, 154.45f, 205.35f];
    public const float MornAfahEnrage = 232.76f;
    public const float Clear = 233.62f;

    public const float MornAfahResolve = 6f;
    public const float AkhMornResolve = 4f;
    private const float AkhMornFollowUpDelay = 6.06f;
    private const float AkhMornFollowUpInterval = 1.07f;
    public const float HolderSwapAfterAkhMorn = 4.12f;
    // His flight in covers the 24y to the middle in under a second.
    private SimWorld world = null!;
    private State state = null!;
    private SimEnemy? bahamut;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new State(world.Rng, settingsWindow.Overrides);
        bahamut = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(StartsAt, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(StartsAt, () => bahamut = P5PhoenixTransition.UcobP5PhoenixTransitionScenario.SpawnBahamut(world, Vector3.Zero));
        world.Events.Add(LohsDaih, () => bahamut?.AddStatusParam(StatusId.LohsDaih, LohsDaihTransformation));
        world.Events.Add(InnerRage, () => world.Announce("Bahamut Prime unleashes his inner rage!"));
        world.Events.Add(InnerRageAnimation, () => bahamut?.PlayActionTimeline(TimelineId.BahamutInnerRage));
        world.Events.Add(Targetable, () => bahamut?.SetTargetable(true));
        world.Events.Add(Targetable, () => FaceTheHolder(PartyRole.MainTank));

        for (var i = 0; i < MornAfahCasts.Length; i++)
        {
            var target = state.MornAfahTargets[i];
            world.Events.Add(MornAfahCasts[i], () => bahamut?.Cast(Actions.MornAfah, MarkedOrAnyoneAlive(target)));
        }

        var targets = state.AkhMornTargets;
        for (var k = 0; k < AkhMornCasts.Length; k++)
        {
            var tank = targets[k];
            if (k > 0 && tank != targets[k - 1])
                world.Events.Add(AkhMornLastHit(k - 1) + HolderSwapAfterAkhMorn, () => FaceTheHolder(tank));
            world.Events.Add(AkhMornCasts[k], () => AkhMorn(Actions.AkhMorn, tank));
            for (var hit = 0; hit < AkhMornFollowUps[k]; hit++)
                world.Events.Add(AkhMornFollowUpAt(k, hit), () => AkhMorn(Actions.AkhMornFollowUp, tank));
        }

        foreach (var exaflares in state.Exaflares) exaflares.Schedule(world, () => bahamut);

        world.Events.Add(MornAfahEnrage, () => bahamut?.Cast(Actions.MornAfahEnrage, world.Party.Get(targets[^1])));
        world.Events.Add(Clear, () => bahamut?.Defeat());
        // Events.Add counts from the clock, so the window goes last.
        world.Events.KeepWindow(StartsAt, float.MaxValue);
    }

    public static float AkhMornFollowUpAt(int akhMorn, int hit) => AkhMornCasts[akhMorn] + AkhMornFollowUpDelay + hit * AkhMornFollowUpInterval;

    public static float AkhMornLastHit(int akhMorn) => AkhMornFollowUpAt(akhMorn, AkhMornFollowUps[akhMorn] - 1);

    private void FaceTheHolder(PartyRole tank) => bahamut?.SetTarget(world.Party.Get(tank), follow: false);

    private SimCharacter? MarkedOrAnyoneAlive(PartyRole role)
        => world.Party.Get(role) is { } marked && marked.IsAlive()
            ? marked
            : Enumerable.Range(0, 8).Select(i => world.Party.Get(i)).FirstOrDefault(m => m != null && m.IsAlive());

    private void AkhMorn(EnemyAction action, PartyRole tank)
    {
        if (world.Party.Get(tank) is { } target && target.IsAlive()) bahamut?.Cast(action, target);
    }
}
