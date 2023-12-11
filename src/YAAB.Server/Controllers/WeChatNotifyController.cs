using Microsoft.AspNetCore.Mvc;
using SKIT.FlurlHttpClient.Wechat.Api;
using SKIT.FlurlHttpClient.Wechat.Api.Events;
using SKIT.FlurlHttpClient.Wechat.Api.Models;
using System.Text;
using YAAB.Cloudflare;

namespace YAAB.Server.Controllers
{
    [ApiController]
    [Route("api/wechat")]
    public class WeChatNotifyController(
        ILogger<WeChatNotifyController> logger,
        Services.IWeChatApiHttpClientFactory wechatApiHttpClientFactory,
        Repositories.IWeChatAccessTokenEntityRepository wechatAccessTokenEntityRepository,
        CloudflareAiClient cloudflareAiClient) : ControllerBase
    {
        private readonly ILogger<WeChatNotifyController> _logger = logger;
        private readonly Services.IWeChatApiHttpClientFactory _wechatApiHttpClientFactory = wechatApiHttpClientFactory;
        private readonly Repositories.IWeChatAccessTokenEntityRepository _wechatAccessTokenEntityRepository = wechatAccessTokenEntityRepository;
        private readonly CloudflareAiClient _cloudflareAiClient = cloudflareAiClient;

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
                            response.Content = HandleTextMessage(client, _cloudflareAiClient, response.ToUserName!, eventModel.Content);
                        }
                    }
                    break;
                case "VOICE":
                    {
                        var eventModel = client.DeserializeEventFromXml<VoiceMessageEvent>(content);
                        _logger.LogInformation("接收到微信推送的语音消息，语音ID：{mediaId}", eventModel.MediaId);
                    }
                    break;
                default:
                    {
                        _logger.LogInformation("接收到微信推送的消息，类型：{type}", msgType);
                    }
                    break;
            }

            if (response != null && !string.IsNullOrEmpty(response.Content))
            {
                return Content(client.SerializeEventToXml(response, safety: false), "application/xml");
            }
            else
            {
                return Content("success");
            }
        }

        private string HandleTextMessage(WechatApiClient wechat, CloudflareAiClient cloudflareAi, string user, string text)
        {
            if (string.IsNullOrEmpty(user))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(text)) 
            {
                var tokens = text.Split([' ', '　'], 2);
                switch (tokens[0])
                {
                    case "？":
                    case "?":
                    case "帮助":
                    case "说明":
                        return "指令说明（【】内为指令）：\r\n【画图　提示词】基于文本生成图片。\r\n【sd　提示词】基于文本生成图片。\r\n【帮助】显示此说明。\r\n【？】显示此说明。";
                    case "画图":
                    case "sd":
                        {
                            if (tokens.Length < 2 || string.IsNullOrEmpty(tokens[1]))
                            {
                                return "未检测到提示词，请重新输入指令。";
                            }
                            _ = TextToImage(wechat, cloudflareAi, user, tokens[1]);
                            return $"已提交图片生成请求，请稍候。\r\n您的提示词为：\r\n{tokens[1]}";
                        }
                }
            }

            return "输入\"？\"或者\"帮助\"获取说明。";
        }

        private async Task TextToImage(WechatApiClient wechat, CloudflareAiClient cloudflareAi, string user, string prompt)
        {
            var response = await cloudflareAi.TextToImage(prompt);
            if (response.Image.Length > 0)
            {
                var imageId = await UploadImage(wechat, response.Image);
                if (!string.IsNullOrEmpty(imageId))
                {
                    await SendImage(wechat, user, imageId);
                }
                else
                {
                    await SendText(wechat, user, "图片上传失败。");
                }
            }
            else
            {
                await SendText(wechat, user, $"图片生成失败，返回消息为：\r\n{response.Message}");
            }
        }

        private async Task<string> UploadImage(WechatApiClient wechat, byte[] image)
        {
            try
            {
                var entity = _wechatAccessTokenEntityRepository.FirstOrDefault(e => e.AppId == wechat.Credentials.AppId);
                if (entity == null)
                {
                    _logger.LogError("读取 Access Token 失败。AppId：{appId}。", wechat.Credentials.AppId);
                    return string.Empty;
                }

                var request = new CgibinMediaUploadRequest { AccessToken = entity.AccessToken, Type = "image", FileBytes = image };
                CgibinMediaUploadResponse response = await wechat.ExecuteCgibinMediaUploadAsync(request);
                if (response.IsSuccessful())
                {
                    return response.MediaId;
                }
                _logger.LogError("上传图片文件时发生错误：\"{code}\", \"{error}\"。", response.ErrorCode, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传图片文件时发生错误。");
            }
            return string.Empty;
        }

        private async Task SendImage(WechatApiClient wechat, string user, string imageId)
        {
            try
            {
                var entity = _wechatAccessTokenEntityRepository.FirstOrDefault(e => e.AppId == wechat.Credentials.AppId);
                if (entity == null)
                {
                    _logger.LogError("读取 Access Token 失败。AppId：{appId}。", wechat.Credentials.AppId);
                    return;
                }

                var request = new CgibinMessageCustomSendRequest 
                {
                    AccessToken = entity.AccessToken,
                    MessageType = "image", 
                    ToUserOpenId = user, 
                    MessageContentForImage = new CgibinMessageCustomSendRequest.Types.ImageMessage { MediaId = imageId } 
                };
                CgibinMessageCustomSendResponse response = await wechat.ExecuteCgibinMessageCustomSendAsync(request);
                if (response.IsSuccessful())
                {
                    return;
                }
                _logger.LogError("发送图片消息时发生错误：\"{code}\", \"{error}\"。", response.ErrorCode, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送图片消息时发生错误。");
            }
        }

        private async Task SendText(WechatApiClient wechat, string user, string text)
        {
            try
            {
                var entity = _wechatAccessTokenEntityRepository.FirstOrDefault(e => e.AppId == wechat.Credentials.AppId);
                if (entity == null) 
                {
                    _logger.LogError("读取 Access Token 失败。AppId：{appId}。", wechat.Credentials.AppId);
                    return;
                }

                var request = new CgibinMessageCustomSendRequest 
                {
                    AccessToken = entity.AccessToken,
                    MessageType = "text", 
                    ToUserOpenId = user, 
                    MessageContentForText = new CgibinMessageCustomSendRequest.Types.TextMessage { Content = text } 
                };
                CgibinMessageCustomSendResponse response = await wechat.ExecuteCgibinMessageCustomSendAsync(request);
                if (response.IsSuccessful())
                {
                    return;
                }
                _logger.LogError("发送文本消息时发生错误：\"{code}\", \"{error}\"。", response.ErrorCode, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送文本消息时发生错误。");
            }
        }
    }
}
