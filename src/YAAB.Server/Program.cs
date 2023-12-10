using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<YAAB.Server.Options.WeChatOptions>(builder.Configuration.GetSection(YAAB.Server.Options.WeChatOptions.WeChat));

builder.Services.AddSingleton<YAAB.Server.Services.IDistributedLockFactory, YAAB.Server.Services.DistributedLockFactory>();
builder.Services.AddSingleton<YAAB.Server.Repositories.IWeChatAccessTokenEntityRepository, YAAB.Server.Repositories.WeChatAccessTokenEntityRepository>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<YAAB.Server.Services.IWeChatApiHttpClientFactory, YAAB.Server.Services.WeChatApiHttpClientFactory>();
builder.Services.AddHostedService<YAAB.Server.Services.WeChatAccessTokenRefreshingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
