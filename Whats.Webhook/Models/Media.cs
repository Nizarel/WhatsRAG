using System.Text.Json.Serialization;

namespace Whats.Webhook.Models
{
    public class Media
    {
        [JsonPropertyName("mimeType")]
        public string? mimeType { get; set; }

        [JsonPropertyName("id")]
        public string? id { get; set; }

        [JsonPropertyName("caption")]
        public string? caption { get; set; }
    
    }
}