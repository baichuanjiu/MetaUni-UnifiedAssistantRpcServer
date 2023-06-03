using Auth.RPC.Protos.Authentication;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.RPC.Services.Authentication
{
    public class AuthenticationService : Authenticate.AuthenticateBase
    {
        //依赖注入
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IDistributedCache _distributedCache;

        public AuthenticationService(ILogger<AuthenticationService> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public override Task<AuthJWTReply> AuthJWT(AuthJWTRequest request, ServerCallContext context)
        {
            //验证JWT
            string? currentJWT = _distributedCache.GetString(request.UUID.ToString());
            if (currentJWT == request.JWT)
            {
                return Task.FromResult(new AuthJWTReply
                {
                    IsValid = true
                });
            }
            else
            {
                return Task.FromResult(new AuthJWTReply
                {
                    IsValid = false
                });
            }
        }
    }
}
