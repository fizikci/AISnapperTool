using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiSnapper
{
    public static class OpenAIClient
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions"; // vision via content parts
        private const string DefaultModel = "gpt-4o"; // best general vision model

        public static async Task<string> AskAsync(string prompt, string base64Png)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? DefaultModel;

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Build a vision message: text + image_url (data URI). image_url must be an object with url/detail.
            var payload = new
            {
                model = model,
                messages = new object[]
                {
                    new {
                        role = "user",
                        content = new object[] {
                            new { type = "text", text = prompt },
                            new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Png}", detail = "high" } }
                        }
                    }
                },
                temperature = 0.2
            };

            var json = JsonSerializer.Serialize(payload);
            var res = await _http.PostAsync(ApiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI error {res.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return text ?? "";
        }

        // New: Multi-turn chat support. Pass the entire messages array as-is.
        public static async Task<string> AskAsync(object[] messages, string? modelOverride = null)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var model = modelOverride ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? DefaultModel;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = model,
                messages = messages,
                temperature = 0.2
            };

            var json = JsonSerializer.Serialize(payload);
            var res = await _http.PostAsync(ApiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI error {res.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return text ?? "";
        }

        // Streaming: emit deltas as they arrive via Server-Sent Events
        public static async Task AskStreamAsync(object[] messages, Action<string> onDelta, Action? onCompleted = null, string? modelOverride = null, CancellationToken ct = default)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var model = modelOverride ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? DefaultModel;

            var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            // Accept not strictly required, but okay to hint SSE
            // req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var payload = new
            {
                model = model,
                messages = messages,
                temperature = 0.2,
                stream = true
            };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new InvalidOperationException($"OpenAI error {res.StatusCode}: {err}");
            }

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data:")) continue;
                var data = line.Substring(5).Trim();
                if (data == "[DONE]") break;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0) continue;
                    var delta = choices[0].GetProperty("delta");

                    // Handle content in either string or array form
                    if (delta.TryGetProperty("content", out var contentProp))
                    {
                        switch (contentProp.ValueKind)
                        {
                            case JsonValueKind.String:
                                var s = contentProp.GetString();
                                if (!string.IsNullOrEmpty(s)) onDelta(s!);
                                break;
                            case JsonValueKind.Array:
                                foreach (var part in contentProp.EnumerateArray())
                                {
                                    if (part.ValueKind == JsonValueKind.String)
                                    {
                                        var ps = part.GetString();
                                        if (!string.IsNullOrEmpty(ps)) onDelta(ps!);
                                    }
                                    else if (part.ValueKind == JsonValueKind.Object && part.TryGetProperty("text", out var t))
                                    {
                                        var ps = t.GetString();
                                        if (!string.IsNullOrEmpty(ps)) onDelta(ps!);
                                    }
                                }
                                break;
                        }
                    }
                }
                catch { /* ignore malformed lines */ }
            }
            onCompleted?.Invoke();
        }
    }
}
