using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoSopCreator
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey = ""; 

        public GeminiService()
        {
            _httpClient = new HttpClient();
        }

        public void SetApiKey(string key) => _apiKey = key;

        public async Task<string> AnalyzeImageAsync(string modelName, string base64Image, string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey)) return "錯誤：未設定 API Key";
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";
            
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                        }
                    }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    string resultJson = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(resultJson))
                    {
                        try 
                        {
                            string text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                            return text;
                        }
                        catch
                        {
                            return "API 回傳格式無法解析";
                        }
                    }
                }
                else return $"API 錯誤: {response.StatusCode}";
            }
            catch (Exception ex) { return $"連線異常: {ex.Message}"; }
        }
    }
}