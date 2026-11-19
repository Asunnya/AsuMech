using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Top.P2PartySynergy;
using AsuMech.Scenarios.Top.P5Delta;

namespace AsuMech.Tests;

// However a peer's run ends, nothing the host's packets put on its own player stays on it.
public class PeerCleanupTests
{
    private const double Every = 0.25, Until = 60, Forever = 1e9;

    public enum Ending { HostResets, HostVanishes, PeerLeavesTheSession }

    // The peer's player outlives the stay, and the next run's doppels come back with the same
    // entity ids: a tether left on it reappears, tied to whoever now holds the id.
    [Test]
    public void APeersPlayerKeepsNoTetherOnceItsRunEnds(
        [Values(typeof(TopP2PartySynergyScenario), typeof(TopP5DeltaScenario))] Type scenarioType, [Values] Ending ending)
    {
        var peerSeat = scenarioType == typeof(TopP5DeltaScenario) ? PartyRole.MainTank : PartyRole.RegenHealer;
        TestRelay? relay = null;
        ushort held = 0;
        var ended = false;
        var inputs = new List<(double At, int Client, Action<TestClient> Input)>();
        for (var at = Every; at < Until; at += Every)
        {
            inputs.Add((at, 1, peer =>
            {
                if (held == 0) held = peer.Fake.BattleCharas.Player.Tethers.GetValueOrDefault((byte)0);
                else if (!ended && ending is Ending.PeerLeavesTheSession) End(peer);
            }));
            inputs.Add((at, 0, host =>
            {
                if (held != 0 && !ended && ending is not Ending.PeerLeavesTheSession) End(host);
            }));
        }
        void End(TestClient client)
        {
            ended = true;
            switch (ending)
            {
                case Ending.HostResets:
                    client.Game.Reset();
                    break;
                case Ending.HostVanishes:
                    relay!.Schedule(0, relay.Now, new LinkDrops(Forever));
                    break;
                case Ending.PeerLeavesTheSession:
                    client.Multiplayer.LeaveSession();
                    if (client.Game.World.Map.IsInInstance) client.Game.Leave();
                    break;
            }
        }
        var run = MultiplayerRun.Play(scenarioType, 0, 1, new PlayerSeat(PartyRole.MeleeDpsA), [new PlayerSeat(peerSeat)],
            immortal: true, interrupt: r => relay = r, inputs: inputs);
        Assert.That(run.Failure, Is.Null);
        Assert.That(held, Is.Not.Zero, "the peer's player held no tether");
        Assert.That(ended, Is.True, "the run ended before the ending");
        Assert.That(run.Peers[0].Fake.BattleCharas.Player.Tethers.Values.Where(id => id != 0), Is.Empty);
    }
}
