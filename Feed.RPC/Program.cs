using Consul;
using Consul.AspNetCore;
using Feed.RPC.DataCollection.Feed;
using Feed.RPC.DataCollection.MiniApp;
using Feed.RPC.Interceptors;
using Feed.RPC.MongoDBServices.Feed;
using Feed.RPC.MongoDBServices.MiniApp;
using Feed.RPC.Redis;
using Feed.RPC.Services.Feed;
using Feed.RPC.Services.HealthCheck;
using Feed.RPC.TrendManager;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//配置gRpc与Interceptor
builder.Services.AddGrpc(
    options => options.Interceptors.Add<AuthInterceptor>()
    );

//配置Serilog
var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
builder.Host.UseSerilog();

//配置Consul
builder.Services.AddConsul(options => options.Address = new Uri(builder.Configuration["Consul:Address"]!));
builder.Services.AddConsulServiceRegistration(options =>
{
    options.Check = new AgentServiceCheck()
    {
        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5), //服务停止运行后多长时间自动注销该服务
        Interval = TimeSpan.FromSeconds(60), //心跳检查间隔
        GRPC = builder.Configuration["Consul:IP"]! + ":" + builder.Configuration["Consul:Port"]!, //健康检查地址
        Timeout = TimeSpan.FromSeconds(10), //超时时间
    };
    options.ID = builder.Configuration["Consul:ID"]!;
    options.Name = builder.Configuration["Consul:Name"]!;
    options.Address = builder.Configuration["Consul:IP"]!;
    options.Port = int.Parse(builder.Configuration["Consul:Port"]!);
});

//配置Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<RedisConnection>();

//配置DataCollection
builder.Services.Configure<FeedCollectionSettings>(
    builder.Configuration.GetSection("FeedCollection"));
builder.Services.Configure<MiniAppCollectionSettings>(
    builder.Configuration.GetSection("MiniAppCollection"));

builder.Services.AddSingleton<FeedService>();
builder.Services.AddSingleton<MiniAppService>();

//配置TrendManager
builder.Services.AddSingleton<TrendManager>();

var app = builder.Build();

//使用Serilog处理请求日志
app.UseSerilogRequestLogging();

//配置gRPC健康检查
app.MapGrpcService<HealthCheckService>();

//配置gRPC服务
app.MapGrpcService<RPCFeedService>();

app.Run();
