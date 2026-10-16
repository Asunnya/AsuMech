using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnoMech.Multiplayer;

// The relay link MultiplayerManager talks through: RelayClient in the game, an in-memory relay
// in tests.
internal interface IRelayConnection : IDisposable
{
    bool IsConnected { get; }
    bool IsEncrypted { get; }
    bool FellBackToUnencrypted { get; }
    bool SupportsCompression { get; }
    bool SupportsSenderIdentity { get; }

    // (message, isFromHost, relay-assigned sender connection id, authenticated sender identity)
    event Action<MpMessage, bool, uint, Guid>? MessageReceived;
    event Action<IReadOnlyList<Guid>>? PeersRemoved;
    event Action<Exception?>? Disconnected;

    Task ConnectAsync(string relayUrl, string sessionCode, string? accessToken = null);
    Task<string?> ConnectAndHostAsync(string relayUrl, string? accessToken = null);
    Task SendAsync(MpMessage message);
    Task ModerateAsync(string operation, Guid peerId);
}
