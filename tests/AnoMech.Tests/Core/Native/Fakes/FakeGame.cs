using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// The game client behind Natives for a headless run.
internal sealed class FakeGame
{
    public SimTimeline Timeline { get; } = new();
    public FakeBattleCharas BattleCharas { get; }
    public FakeEventObjects EventObjects { get; }
    public FakeFramework Framework { get; } = FakeFramework.Create();
    public FakeZoneControl Zone { get; }
    public FakeMapEffects MapEffects { get; }
    public FakeUserActions UserActions { get; } = new();
    public FakeLocalPlayerInput PlayerInput { get; } = new();
    public FakePlayerConditions PlayerConditions { get; } = new();

    private FakeGame()
    {
        BattleCharas = new FakeBattleCharas(Timeline);
        EventObjects = new FakeEventObjects(Timeline);
        MapEffects = new FakeMapEffects(Timeline);
        Zone = new FakeZoneControl(BattleCharas.Player, Timeline);
    }

    public static FakeGame Install()
    {
        var game = new FakeGame();
        var rsv = new FakeRsvFunctions();
        Natives.Data = new DataminingGameData(rsv);
        Natives.BattleCharas = game.BattleCharas;
        Natives.EventObjects = game.EventObjects;
        Natives.PlayerInput = game.PlayerInput;
        Natives.UserActions = game.UserActions;
        Natives.TimelinePreload = new FakeActionTimelinePreload();
        Natives.Zone = game.Zone;
        Natives.MapEffects = game.MapEffects;
        Natives.Layout = new FakeLayoutFunctions();
        Natives.Director = new FakeInstanceContentDirector(game.Zone, game.Timeline);
        Natives.Rsv = rsv;
        Natives.Rsf = new FakeRsfFunctions();
        Natives.PartyHud = new FakePartyHud();
        Natives.HaterList = new FakeHaterList(game.Timeline);
        Natives.PlayerConditions = game.PlayerConditions;
        Natives.LimitBreak = new FakeLimitBreakController();
        Natives.Waymarks = new FakeWaymarks();
        Natives.Bgm = new FakeBgm(game.Timeline);
        DalamudServices.Install(nameof(Plugin.Framework), game.Framework);
        return game;
    }

    // One client frame in the real order: Dalamud drains queued Framework.Run work, then raises
    // Update (the plugin), then the game runs its own update (cast bars, carries, spawns).
    public void Frame(float deltaSeconds, Action<float> update)
    {
        Framework.RunPending();
        update(deltaSeconds);
        WalkSteeredPlayer(deltaSeconds);
        BattleCharas.Tick(deltaSeconds);
    }

    private void WalkSteeredPlayer(float deltaSeconds)
    {
        if (PlayerInput.SteerDirection is not { } dir) return;
        var step = dir * PlayerInput.SteerSpeed * deltaSeconds;
        BattleCharas.Player.Position += new System.Numerics.Vector3(step.X, 0f, step.Y);
        BattleCharas.Player.Rotation = MathF.Atan2(dir.X, dir.Y);
    }
}
