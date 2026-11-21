using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P5ParadiseLost;

// The enrage. Pandora falls just before her cast ends, as she does for a party that clears, unless
// the enrage is asked to play out.
public sealed class FruP5ParadiseLostScenario : IScenario
{
    public string Name => "Paradise Lost";
    public IPhase Phase => FruZone.P5;
    public float BgmSecondsAtStart => 261f;
    public bool SupportsSolo => true;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5ParadiseLostAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly FruP5ParadiseLostSettingsWindow settingsWindow = new();

    private SimWorld world = null!;
    private FruP5ParadiseLostState state = null!;
    private SimEnemy? pandora;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new FruP5ParadiseLostState(settingsWindow.Overrides);
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP5ParadiseLostState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnActors);
        world.Events.Add(1f, () => pandora?.SetTarget(world.Party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(1.5f, () => pandora?.Cast(ActionId.ParadiseLost, animationLock: 3.1f));
        world.Events.Add(1.5f, StartTheEnrage);
        world.Events.Add(1.5f, () => new FruBattleTalk(0, 0x30, BNpcNameId.Pandora).Say(world, pandora));
        world.Events.Add(7.01f, () => new FruBattleTalk(0, 0x31, GaiaBattleTalk).Say(world, pandora));
        world.Events.Add(12.04f, () => new FruBattleTalk(0, 0x32, RyneBattleTalk).Say(world, pandora));
        world.Events.Add(12.89f, DefeatPandora);
    }

    private void SpawnActors()
    {
        pandora = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Pandora,
            NameId: BNpcNameId.Pandora,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI),
            LayoutId: LayoutId.PandoraP5));
    }

    private void StartTheEnrage()
    {
        if (state.PlayOutEnrage) pandora?.CastFromHelper(Actions.ParadiseLostEnrage, at: new Placement(Vector3.Zero, MathF.PI));
    }

    private void DefeatPandora()
    {
        if (!state.PlayOutEnrage) pandora?.Defeat();
    }
}
