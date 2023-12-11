using System.Text;
using YAAB.Cloudflare;
using YAAB.Cloudflare.Options;
using YAAB.Server.Options;
using YAAB.Server.Repositories;
using YAAB.Server.Services;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<WeChatOptions>(builder.Configuration.GetSection(WeChatOptions.WeChat));
builder.Services.Configure<CloudflareAiOptions>(builder.Configuration.GetSection(CloudflareAiOptions.Cloudflare));

builder.Services.AddHttpClient<CloudflareAiClient>();
builder.Services.AddSingleton<IDistributedLockFactory, DistributedLockFactory>();
builder.Services.AddSingleton<IWeChatAccessTokenEntityRepository, WeChatAccessTokenEntityRepository>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IWeChatApiHttpClientFactory, WeChatApiHttpClientFactory>();
builder.Services.AddHostedService<WeChatAccessTokenRefreshingService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
