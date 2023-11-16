using Auth.RPC.Interceptors;
using Auth.RPC.Services.Authentication;
using Auth.RPC.Services.HealthCheck;
using Consul;
using Consul.AspNetCore;
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

var app = builder.Build();

//ʹ��Serilog����������־
app.UseSerilogRequestLogging();

//����gRPC�������
app.MapGrpcService<HealthCheckService>();

//����gRPC����
app.MapGrpcService<AuthenticationService>();

app.Run();
