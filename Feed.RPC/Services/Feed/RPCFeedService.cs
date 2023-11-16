using Feed.RPC.MongoDBServices.Feed;
using Feed.RPC.MongoDBServices.MiniApp;
using Feed.RPC.Protos.Feed;
using Feed.RPC.ReusableClass;
using Grpc.Core;
using System.Text.Json;

namespace Feed.RPC.Services.Feed
{
    public class RPCFeedService : AddFeed.AddFeedBase
    {
        //依赖注入
        private readonly ILogger<RPCFeedService> _logger;
        private readonly FeedService _feedService;
        private readonly MiniAppService _miniAppService;
        private readonly TrendManager.TrendManager _trendManager;

        public RPCFeedService(ILogger<RPCFeedService> logger, FeedService feedService, MiniAppService miniAppService, TrendManager.TrendManager trendManager)
        {
            _logger = logger;
            _feedService = feedService;
            _miniAppService = miniAppService;
            _trendManager = trendManager;
        }

        public override async Task<GeneralReply> AddFeedSingle(AddFeedSingleRequest request, ServerCallContext context) 
        {
            string? id = context.GetHttpContext().Request.Headers["id"];
            BriefMiniAppInfo briefMiniAppInfo = JsonSerializer.Deserialize<BriefMiniAppInfo>(_miniAppService.GetInformationById(id!))!;
            Models.Feed.Feed feed = new(null,request.Cover == null?null: new(request.Cover.Type, request.Cover.URL, request.Cover.AspectRatio, request.Cover.PreviewImage, request.Cover.TimeTotal), request.PreviewContent,briefMiniAppInfo,request.Title,request.Description,request.OpenPageUrl);
            await _feedService.CreateAsync(feed);
            _trendManager.InitAction(id!,feed.Id!);
            GeneralReply addFeedSucceed = new()
            {
                Code = 0,
                Message = "推流成功",
            };
            return addFeedSucceed;
        }
    }
}
