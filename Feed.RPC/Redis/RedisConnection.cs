﻿using StackExchange.Redis;

namespace Feed.RPC.Redis
{
    public class RedisConnection
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionMultiplexer _connectionMultiplexer;

        public RedisConnection(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionMultiplexer = ConnectionMultiplexer.Connect(_configuration.GetConnectionString("Redis")!);
        }

        public IDatabase GetMiniAppDatabase()
        {
            return _connectionMultiplexer.GetDatabase(int.Parse(_configuration["RedisDatabase:MiniApp"]!));
        }

        public IDatabase GetFeedDatabase()
        {
            return _connectionMultiplexer.GetDatabase(int.Parse(_configuration["RedisDatabase:Feed"]!));
        }
    }
}
