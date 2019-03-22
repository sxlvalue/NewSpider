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

        protected abstract IDbConnection CreateDbConnection(string connectString);

        protected abstract SqlStatements GenerateSqlStatements(TableMetadata tableMetadata);

        protected RelationalDatabaseEntityStorageBase(string connectString = null)
        {
            ConnectString = connectString;
        }

        protected override async Task<DataFlowResult> Store(DataFlowContext context)
        {
            var items = context.GetItems();
            if (items == null || items.Count == 0)
            {
                return DataFlowResult.Success;
            }

            IDbConnection conn = null;
            for (int i = 0; i < RetryTimes; ++i)
            {
                conn = TryCreateDbConnection(context);
                if (conn == null)
                {
                    Logger?.LogWarning("无有效的数据库连接配置");
                }
                else
                {
                    break;
                }
            }

            if (conn == null)
            {
                throw new SpiderException(
                    "无有效的数据库连接配置");
            }

            using (conn)
            {
                foreach (var item in items)
                {
                    var tableMetadata = (TableMetadata) context[item.Key];

                    // 每天执行一次建表操作, 可以实现每天一个表的操作，或者按周分表可以在运行时创建新表。
                    var key = tableMetadata.Schema.TablePostfix != TablePostfix.None
                        ? $"{tableMetadata.TypeName}-{DateTime.Now:yyyyMMdd}"
                        : tableMetadata.TypeName;
                    SqlStatements sqlStatements;
                    lock (this)
                    {
                        if (_sqlStatements.ContainsKey(key))
                        {
                            sqlStatements = _sqlStatements[key];
                        }
                        else
                        {
                            sqlStatements = GenerateSqlStatements(tableMetadata);
                            EnsureDatabaseAndTableCreated(conn, sqlStatements);
                            _sqlStatements.Add(key, sqlStatements);
                        }
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

                            var list = (List<dynamic>) item.Value ;
                            await conn.ExecuteAsync(sqlStatements.InsertSql, list);

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

        protected abstract void EnsureDatabaseAndTableCreated(IDbConnection conn, SqlStatements sqlStatements);

        private IDbConnection TryCreateDbConnection(DataFlowContext context)
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

            return null;
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