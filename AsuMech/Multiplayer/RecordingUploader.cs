using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsuMech.Core;
using AsuMech.Network;

namespace AsuMech.Multiplayer;

// Sends a finished pull recording to the active relay's POST /recordings, gzipped, with the same
// credential a join presents. The relay token only ever travels over https, as RelayClient does.
internal static class RecordingUploader
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public sealed record Result(bool Ok, string Message);

    public static async Task<Result> UploadAsync(string path, string relayUrl, string? accessToken, string peerSecret, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relayUrl)) return new Result(false, "no relay configured");
        byte[] body;
        try
        {
            body = await Task.Run(() => Gzip(path), ct).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return new Result(false, $"couldn't read {path}: {e.Message}");
        }
        IReadOnlyList<Uri> candidates = RelayClient.ResolveHttpCandidateUris(relayUrl, "recordings");
        if (!string.IsNullOrEmpty(accessToken))
        {
            candidates = candidates.Where(u => u.Scheme == Uri.UriSchemeHttps).ToList();
            if (candidates.Count == 0) return new Result(false, "the relay has a password but isn't reached over https; not sending it in the clear");
        }
        string? last = null;
        foreach (var uri in candidates)
        {
            // One retry: a relay's reverse proxy drops the odd connection.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new ByteArrayContent(body) };
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/gzip");
                    foreach (var name in RelayHeaders.Names(RelayHeaders.RecordingName)) request.Headers.Add(name, Path.GetFileName(path));
                    foreach (var name in RelayHeaders.Names(RelayHeaders.Protocol)) request.Headers.Add(name, RelayWire.Version.ToString());
                    foreach (var name in RelayHeaders.Names(RelayHeaders.PeerSecret)) request.Headers.Add(name, peerSecret);
                    if (!string.IsNullOrEmpty(accessToken))
                        foreach (var name in RelayHeaders.Names(RelayHeaders.RelayToken)) request.Headers.Add(name, accessToken);
                    using var response = await Http.SendAsync(request, ct).ConfigureAwait(false);
                    var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var name = JsonDocument.Parse(text).RootElement.GetProperty("name").GetString();
                        return new Result(true, $"uploaded to {uri.Host} as {name} ({body.Length / 1024} KB)");
                    }
                    last = $"{uri.Host} answered {(int)response.StatusCode}" + ((int)response.StatusCode switch
                    {
                        401 => " (relay password wrong or missing)",
                        403 => " (this peer isn't allowed to upload there)",
                        404 => " (that relay doesn't take recordings)",
                        413 => " (too large for the relay's cap)",
                        _ => "",
                    });
                    break;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    last = $"{uri.Host}: {e.Message}";
                    DiagnosticLog.Info($"[RecordingUploader] {uri} attempt {attempt + 1} failed: {e.Message}");
                }
            }
        }
        return new Result(false, last ?? "no relay address to try");
    }

    private static byte[] Gzip(string path)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        using (var input = File.OpenRead(path))
            input.CopyTo(gzip);
        return output.ToArray();
    }
}
