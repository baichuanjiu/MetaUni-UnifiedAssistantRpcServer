using Consul;
using Consul.AspNetCore;
using Message.RPC.DataContext.Message;
using Message.RPC.DataContext.User;
using Message.RPC.Interceptors;
using Message.RPC.RabbitMQ;
using Message.RPC.Redis;
using Message.RPC.Services.HealthCheck;
using Message.RPC.Services.SystemMessage;
using Microsoft.EntityFrameworkCore;
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

//配置DbContext
builder.Services.AddDbContext<MessageContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("MessageContext")));

builder.Services.AddDbContext<UserContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("UserContext")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//配置Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<RedisConnection>();

//配置消息队列生产者（消息发布者）
builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

var app = builder.Build();

//使用Serilog处理请求日志
app.UseSerilogRequestLogging();

//配置gRPC健康检查
app.MapGrpcService<HealthCheckService>();

//配置gRPC服务
app.MapGrpcService<GeneralNotificationService>();
app.MapGrpcService<ChatRequestService>();

app.Run();
