namespace AnoMech.Multiplayer;

// The server multiplayer uses unless the player picks their own in Settings: the same relay that
// takes the pull recordings, with its join token. Dialed as wss:// outright: it serves TLS, so
// the unencrypted fallback a bare address gets could only downgrade it. The upstream plugin's
// relay stays reachable as a custom server, since clients send the legacy headers too.
public static class DefaultRelay
{
    public const string Address = "asulinux-1.taild1e27f.ts.net";
    public const string Url = "wss://" + Address;
    public const string Token = DefaultRecordingsRelay.Token;
    public const string Location = "AsuMech server";
    public const string UpstreamUrl = "wss://sim.everscloset.com";
}
