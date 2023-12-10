using Microsoft.Extensions.Options;
using SKIT.FlurlHttpClient.Wechat.Api;

namespace YAAB.Server.Services
{
    internal partial class WeChatApiHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<Options.WeChatOptions> wechatOptions) : IWeChatApiHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly Options.WeChatOptions _wechatOptions = wechatOptions.Value;

        public WechatApiClient Create(string appId)
        {
            var wechatAccountOptions = (_wechatOptions.Accounts?.FirstOrDefault(e => string.Equals(appId, e.AppId))) ?? throw new Exception("未在配置项中找到该 AppId 对应的微信账号。");

            var wechatApiClientOptions = new WechatApiClientOptions()
            {
                AppId = wechatAccountOptions.AppId,
                AppSecret = wechatAccountOptions.AppSecret,
                PushEncodingAESKey = wechatAccountOptions.CallbackEncodingAESKey,
                PushToken = wechatAccountOptions.CallbackToken
            };
            var wechatApiClient = new WechatApiClient(wechatApiClientOptions);
            wechatApiClient.Configure((settings) => settings.FlurlHttpClientFactory = new DelegatingFlurlClientFactory(_httpClientFactory));
            return wechatApiClient;
        }
    }

    internal partial class WeChatApiHttpClientFactory
    {
        internal class DelegatingFlurlClientFactory(IHttpClientFactory httpClientFactory) : Flurl.Http.Configuration.DefaultHttpClientFactory
        {
            private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

            public override HttpClient CreateHttpClient(HttpMessageHandler handler)
            {
                return _httpClientFactory.CreateClient();
            }
        }
    }

}
