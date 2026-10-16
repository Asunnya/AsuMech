namespace AnoMech.Tests;

// How a connection misbehaves. Rates are bytes per second and times are seconds; each "Every"
// is the mean gap between random events of that kind. UploadSwing is how far the upload's share
// moves either way, as a fraction, each UploadSwingEvery: other traffic on the same line (a voice
// call, a stream) comes and goes. SendLoopLate is how often an idle send loop only wakes after a
// frame's later sends have joined the first.
public sealed record ConnectionProfile(
    string Name,
    double UploadRate, int SendBufferBytes, double UploadLatency,
    double UploadStallEvery, double UploadStallMin, double UploadStallMax,
    double DownloadRate, double DownloadLatency, double DownloadJitter,
    double DownloadStallEvery, double DownloadStallMin, double DownloadStallMax,
    double PeerHitchEvery, double PeerHitchMin, double PeerHitchMax,
    double UploadSwing = 0, double UploadSwingEvery = 0.25,
    double HostFps = 60, double PeerFps = 60, double SendLoopLate = 0.25)
{
    public override string ToString() => Name;
}
