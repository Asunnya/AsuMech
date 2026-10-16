namespace AnoMech.Tests;

// Real connections, rates in bytes per second: an upload the host's packets and poses only just
// fit through, stalls (a lost packet holding back what follows), and a peer that hitches.
internal static class BadConnections
{
    // High frame rates outrun ordinary uplinks.
    public static readonly ConnectionProfile HighFrameRateHost = new("144 fps host, 3.2 Mbit/s upload",
        UploadRate: 400_000, SendBufferBytes: 16 * 1024, UploadLatency: 0.03,
        UploadStallEvery: 8, UploadStallMin: 0.2, UploadStallMax: 0.4,
        DownloadRate: 5_000_000, DownloadLatency: 0.04, DownloadJitter: 0.004,
        DownloadStallEvery: 10, DownloadStallMin: 0.1, DownloadStallMax: 0.3,
        PeerHitchEvery: 8, PeerHitchMin: 0.08, PeerHitchMax: 0.2, HostFps: 144);

    // The rest of the line taken by other traffic.
    public static readonly ConnectionProfile SlowUpload = new("60 fps host, 1.3 Mbit/s upload",
        UploadRate: 165_000, SendBufferBytes: 16 * 1024, UploadLatency: 0.03,
        UploadStallEvery: 8, UploadStallMin: 0.2, UploadStallMax: 0.4,
        DownloadRate: 5_000_000, DownloadLatency: 0.04, DownloadJitter: 0.004,
        DownloadStallEvery: 10, DownloadStallMin: 0.1, DownloadStallMax: 0.3,
        PeerHitchEvery: 8, PeerHitchMin: 0.08, PeerHitchMax: 0.2);

    // Less upload than a busy run's sends, so they back up.
    public static readonly ConnectionProfile CongestedUpload = new("60 fps host, 0.5 Mbit/s upload",
        UploadRate: 62_500, SendBufferBytes: 16 * 1024, UploadLatency: 0.03,
        UploadStallEvery: 8, UploadStallMin: 0.2, UploadStallMax: 0.4,
        DownloadRate: 5_000_000, DownloadLatency: 0.04, DownloadJitter: 0.004,
        DownloadStallEvery: 10, DownloadStallMin: 0.1, DownloadStallMax: 0.3,
        PeerHitchEvery: 8, PeerHitchMin: 0.08, PeerHitchMax: 0.2, UploadSwing: 0.3);

    // A fine host and a peer on bad Wi-Fi.
    public static readonly ConnectionProfile BadWifiPeer = new("peer on bad Wi-Fi",
        UploadRate: 2_000_000, SendBufferBytes: 64 * 1024, UploadLatency: 0.02,
        UploadStallEvery: 30, UploadStallMin: 0.2, UploadStallMax: 0.3,
        DownloadRate: 1_500_000, DownloadLatency: 0.05, DownloadJitter: 0.02,
        DownloadStallEvery: 2, DownloadStallMin: 0.1, DownloadStallMax: 0.5,
        PeerHitchEvery: 5, PeerHitchMin: 0.1, PeerHitchMax: 0.25, UploadSwing: 0.3);

    public static readonly ConnectionProfile[] All = [HighFrameRateHost, SlowUpload, CongestedUpload, BadWifiPeer];
}
