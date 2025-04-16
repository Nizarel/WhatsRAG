using System.Text;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text.Json;
using Whats.Webhook.Models;
using Microsoft.Extensions.Logging;
using Whats.Webhook.Services;

namespace Whats.Webhook.Controllers
{
    [Route("webhook")]
    public class WebhookController : Controller
    {
        private readonly ISessionService _sessionService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WebhookController> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private bool EventTypeSubscriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "SubscriptionValidation";

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "Notification";

        public WebhookController(
            ISessionService sessionService,
            INotificationService notificationService,
            ILogger<WebhookController> logger)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpOptions]
        public IActionResult Options()
        {
            var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
            
            // Set CORS headers for webhook validation
            HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
            HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            _logger.LogInformation("Processing webhook request");
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            try
            {
                if (EventTypeSubscriptionValidation)
                {
                    _logger.LogInformation("Processing subscription validation request");
                    return await HandleValidation(jsonContent);
                }
                else if (EventTypeNotification)
                {
                    _logger.LogInformation("Processing notification event");
                    return await HandleGridEvents(jsonContent);
                }

                _logger.LogWarning("Unknown event type received");
                return BadRequest("Unknown event type");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook request");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<JsonResult> HandleValidation(string jsonContent)
        {
            var eventGridEvent = JsonSerializer.Deserialize<EventGridEvent[]>(jsonContent, _jsonOptions).First();
            var eventData = JsonSerializer.Deserialize<SubscriptionValidationEventData>(eventGridEvent.Data.ToString(), _jsonOptions);
            
            _logger.LogInformation("Subscription validation with code: {ValidationCode}", eventData.ValidationCode);
            
            var responseData = new SubscriptionValidationResponse
            {
                ValidationResponse = eventData.ValidationCode
            };
            
            return new JsonResult(responseData);
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            var eventGridEvents = JsonSerializer.Deserialize<EventGridEvent[]>(jsonContent, _jsonOptions);
            
            foreach (var eventGridEvent in eventGridEvents)
            {
                if (eventGridEvent.EventType.Equals("microsoft.communication.advancedmessagereceived", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessWhatsAppMessage(eventGridEvent);
                }
                else
                {
                    _logger.LogInformation("Skipping non-WhatsApp event type: {EventType}", eventGridEvent.EventType);
                }
            }

            return Ok();
        }
        
        private async Task ProcessWhatsAppMessage(EventGridEvent eventGridEvent)
        {
            try
            {
                var messageData = JsonSerializer.Deserialize<WhatsEventType>(eventGridEvent.Data.ToString(), _jsonOptions);
                
                if (messageData == null || string.IsNullOrEmpty(messageData.from))
                {
                    _logger.LogError("Invalid message data received");
                    return;
                }

                // Log the incoming message
                _logger.LogInformation("Received message from {Sender}: {Content}", 
                    messageData.from, messageData.content);
                
                // Keep message history for UI display
                Messages.MessagesListStatic.Add(new Message
                {
                    Text = $"Customer({messageData.from}): \"{messageData.content}\""
                });

                var sessionId = GenerateSessionId(messageData.from);
                var recipientList = new List<string> { messageData.from };

                // Check if session exists, create if not
                var sessionExists = await _sessionService.CheckSessionExistsAsync(sessionId);
                if (!sessionExists)
                {
                    await _sessionService.CreateSessionAsync(sessionId);
                }

                // Process the message and get a response from backend service
                var response = await _sessionService.ProcessChatAsync(sessionId, messageData.content);
                
                if (!string.IsNullOrEmpty(response))
                {
                    // Send the response back to the customer
                    await _notificationService.SendTextNotificationAsync(response, recipientList);
                    
                    // Add to message history for UI display
                    Messages.MessagesListStatic.Add(new Message
                    {
                        Text = $"Assistant: {response}"
                    });
                }
                else
                {
                    _logger.LogWarning("No response generated for message from {Sender}", messageData.from);
                    Messages.MessagesListStatic.Add(new Message
                    {
                        Text = "Error: No response generated from backend service."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp message");
            }
        }
        
        private static string GenerateSessionId(string phoneNumber)
        {
            return phoneNumber?.Trim();
        }
    }
}