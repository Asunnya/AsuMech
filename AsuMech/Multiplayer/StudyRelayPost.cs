using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsuMech.Core;
using AsuMech.Network;

namespace AsuMech.Multiplayer;

// A JSON POST to the study relay with the credential a recording upload presents. The relay token
// only travels over https.
internal static class StudyRelayPost
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public sealed record Result(bool Ok, string Message, string? Body = null);

    public static async Task<Result> SendAsync(string path, string json, string relayUrl, string? accessToken, string credential, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relayUrl)) return new Result(false, "no study server configured");
        IReadOnlyList<Uri> candidates = RelayClient.ResolveHttpCandidateUris(relayUrl, path);
        if (!string.IsNullOrEmpty(accessToken))
        {
            candidates = candidates.Where(u => u.Scheme == Uri.UriSchemeHttps).ToList();
            if (candidates.Count == 0) return new Result(false, "the server has a password but isn't reached over https; not sending it in the clear");
        }
        string? last = null;
        foreach (var uri in candidates)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                foreach (var name in RelayHeaders.Names(RelayHeaders.Protocol)) request.Headers.Add(name, RelayWire.Version.ToString());
                foreach (var name in RelayHeaders.Names(RelayHeaders.PeerSecret)) request.Headers.Add(name, credential);
                if (!string.IsNullOrEmpty(accessToken))
                    foreach (var name in RelayHeaders.Names(RelayHeaders.RelayToken)) request.Headers.Add(name, accessToken);
                using var response = await Http.SendAsync(request, ct).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return new Result(true, "sent", text);
                last = (int)response.StatusCode switch
                {
                    404 => $"the study server doesn't take /{path}",
                    413 => "too large for the server",
                    429 => "too many sent this hour, try later",
                    _ => $"{uri.Host} answered {(int)response.StatusCode}",
                };
                break;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                last = $"{uri.Host}: {e.Message}";
                DiagnosticLog.Info($"[StudyRelay] {uri} failed: {e.Message}");
            }
        }
        return new Result(false, last ?? "no server address to try");
    }

    // The study relay this install sends to, and its credential there.
    public static (string Url, string? Token, string Credential) Destination()
    {
        var config = Plugin.Config;
        var (url, token) = config.RecordingsDestination();
        return (url, token, RelayWire.RelayCredential(config.EnsurePeerSecret(), url));
    }
}
