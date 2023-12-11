using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using YAAB.Cloudflare.Models;
using YAAB.Cloudflare.Options;

namespace YAAB.Cloudflare
{
    public class CloudflareAiClient
    {
        private readonly HttpClient _httpClient;
        private readonly CloudflareAiOptions _options;
        private readonly ILogger<CloudflareAiClient> _logger;

        public CloudflareAiClient(HttpClient httpClient, IOptions<CloudflareAiOptions> options, ILogger<CloudflareAiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.BaseAddress = new Uri("https://api.cloudflare.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        }

        public async Task<TextToImageResponse> TextToImage(string prompt)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"client/v4/accounts/{_options.AccountId}/ai/run/@cf/stabilityai/stable-diffusion-xl-base-1.0", new TextToImageRequest { Prompt = prompt });
                response.EnsureSuccessStatusCode();
                var type = response.Content.Headers.ContentType?.MediaType;
                if (!string.IsNullOrEmpty(type) && type == "image/png")
                {
                    return new TextToImageResponse { Image = await response.Content.ReadAsByteArrayAsync() };
                }

                return new TextToImageResponse { Message = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求 Cloudfalre AI Text-To-Image 接口时发生错误。");
                return new TextToImageResponse { Message = ex.Message };
            }
        }
    }
}
