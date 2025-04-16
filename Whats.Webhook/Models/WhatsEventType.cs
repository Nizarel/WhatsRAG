using System.Text.Json.Serialization;

namespace Whats.Webhook.Models
{
    public class WhatsEventType
    {
        [JsonPropertyName("content")]
        public string? content { get; set; }

        [JsonPropertyName("channelType")]
        public string? channelType { get; set; }

        [JsonPropertyName("from")]
        public string? from { get; set; }

        [JsonPropertyName("to")]
        public string? to { get; set; }

        [JsonPropertyName("receivedTimestamp")]
        public DateTime receivedTimestamp { get; set; }

        public Media? media { get; set; }
    }
}
// This class represents the data structure for an advanced message received event in a WhatsApp webhook.