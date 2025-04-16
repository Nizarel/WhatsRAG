using System.Net.Http.Headers;
using System.Text.Json;
using Azure;
using Azure.Communication.Messages;
using Whats.Webhook.Repositories;
using Whats.Webhook.Models;


namespace Whats.Webhook.Services
{
    public class MediaService
    {
        private static readonly string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING")
            ?? throw new ArgumentNullException("COMMUNICATION_SERVICES_CONNECTION_STRING environment variable is not set.");

        private readonly ChatRepository _chatRepository;
        private readonly NotificationMessagesClient _notificationMessagesClient;

        public MediaService(ChatRepository chatRepository)
        {
            _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
            _notificationMessagesClient = new NotificationMessagesClient(connectionString);
        }

        public async Task<string?> ProcessImageAsync(WhatsEventType eventData, string sessionId, ILogger log)
        {
            try
            {
                if (eventData.media == null || string.IsNullOrEmpty(eventData.media.id))
                {
                    log.LogError("Media data is null or media ID is missing.");
                    return null;
                }

                log.LogInformation("Processing image with media ID: {MediaId}", eventData.media.id);

                Response<Stream> mediaContentResponse = await _notificationMessagesClient.DownloadMediaAsync(eventData.media.id);
                using (Stream mediaContentStream = mediaContentResponse.Value)
                {
                    log.LogInformation("Media content length: {Length}", mediaContentStream.Length);

                    string base64Image = Utilities.ConvertImageToBase64(mediaContentStream);
                    log.LogInformation("Base64 image length: {Length}", base64Image.Length);

                    string promptText = eventData.media.caption ?? string.Empty;
                    return await SendImageAnalysisRequestAsync(sessionId, promptText, base64Image, log);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the image.");
                return null;
            }
        }

        public async Task<string?> ProcessVoiceAsync(WhatsEventType eventData, string sessionId, ILogger log)
        {
            try
            {
                if (eventData.media == null || string.IsNullOrEmpty(eventData.media.id))
                {
                    log.LogError("Media data is null or media ID is missing.");
                    return null;
                }

                log.LogInformation("Processing voice message with media ID: {MediaId}", eventData.media.id);

                Response<Stream> mediaContentResponse = await _notificationMessagesClient.DownloadMediaAsync(eventData.media.id);
                using (Stream mediaContentStream = mediaContentResponse.Value)
                {
                    log.LogInformation("Media content length: {Length}", mediaContentStream.Length);

                    return await SendVoiceAnalysisRequestAsync(sessionId, mediaContentStream, log);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the voice message.");
                return null;
            }
        }

        private async Task<string?> SendImageAnalysisRequestAsync(string sessionId, string promptText, string base64Image, ILogger log)
        {
            var imagePayload = new
            {
                sessionId = sessionId,
                promptText = promptText,
                imageFile = base64Image
            };

            try
            {
                var response = await _chatRepository.SendImageRequest(imagePayload);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent) && Utilities.IsValidJson(responseContent))
                {
                    var chatCompletion = JsonSerializer.Deserialize<ChatCompletion>(responseContent);
                    return chatCompletion?.completion;
                }
                else
                {
                    log.LogError("Invalid response received from image analysis.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred during image analysis request.");
                return null;
            }
        }

        private async Task<string?> SendVoiceAnalysisRequestAsync(string sessionId, Stream voiceStream, ILogger log)
        {
            var apiUrl = _chatRepository.GetVoiceApiUrl();
            try
            {
                using (var content = new MultipartFormDataContent())
                {
                    if (voiceStream != null)
                    {
                        var fileContent = new StreamContent(voiceStream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(fileContent, "audioFile", "voiceMessage.wav");
                    }

                    content.Add(new StringContent(sessionId), "SessionId");

                    var response = await _chatRepository.SendVoiceRequest(apiUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(responseContent) && Utilities.IsValidJson(responseContent))
                    {
                        var chatCompletion = JsonSerializer.Deserialize<ChatCompletion>(responseContent);
                        return chatCompletion?.completion;
                    }
                    else
                    {
                        log.LogError("Invalid response received from voice analysis.");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred during voice analysis request.");
                return null;
            }
        }
    }
}