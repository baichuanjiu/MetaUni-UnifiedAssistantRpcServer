using Feed.RPC.Redis;
using StackExchange.Redis;

namespace Feed.RPC.TrendManager
{
    public class TrendManager
    {
        //依赖注入
        private readonly RedisConnection _redisConnection;

        public TrendManager(RedisConnection redisConnection)
        {
            _redisConnection = redisConnection;
        }

        // 传入DateTime，根据DateTime计算出对应的周期后缀
        // 计算规则为
        // ①：按两小时作为周期，从 当日 00:00 PM 起至 当日 23:59 PM 结束，一天共划分12个周期。
        // ②：XXXX年XX月XX日00:00PM - 01:59PM 后缀计算为：XXXX-XX-XX-01
        // ③：XXXX年XX月XX日02:00PM - 03:59PM 后缀计算为：XXXX-XX-XX-02
        // ④：依此类推，XXXX年XX月XX日22:00PM - 23:59PM 后缀计算为：XXXX-XX-XX-12
        private string GetCycleSuffix(DateTime dateTime)
        {
            return $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{(dateTime.Hour / 2) + 1}";
        }

        //初始化，将一条新的Feed加入热度榜，自动获取初始热度
        public void InitAction(string miniAppId,string feedId)
        {
            IDatabase feedRedis = _redisConnection.GetFeedDatabase();
            IDatabase miniAppRedis = _redisConnection.GetMiniAppDatabase();

            var batch = feedRedis.CreateBatch();
            DateTime now = DateTime.Now;
            DateTime next = now.AddHours(2);

            double? miniAppTrendValue = miniAppRedis.SortedSetScore($"TrendList{GetCycleSuffix(now)}", miniAppId);
            double trendValue;
            if (miniAppTrendValue == null)
            {
                trendValue = 0;
            }
            else 
            {
                trendValue = (double)miniAppTrendValue * 0.1;
            }

            _ = batch.SortedSetIncrementAsync($"TrendList{GetCycleSuffix(now)}", feedId, trendValue);
            _ = batch.SortedSetIncrementAsync($"TrendCycle{GetCycleSuffix(now)}", feedId, trendValue);
            _ = batch.SortedSetIncrementAsync($"TrendList{GetCycleSuffix(next)}", feedId, trendValue);

            batch.Execute();
        }
    }
}
