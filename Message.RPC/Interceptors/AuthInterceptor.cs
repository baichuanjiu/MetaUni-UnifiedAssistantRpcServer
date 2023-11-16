using Grpc.Core;
using Grpc.Core.Interceptors;
using Message.RPC.DataContext.Message;
using Microsoft.Extensions.Caching.Distributed;

namespace Message.RPC.Interceptors
{
    public class AuthInterceptor : Interceptor
    {
        //依赖注入
        private readonly ILogger<AuthInterceptor> _logger;
        private readonly MessageContext _messageContext;
        private readonly IDistributedCache _distributedCache;

        public AuthInterceptor(ILogger<AuthInterceptor> logger, MessageContext messageContext, IDistributedCache distributedCache)
        {
            _logger = logger;
            _messageContext = messageContext;
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
            if (currentJWT != jwt)
            {
                _logger.LogWarning("Warning：Id[ {Id} ]在访问RPC服务[ {service} ]时使用了无效的JWT。", id, context.Method);
                throw new RpcException(new Status(StatusCode.PermissionDenied, "您没有权限调用该RPC服务"));
            }

            //未注册SystemPromotion的禁止调用RPC
            if (_messageContext.SystemPromotions.Select(p => new { p.MiniAppId }).SingleOrDefault(p => p.MiniAppId == id) == null) 
            {
                _logger.LogWarning("Warning：未注册SystemPromotion的MiniApp[ {Id} ]尝试访问消息RPC服务[ {service} ]。", id, context.Method);
                throw new RpcException(new Status(StatusCode.PermissionDenied, "您尚未注册SystemPromotion"));
            }
            return continuation(request, context);
        }
    }
}
