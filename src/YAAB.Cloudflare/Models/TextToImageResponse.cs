namespace YAAB.Cloudflare.Models
{
    public class TextToImageResponse
    {
        public byte[] Image { get; set; } = [];

        public string Message { get; set; } = string.Empty;
    }
}
