using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Data.Storage.Model;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotnetSpider.Data.Storage
{
    /// <summary>
    /// MongoDB 存储器
    /// TODO: 是否要考虑存储模式：插入，新的插入旧的更新，更新 ETC
    /// </summary>
    public class MongoEntityStorage : StorageBase
    {
        private readonly MongoClient _client;

        private readonly ConcurrentDictionary<string, IMongoDatabase> _cache =
            new ConcurrentDictionary<string, IMongoDatabase>();

        public MongoEntityStorage(string connectionString)
        {
            _client = new MongoClient(connectionString);
        }

        protected override async Task<DataFlowResult> Store(DataFlowContext context)
        {
            var items = context.GetItems();
            if (items == null || items.Count == 0)
            {
                return DataFlowResult.Success;
            }

            foreach (var item in items)
            {
                var tableMetadata = (TableMetadata) context[item.Key];

                if (!_cache.ContainsKey(tableMetadata.Schema.Database))
                {
                    _cache.TryAdd(tableMetadata.Schema.Database, _client.GetDatabase(tableMetadata.Schema.Database));
                }

                var db = _cache[tableMetadata.Schema.Database];
                var collection = db.GetCollection<BsonDocument>(tableMetadata.Schema.Table);

                var bsonDocs = new List<BsonDocument>();
                foreach (var data in item.Value)
                {
                    bsonDocs.Add(BsonDocument.Create(data));
                }

                await collection.InsertManyAsync(bsonDocs);
            }

            return DataFlowResult.Success;
        }
    }
}