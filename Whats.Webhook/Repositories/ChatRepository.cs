using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Whats.Webhook.Repositories
{
    public class ChatRepository
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string chatapi = Environment.GetEnvironmentVariable("AGENT_API") ?? "default_api_url";

        public async Task<HttpResponseMessage> GetSession(string sessionId)
        {
            return await httpClient.GetAsync($"{chatapi}session/{sessionId}");
        }

        public async Task<HttpResponseMessage> CreateSession(string sessionId)
        {
            return await httpClient.PostAsync($"{chatapi}session/{sessionId}", null);
        }

        public async Task<HttpResponseMessage> SendChatRequest(object chatPayload)
        {
            return await httpClient.PostAsync(chatapi, new StringContent(JsonSerializer.Serialize(chatPayload), Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> SendImageRequest(object chatPayload)
        {
            return await httpClient.PostAsync(chatapi + "image", new StringContent(JsonSerializer.Serialize(chatPayload), Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> SendVoiceRequest(string apiUrl, MultipartFormDataContent content)
        {
            return await httpClient.PostAsync(apiUrl, content);
        }

        public string GetVoiceApiUrl()
        {
            return chatapi + "voice";
        }
    }
}