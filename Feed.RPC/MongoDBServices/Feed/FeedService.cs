using Feed.RPC.DataCollection.Feed;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Feed.RPC.MongoDBServices.Feed
{
    public class FeedService
    {
        private readonly IMongoCollection<Models.Feed.Feed> _feedCollection;
        private readonly IMongoCollection<BsonDocument> _bsonDocumentsCollection;

        public FeedService(IOptions<FeedCollectionSettings> feedCollectionSettings)
        {
            var mongoClient = new MongoClient(
                feedCollectionSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                feedCollectionSettings.Value.DatabaseName);

            _feedCollection = mongoDatabase.GetCollection<Models.Feed.Feed>(
                feedCollectionSettings.Value.FeedCollectionName);

            _bsonDocumentsCollection = mongoDatabase.GetCollection<BsonDocument>(
                feedCollectionSettings.Value.FeedCollectionName);
        }
        public async Task CreateAsync(Models.Feed.Feed feed)
        {
            await _feedCollection.InsertOneAsync(feed);
        }
    }
}
