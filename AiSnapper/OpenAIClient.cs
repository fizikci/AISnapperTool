using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiSnapper
{
    public static class OpenAIClient
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions"; // vision via content parts

        public static async Task<string> AskAsync(string prompt, string base64Png)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Build a vision message: text + image_url (data URI)
            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new object[]
                {
                    new {
                        role = "user",
                        content = new object[] {
                            new { type = "text", text = prompt },
                            new { type = "image_url", image_url = $"data:image/png;base64,{base64Png}" }
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
    }
}
