using Feed.RPC.DataCollection.MiniApp;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Feed.RPC.MongoDBServices.MiniApp
{
    public class MiniAppService
    {
        private readonly IMongoCollection<BsonDocument> _bsonDocumentsCollection;

        public MiniAppService(IOptions<MiniAppCollectionSettings> miniAppCollectionSettings)
        {
            var mongoClient = new MongoClient(
                miniAppCollectionSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                miniAppCollectionSettings.Value.DatabaseName);

            _bsonDocumentsCollection = mongoDatabase.GetCollection<BsonDocument>(
                miniAppCollectionSettings.Value.MiniAppCollectionName);
        }

        public string GetInformationById(string id)
        {
            return _bsonDocumentsCollection.Find(app => app["_id"] == new ObjectId(id))
                .Project(app => new { Id = app["_id"].ToString(), Type = app["Type"], Name = app["Name"], Avatar = app["Avatar"]})
                .FirstOrDefault()
                .ToJson();
        }
    }
}
