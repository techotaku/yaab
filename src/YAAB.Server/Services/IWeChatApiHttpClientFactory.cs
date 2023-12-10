using SKIT.FlurlHttpClient.Wechat.Api;

namespace YAAB.Server.Services
{
    public interface IWeChatApiHttpClientFactory
    {
        WechatApiClient Create(string appId);
    }
}
