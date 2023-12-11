using System.Text.Json.Serialization;

namespace YAAB.Cloudflare.Models
{
    public class TextToImageRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("num_steps")]
        public int Steps { get; set; } = 20;
    }
}
