using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsuMech.Core;
using AsuMech.Network;

namespace AsuMech.Multiplayer;

// One thumbs up or down to the study relay's POST /feedback, with the credential a recording
// upload presents. The relay token only travels over https.
internal static class LessonFeedbackSender
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public sealed record Result(bool Ok, string Message);

    public static async Task<Result> SendAsync(LessonFeedback feedback, string relayUrl, string? accessToken, string peerSecret, CancellationToken ct = default)
    {
        if (!feedback.IsValid) return new Result(false, "nothing valid to send");
        if (string.IsNullOrWhiteSpace(relayUrl)) return new Result(false, "no study server configured");
        IReadOnlyList<Uri> candidates = RelayClient.ResolveHttpCandidateUris(relayUrl, "feedback");
        if (!string.IsNullOrEmpty(accessToken))
        {
            candidates = candidates.Where(u => u.Scheme == Uri.UriSchemeHttps).ToList();
            if (candidates.Count == 0) return new Result(false, "the server has a password but isn't reached over https; not sending it in the clear");
        }
        var body = JsonSerializer.Serialize(feedback, LessonFeedback.Json);
        string? last = null;
        foreach (var uri in candidates)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                foreach (var name in RelayHeaders.Names(RelayHeaders.Protocol)) request.Headers.Add(name, RelayWire.Version.ToString());
                foreach (var name in RelayHeaders.Names(RelayHeaders.PeerSecret)) request.Headers.Add(name, peerSecret);
                if (!string.IsNullOrEmpty(accessToken))
                    foreach (var name in RelayHeaders.Names(RelayHeaders.RelayToken)) request.Headers.Add(name, accessToken);
                using var response = await Http.SendAsync(request, ct).ConfigureAwait(false);
                if (response.IsSuccessStatusCode) return new Result(true, "thanks, sent");
                last = (int)response.StatusCode switch
                {
                    404 => "the study server doesn't take feedback",
                    429 => "too many votes this hour, try later",
                    _ => $"{uri.Host} answered {(int)response.StatusCode}",
                };
                break;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                last = $"{uri.Host}: {e.Message}";
                DiagnosticLog.Info($"[LessonFeedback] {uri} failed: {e.Message}");
            }
        }
        return new Result(false, last ?? "no server address to try");
    }
}
