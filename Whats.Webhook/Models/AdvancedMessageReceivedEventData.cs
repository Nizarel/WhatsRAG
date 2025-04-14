namespace Whats.Webhook.Models
{
    public class AdvancedMessageReceivedEventData
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Content { get; set; }
        public string ChannelType { get; set; }
        public string ReceivedTimeStamp { get; set; }
    }
}
// This class represents the data structure for an advanced message received event in a WhatsApp webhook.