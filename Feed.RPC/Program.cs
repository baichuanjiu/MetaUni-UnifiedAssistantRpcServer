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

//����gRpc��Interceptor
builder.Services.AddGrpc(
    options => options.Interceptors.Add<AuthInterceptor>()
    );

//����Serilog
var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
builder.Host.UseSerilog();

//����Consul
builder.Services.AddConsul(options => options.Address = new Uri(builder.Configuration["Consul:Address"]!));
builder.Services.AddConsulServiceRegistration(options =>
{
    options.Check = new AgentServiceCheck()
    {
        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5), //����ֹͣ���к�೤ʱ���Զ�ע���÷���
        Interval = TimeSpan.FromSeconds(60), //���������
        GRPC = builder.Configuration["Consul:IP"]! + ":" + builder.Configuration["Consul:Port"]!, //��������ַ
        Timeout = TimeSpan.FromSeconds(10), //��ʱʱ��
    };
    options.ID = builder.Configuration["Consul:ID"]!;
    options.Name = builder.Configuration["Consul:Name"]!;
    options.Address = builder.Configuration["Consul:IP"]!;
    options.Port = int.Parse(builder.Configuration["Consul:Port"]!);
});

//����Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<RedisConnection>();

//����DataCollection
builder.Services.Configure<FeedCollectionSettings>(
    builder.Configuration.GetSection("FeedCollection"));
builder.Services.Configure<MiniAppCollectionSettings>(
    builder.Configuration.GetSection("MiniAppCollection"));

builder.Services.AddSingleton<FeedService>();
builder.Services.AddSingleton<MiniAppService>();

//����TrendManager
builder.Services.AddSingleton<TrendManager>();

var app = builder.Build();

//ʹ��Serilog����������־
app.UseSerilogRequestLogging();

//����gRPC�������
app.MapGrpcService<HealthCheckService>();

//����gRPC����
app.MapGrpcService<RPCFeedService>();

app.Run();
