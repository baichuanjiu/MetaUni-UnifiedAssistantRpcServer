using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using User.RPC.BriefUserInfo;
using User.RPC.DataContext.User;

namespace User.RPC.Services.GetBriefUserInfo
{
    public class BriefUserInformation
    {
        public BriefUserInformation(int UUID, string avatar, string nickname, DateTime updatedTime)
        {
            this.UUID = UUID;
            Avatar = avatar;
            Nickname = nickname;
            UpdatedTime = updatedTime;
        }

        public int UUID { get; set; }
        public string Avatar { get; set; }
        public string Nickname { get; set; }
        public DateTime UpdatedTime { get; set; }
    }

    public class GetBriefUserInfoService : BriefUserInfo.Get.GetBase
    {
        //依赖注入
        private readonly ILogger<GetBriefUserInfoService> _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly UserContext _userContext;

        public GetBriefUserInfoService(ILogger<GetBriefUserInfoService> logger, IDistributedCache distributedCache, UserContext userContext)
        {
            _logger = logger;
            _distributedCache = distributedCache;
            _userContext = userContext;
        }

        public override Task<GetReply> Get(GetRequest request, ServerCallContext context)
        {
            BriefUserInformation briefUserInformation;
            //优先查找Redis缓存中的数据
            string? briefUserInformationJson = _distributedCache.GetString(request.UUID.ToString() + "BriefUserInfo");
            if (briefUserInformationJson != null)
            {
                briefUserInformation = JsonSerializer.Deserialize<BriefUserInformation>(briefUserInformationJson)!;
            }
            else
            {
                //查找数据库
                var targetInformation = _userContext.UserProfiles.Select(profile => new { profile.UUID, profile.Avatar, profile.Nickname, profile.UpdatedTime }).FirstOrDefault(profile => profile.UUID == request.UUID);
                if (targetInformation == null)
                {
                    return Task.FromResult(new GetReply
                    {
                        IsValid = false
                    });
                }

                briefUserInformation = new(targetInformation.UUID, targetInformation.Avatar, targetInformation.Nickname, targetInformation.UpdatedTime);

                //往Redis里做缓存
                //设置缓存在Redis中的过期时间
                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(3));
                options.SetSlidingExpiration(TimeSpan.FromSeconds(60));
                //将数据存入Redis
                _distributedCache.SetStringAsync(request.UUID.ToString() + "BriefUserInfo", JsonSerializer.Serialize(briefUserInformation), options);
            }

            return Task.FromResult(new GetReply
            {
                IsValid = true,
                UUID = briefUserInformation.UUID,
                Avatar = briefUserInformation.Avatar,
                Nickname = briefUserInformation.Nickname,
                UpdatedTime = briefUserInformation.UpdatedTime.ToString()
            });
        }
    }
}
