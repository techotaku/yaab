using Microsoft.AspNetCore.Mvc;
using SKIT.FlurlHttpClient.Wechat.Api;
using SKIT.FlurlHttpClient.Wechat.Api.Events;
using System.Text;

namespace YAAB.Server.Controllers
{
    [ApiController]
    [Route("api/wechat")]
    public class WeChatNotifyController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly Services.IWeChatApiHttpClientFactory _wechatApiHttpClientFactory;


        public WeChatNotifyController(
            ILoggerFactory loggerFactory,
            Services.IWeChatApiHttpClientFactory wechatApiHttpClientFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _wechatApiHttpClientFactory = wechatApiHttpClientFactory;
        }

        [HttpGet]
        [Route("{app_id}/message-push")]
        public IActionResult VerifyMessage(
            [FromRoute(Name = "app_id")] string appId,
            [FromQuery(Name = "timestamp")] string timestamp,
            [FromQuery(Name = "nonce")] string nonce,
            [FromQuery(Name = "signature")] string signature,
            [FromQuery(Name = "echostr")] string echoString)
        {
            var client = _wechatApiHttpClientFactory.Create(appId);
            bool valid = client.VerifyEventSignatureForEcho(callbackTimestamp: timestamp, callbackNonce: nonce, callbackSignature: signature);
            if (!valid)
            {
                return Content("fail");
            }

            return Content(echoString);
        }

        [HttpPost]
        [Route("{app_id}/message-push")]
        public async Task<IActionResult> ReceiveMessage(
            [FromRoute(Name = "app_id")] string appId)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string content = await reader.ReadToEndAsync();
            _logger.LogDebug("接收到微信推送的数据：{content}", content);

            var client = _wechatApiHttpClientFactory.Create(appId);
            var message = client.DeserializeEventFromXml(content);

            TextMessageReply? response = null;
            if (message != null && !string.IsNullOrEmpty(message.FromUserName) && !string.IsNullOrEmpty(message.ToUserName))
            {
                response = new TextMessageReply()
                {
                    ToUserName = message.FromUserName,
                    FromUserName = message.ToUserName,
                    MessageType = "text",
                    CreateTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                };
            }

            var msgType = message?.MessageType?.ToUpper();
            switch (msgType)
            {
                case "TEXT":
                    {
                        var eventModel = client.DeserializeEventFromXml<TextMessageEvent>(content);
                        _logger.LogInformation("接收到微信推送的文本消息，消息内容：{content}", eventModel.Content);
                        if (response != null)
                        {
                            response.Content = $"文本消息收到：{eventModel.Content}";
                        }
                    }
                    break;
                case "VOICE":
                    {
                        var eventModel = client.DeserializeEventFromXml<VoiceMessageEvent>(content);
                        _logger.LogInformation("接收到微信推送的语音消息，语音ID：{mediaId}", eventModel.MediaId);
                        if (response != null)
                        {
                            response.Content = $"语音消息收到：[{eventModel.Format}] {eventModel.MediaId}";
                        }
                    }
                    break;
                default:
                    {
                        _logger.LogInformation("接收到微信推送的消息，类型：{type}", msgType);
                        if (response != null)
                        {
                            response.Content = $"消息收到，类型：{msgType}";
                        }
                    }
                    break;
            }

            if (response != null)
            {
                return Content(client.SerializeEventToXml(response, safety: false), "application/xml");
            }
            else
            {
                return Content("success");
            }
        }
    }
}
