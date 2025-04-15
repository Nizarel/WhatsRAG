using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;

////
namespace Whats.Webhook.Services
{
    public static class Utilities
    {
        public static string ConvertImageToBase64(Stream stream)
        {
            using var skData = SKData.Create(stream);
            using var skCodec = SKCodec.Create(skData);
            using var originalBitmap = SKBitmap.Decode(skCodec);

            SKImageInfo resizedInfo = new(640, 480);
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            using var resizedBitmap = originalBitmap.Resize(resizedInfo, samplingOptions);
            using var image = SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);

            return Convert.ToBase64String(data.ToArray());
        }

        public static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) return false;
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
                (strInput.StartsWith("[") && strInput.EndsWith("]")))   // For array
            {
                try
                {
                    var obj = JsonDocument.Parse(strInput);
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static string GenerateSessionId(string phoneNumber)
        {
            return $"{SanitizePhoneNumber(phoneNumber)}-{DateTime.Now.Year}-{DateTime.Now.Month}{DateTime.Now.Day}";
        }

        public static string SanitizePhoneNumber(string phoneNumber)
        {
            var regex = new Regex("[^+\\d\\-\\(\\)\\s]");
            return regex.Replace(phoneNumber, "");
        }
    }
}