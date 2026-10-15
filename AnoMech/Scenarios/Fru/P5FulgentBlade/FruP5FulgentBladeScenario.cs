using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P5FulgentBlade.FruP5FulgentBladeState;
using Actions = AnoMech.Scenarios.Fru.FruActions;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// Pandora's exalines: three trains of four strips, each telegraphed by its first cast's omen and
// then rolling on as instant hits from the same helper. Akh Morn closes it on one member of
// each light party.
public sealed class FruP5FulgentBladeScenario : IScenario
{
    public string Name => "Fulgent Blade";
    public IPhase Phase => FruZone.P5;
    public bool SupportsSolo => true;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5FulgentBladeAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP5FulgentBladeSettingsWindow settingsWindow = new();

    private SimWorld world = null!;
    private FruP5FulgentBladeState state = null!;
    private SimEnemy? pandora;
    private readonly SimEnemy?[,] exalines = new SimEnemy?[GroupCount, WavesPerGroup];
    private readonly SimEnemy?[] akhMornHelpers = new SimEnemy?[2];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP5FulgentBladeState(world.Rng, settingsWindow.Overrides);
        var solo = selectedAi is null;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP5FulgentBladeState>)AiStrats[idx]).Run(state, world);

        // TODO: the seams and their gold/purple arrows are scenery AVFX driven through its
        // trigger stages (b3560omn01_y1 / b3560omn02_y1); there's no way to queue an AVFX trigger.
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, () => pandora?.SetTarget(world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(1.5f, () => pandora?.Cast(Actions.FulgentBlade));
        world.Events.Add(13.8f, () => StartExalines(0));
        world.Events.Add(17.8f, () => StartExalines(1));
        world.Events.Add(21.8f, () => StartExalines(2));
        for (var group = 0; group < GroupCount; group++)
            for (var hit = 1; hit < HitCount; hit++)
            {
                var g = group;
                var h = hit;
                world.Events.Add(HitAt(g, h), () => RollExalines(g, h));
            }
        world.Events.Add(28.5f, () => pandora?.Cast(Actions.AkhMornPandora));
        // TODO: solo has nobody to share the light-party stacks with; spawning support bots
        // mid-run isn't possible, so solo practice ends with the exalines.
        if (!solo) world.Events.Add(36.2f, AkhMornOnLightParties);
    }

    private void SpawnActors()
    {
        pandora = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Pandora,
            NameId: BNpcNameId.Pandora,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, 0f)));
        for (var group = 0; group < GroupCount; group++)
            for (var wave = 0; wave < WavesPerGroup; wave++)
                exalines[group, wave] = SpawnHelper(state.Wave(group, wave).Placement);
        for (var i = 0; i < akhMornHelpers.Length; i++)
            akhMornHelpers[i] = SpawnHelper(new Placement(Vector3.Zero, 0f));
    }

    private SimEnemy? SpawnHelper(Placement placement) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Pandora,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: placement));

    private void StartExalines(int group)
    {
        for (var wave = 0; wave < WavesPerGroup; wave++)
            exalines[group, wave]?.Cast(state.Wave(group, wave).IsLight ? Actions.PathOfLightFirst : Actions.PathOfDarknessFirst);
    }

    private void RollExalines(int group, int hit)
    {
        for (var wave = 0; wave < WavesPerGroup; wave++)
        {
            var (placement, isLight) = state.Wave(group, wave, hit);
            exalines[group, wave]?.SetPosition(placement);
            exalines[group, wave]?.Cast(isLight ? Actions.PathOfLightRest : Actions.PathOfDarknessRest);
        }
    }

    private void AkhMornOnLightParties()
    {
        CastAkhMorn(akhMornHelpers[0], Actions.AkhMornPandoraLeft, state.AkhMornFirstLightParty);
        CastAkhMorn(akhMornHelpers[1], Actions.AkhMornPandoraRight, state.AkhMornSecondLightParty);
    }

    private void CastAkhMorn(SimEnemy? helper, EnemyAction action, PartyRole role)
    {
        if (world.Party.Get(role) is not { } target) return;
        helper?.SetPosition(target.Position);
        helper?.Cast(action, target);
    }
}
