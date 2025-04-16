using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Communication.Messages;
using Whats.Webhook.Models;

namespace Whats.Webhook.Services
{
    public interface INotificationService
    {
        Task SendTextNotificationAsync(string message, List<string> recipients);
    }
    public class NotificationService : INotificationService
    {
        private readonly NotificationMessagesClient _notificationMessagesClient;
        private readonly Guid _channelRegistrationId;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IOptions<NotificationMessagesClientOptions> notificationOptions,
            ILogger<NotificationService> logger)
        {
            if (string.IsNullOrEmpty(notificationOptions?.Value?.ConnectionString))
                throw new ArgumentException("NotificationMessagesClient ConnectionString is missing");
                
            if (string.IsNullOrEmpty(notificationOptions?.Value?.ChannelRegistrationId))
                throw new ArgumentException("NotificationMessagesClient ChannelRegistrationId is missing");
                
            _notificationMessagesClient = new NotificationMessagesClient(notificationOptions.Value.ConnectionString);
            _channelRegistrationId = Guid.Parse(notificationOptions.Value.ChannelRegistrationId);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendTextNotificationAsync(string message, List<string> recipients)
        {
            try
            {
                _logger.LogInformation("Sending notification to {RecipientCount} recipients", recipients.Count);
                var textContent = new TextNotificationContent(_channelRegistrationId, recipients, message);
                await _notificationMessagesClient.SendAsync(textContent);
                _logger.LogInformation("Notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to recipients");
                throw;
            }
        }
    }
}