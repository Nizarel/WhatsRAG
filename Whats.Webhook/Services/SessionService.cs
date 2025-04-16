using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace Whats.Webhook.Services
{
    public class SessionService : ISessionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SessionService> _logger;
        private readonly string _apiBaseUrl;

        public SessionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SessionService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiBaseUrl = configuration["BackendApi:BaseUrl"] ?? throw new ArgumentException("BackendApi:BaseUrl configuration is missing");
        }

        public async Task<bool> CheckSessionExistsAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/sessions/{sessionId}/exists");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if session {SessionId} exists", sessionId);
                // Default to false, which will trigger session creation
                return false;
            }
        }

        public async Task CreateSessionAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/sessions/{sessionId}", null);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Created new session: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<string> ProcessChatAsync(string sessionId, string message)
        {
            try
            {
                _logger.LogInformation("Processing chat message for session {SessionId}", sessionId);
                
                var request = new
                {
                    Message = message
                };

                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/sessions/{sessionId}/chat", request);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                return result?.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat for session {SessionId}", sessionId);
                return null;
            }
        }
    }

    public class ChatResponse
    {
        public string Response { get; set; }
    }
}