namespace YAAB.Server.Models
{
    public class WeChatAccessTokenEntity
    {
        public string AppId { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public long ExpireTimestamp { get; set; }

        public long UpdateTimestamp { get; set; }

        public long CreateTimestamp { get; set; }
    }
}
