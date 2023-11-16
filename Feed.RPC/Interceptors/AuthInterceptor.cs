using Grpc.Core.Interceptors;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;

namespace Feed.RPC.Interceptors
{
    public class AuthInterceptor : Interceptor
    {
        //依赖注入
        private readonly ILogger<AuthInterceptor> _logger;
        private readonly IDistributedCache _distributedCache;

        public AuthInterceptor(ILogger<AuthInterceptor> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        // 重写 UnaryServerHandler() 方法
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
          TRequest request, ServerCallContext context,
          UnaryServerMethod<TRequest, TResponse> continuation
        )
        {
            if (context.Method == "/grpc.health.v1.Health/Check")
            {
                return continuation(request, context);
            }

            string? id = context.GetHttpContext().Request.Headers["id"];
            string? jwt = context.GetHttpContext().Request.Headers["jwt"];

            if (id == null || jwt == null)
            {
                _logger.LogWarning("Warning：Id[ {Id} ]在访问RPC服务[ {service} ]时使用了无效的JWT。", id, context.Method);
                throw new RpcException(new Status(StatusCode.PermissionDenied, "您没有权限调用该RPC服务"));
            }

            //验证JWT
            string? currentJWT = _distributedCache.GetString(id);
            if (currentJWT == jwt)
            {
                return continuation(request, context);
            }
            else
            {
                _logger.LogWarning("Warning：Id[ {Id} ]在访问RPC服务[ {service} ]时使用了无效的JWT。", id, context.Method);
                throw new RpcException(new Status(StatusCode.PermissionDenied, "您没有权限调用该RPC服务"));
            }
        }
    }
}
