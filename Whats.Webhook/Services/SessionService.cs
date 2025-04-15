using System.Net;
using System.Text.Json;
using Whats.Webhook.Repositories;
using Whats.Webhook.Models;

namespace Whats.Webhook.Services
{
    public class SessionService
    {
        private readonly ChatRepository _chatRepository;

        public SessionService(ChatRepository chatRepository)
        {
            _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        }

        public async Task<bool> CheckSessionExistsAsync(string sessionId, ILogger log)
        {
            try
            {
                var response = await _chatRepository.GetSession(sessionId);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    log.LogInformation("The session already exists.");
                    return true;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    log.LogInformation("The session does not exist.");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while checking the session.");
                throw;
            }

            return false;
        }

        public async Task CreateSessionAsync(string sessionId, ILogger log)
        {
            try
            {
                var response = await _chatRepository.CreateSession(sessionId);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    log.LogInformation("Session was successfully created.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    log.LogError("Failed to create session. StatusCode: {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while creating the session.");
                throw;
            }
        }

        public async Task<string?> ProcessChatAsync(string sessionId, string messageContent, ILogger log)
        {
            var chatPayload = new
            {
                sessionId = sessionId,
                promptText = messageContent
            };

            try
            {
                var chatResponse = await _chatRepository.SendChatRequest(chatPayload);
                var chatResult = await chatResponse.Content.ReadAsStringAsync();

                log.LogInformation("Chat response: {ChatResult}", chatResult);

                if (string.IsNullOrEmpty(chatResult))
                {
                    log.LogError("Chat result is null or empty.");
                    return null;
                }

                if (chatResult == "Invalid request")
                {
                    log.LogError("Chat response indicates an invalid request.");
                    return null;
                }

                if (Utilities.IsValidJson(chatResult))
                {
                    try
                    {
                        var chatCompletionObject = JsonSerializer.Deserialize<ChatCompletion>(chatResult);
                        if (chatCompletionObject != null && !string.IsNullOrEmpty(chatCompletionObject.completion))
                        {
                            return chatCompletionObject.completion;
                        }
                        else
                        {
                            log.LogError("Chat completion object is null or empty.");
                        }
                    }
                    catch (JsonException ex)
                    {
                        log.LogError(ex, "Failed to deserialize chat result.");
                    }
                }
                else
                {
                    log.LogError("Chat result is not a valid JSON.");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the chat.");
                throw;
            }

            return null;
        }
    }
}