using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotnetSpider.Core;
using DotnetSpider.Data.Storage.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Storage
{
    public abstract class RelationalDatabaseEntityStorageBase : StorageBase
    {
        private readonly Dictionary<string, SqlStatements> _sqlStatements = new Dictionary<string, SqlStatements>();

        protected const string BoolType = "Boolean";
        protected const string DateTimeType = "DateTime";
        protected const string DateTimeOffsetType = "DateTimeOffset";
        protected const string DecimalType = "Decimal";
        protected const string DoubleType = "Double";
        protected const string FloatType = "Single";
        protected const string IntType = "Int32";
        protected const string LongType = "Int64";
        protected const string ByteType = "Byte";

        protected readonly StorageType StorageType;

        protected abstract IDbConnection CreateDbConnection(string connectString);

        protected abstract SqlStatements GenerateSqlStatements(TableMetadata tableMetadata);

        protected abstract void EnsureDatabaseAndTableCreated(IDbConnection conn, SqlStatements sqlStatements);
        
        protected RelationalDatabaseEntityStorageBase(StorageType storageType,
            string connectString = null)
        {
            ConnectString = connectString;
            StorageType = storageType;
        }

        public int RetryTimes { get; set; } = 600;

        public string ConnectString { get; }

        /// <summary>
        /// 是否使用事务操作。默认不使用。
        /// </summary>
        public bool UseTransaction { get; set; } = false;

        /// <summary>
        /// 数据库忽略大小写
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        protected override async Task<DataFlowResult> Store(DataFlowContext context)
        {
            var items = context.GetItems();
            if (items == null || items.Count == 0)
            {
                return DataFlowResult.Success;
            }

            IDbConnection conn = TryCreateDbConnection(context);

            using (conn)
            {
                foreach (var item in items)
                {
                    var tableMetadata = (TableMetadata) context[item.Key];

                    SqlStatements sqlStatements = GetSqlStatements(tableMetadata);

                    lock (this)
                    {
                        EnsureDatabaseAndTableCreated(conn, sqlStatements);
                    }

                    for (int i = 0; i < RetryTimes; ++i)
                    {
                        IDbTransaction transaction = null;
                        try
                        {
                            if (UseTransaction)
                            {
                                transaction = conn.BeginTransaction();
                            }

                            var list = (List<dynamic>) item.Value;
                            switch (StorageType)
                            {
                                case StorageType.Insert:
                                {
                                    await conn.ExecuteAsync(sqlStatements.InsertSql, list);
                                    break;
                                }                              
                                case StorageType.InsertIgnoreDuplicate:
                                {
                                    await conn.ExecuteAsync(sqlStatements.InsertIgnoreDuplicateSql, list);
                                    break;
                                }
                                case StorageType.Update:
                                {
                                    if (string.IsNullOrWhiteSpace(sqlStatements.UpdateSql))
                                    {
                                        throw new SpiderException("未能生成更新 SQL");
                                    }
                                    else
                                    {
                                        await conn.ExecuteAsync(sqlStatements.UpdateSql, list);
                                        break;
                                    }
                                }
                                case StorageType.InsertAndUpdate:
                                {
                                    await conn.ExecuteAsync(sqlStatements.InsertAndUpdateSql, list);
                                    break;
                                }
                            }
                           

                            transaction?.Commit();
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError($"尝试插入数据失败: {ex}");
                            try
                            {
                                transaction?.Rollback();
                            }
                            catch (Exception e)
                            {
                                Logger?.LogError($"数据库回滚失败: {e}");
                            }
                        }
                        finally
                        {
                            transaction?.Dispose();
                        }
                    }
                }
            }

            return DataFlowResult.Success;
        }

        protected virtual string GetNameSql(string name)
        {
            return IgnoreCase ? name.ToLowerInvariant() : name;
        }
        
        private SqlStatements GetSqlStatements(TableMetadata tableMetadata)
        {
            // 每天执行一次建表操作, 可以实现每天一个表的操作，或者按周分表可以在运行时创建新表。
            var key = tableMetadata.TypeName;
            if (tableMetadata.Schema.TablePostfix != TablePostfix.None)
            {
                key = $"{key}-{DateTime.Now:yyyyMMdd}";
            }

            lock (this)
            {
                if (!_sqlStatements.ContainsKey(key))
                {
                    _sqlStatements.Add(key, GenerateSqlStatements(tableMetadata));
                }

                return _sqlStatements[key];
            }
        }

        private IDbConnection TryCreateDbConnection(DataFlowContext context)
        {
            for (int i = 0; i < RetryTimes; ++i)
            {
                if (!string.IsNullOrWhiteSpace(ConnectString))
                {
                    var conn = TryCreateDbConnection(ConnectString);
                    if (conn != null)
                    {
                        return conn;
                    }
                }

                var options = context.Services.GetRequiredService<SpiderOptions>();
                if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    var conn = TryCreateDbConnection(options.ConnectionString);
                    if (conn != null)
                    {
                        return conn;
                    }
                }

                Logger?.LogWarning("无有效的数据库连接配置");
            }

            throw new SpiderException(
                "无有效的数据库连接配置");
        }

        private IDbConnection TryCreateDbConnection(string connectionString)
        {
            try
            {
                var conn = CreateDbConnection(connectionString);
                conn.Open();
                return conn;
            }
            catch
            {
                Logger?.LogWarning($"无法打开数据库连接: {connectionString}.");
            }

            return null;
        }
    }
}