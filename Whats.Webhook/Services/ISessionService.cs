using Microsoft.Extensions.Logging;

namespace Whats.Webhook.Services
{
    public interface ISessionService
    {
        Task<bool> CheckSessionExistsAsync(string sessionId);
        Task CreateSessionAsync(string sessionId);
        Task<string> ProcessChatAsync(string sessionId, string message);
    }
}