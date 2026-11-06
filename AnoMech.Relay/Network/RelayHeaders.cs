using System.Collections.Generic;

namespace AnoMech.Network;

// Request headers clients send to a relay. Relays still run by the upstream plugin only read the
// legacy prefix, so clients send every header under both names and relays accept either.
public static class RelayHeaders
{
    public const string Protocol = "Protocol";
    public const string PeerSecret = "Peer-Secret";
    public const string RelayToken = "Relay-Token";
    public const string AdminToken = "Admin-Token";
    public const string RecordingName = "Recording-Name";

    public const string Prefix = "X-AsuMech-";
    public const string LegacyPrefix = "X-AnoMech-"; // rename:keep

    public static IEnumerable<string> Names(string field) => [Prefix + field, LegacyPrefix + field];
}
