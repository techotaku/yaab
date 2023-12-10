namespace YAAB.Server.Options
{
    public class WeChatOptions
    {
        public const string WeChat = "WeChat";

        public WeChatAccount[] Accounts { get; set; } = [];
    }

    public class WeChatAccount
    {
        public string GhId { get; set; } = string.Empty;

        public string AppId { get; set; } = string.Empty;

        public string AppSecret { get; set; } = string.Empty;

        public string CallbackToken { get; set; } = string.Empty;

        public string CallbackEncodingAESKey { get; set; } = string.Empty;
    }
}
