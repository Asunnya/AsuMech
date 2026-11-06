namespace AnoMech.Multiplayer;

// Where finished pull recordings go unless Configuration.RecordingsRelayUrl says otherwise: the
// study relay, independent of the multiplayer relay a player picks. The token keeps drive-by
// scanners off the endpoint, not people with this source; the relay's size caps and its
// --recordings-from list are the real limits. Empty Url turns the default off.
public static class DefaultRecordingsRelay
{
    public const string Url = "wss://asulinux-1.taild1e27f.ts.net";
    public const string Token = "af29ddc91d65317a68a5958f785206ac";
}
