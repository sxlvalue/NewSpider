using System.Data;
using MySql.Data.MySqlClient;

namespace DotnetSpider.Data.Storage
{
    public class PostgreSqlEntityStorage : MySqlEntityStorage
    {
        public PostgreSqlEntityStorage(StorageType storageType = StorageType.InsertIgnoreDuplicate,
            string connectionString = null) : base(storageType,
            connectionString)
        {
        }
    }
}