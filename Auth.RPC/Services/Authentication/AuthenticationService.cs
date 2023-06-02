using Auth.RPC.UserJWT;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.RPC.Services.Authentication
{
    public class AuthenticationService : UserJWT.Auth.AuthBase
    {
        //依赖注入
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IDistributedCache _distributedCache;

        public AuthenticationService(ILogger<AuthenticationService> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public override Task<AuthReply> Auth(AuthRequest request, ServerCallContext context)
        {
            //验证JWT
            string? currentJWT = _distributedCache.GetString(request.UUID.ToString());
            if (currentJWT == request.JWT)
            {
                return Task.FromResult(new AuthReply
                {
                    IsValid = true
                });
            }
            else
            {
                return Task.FromResult(new AuthReply
                {
                    IsValid = false
                });
            }
        }
    }
}
