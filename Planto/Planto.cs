using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.CompilerServices;
using Planto.DatabaseImplementation;
using Planto.DatabaseImplementation.NpgSql;

[assembly: InternalsVisibleTo("Planto.Test")]

namespace Planto;

public class Planto
{
    private readonly string _connectionString;
    private readonly IDatabaseSchemaHelper _dbSchemaHelper;

    public Planto(string connectionString, DbmsType dbmsType)
    {
        _connectionString = connectionString;
        _dbSchemaHelper = dbmsType switch
        {
            DbmsType.NpgSql => new NpgSql(),
            DbmsType.MsSql => new MsSql(),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + dbmsType)
        };
    }

    internal string CreateInsertStatement(List<ColumnInfo> columns, string tableName)
    {
        return _dbSchemaHelper.CreateInsertStatement(columns, tableName);
    }

    internal async Task<List<ColumnInfo>> GetColumnInfo(string tableName)
    {
        await using var connection = _dbSchemaHelper.GetOpenConnection(_connectionString);

        await using var command = connection.CreateCommand();
        command.CommandText = _dbSchemaHelper.GetColumnInfoSql(tableName);
        var dataReader = await command.ExecuteReaderAsync();

        var result = new List<ColumnInfo>();

        while (await dataReader.ReadAsync())
        {
            var columnInfo = new ColumnInfo();
            var properties = typeof(ColumnInfo).GetProperties();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;

                // improve not found columns, bec some columns are only used in some dbs
                try
                {
                    if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                }
                catch (Exception e)
                {
                    continue;
                }

                var value = dataReader[columnName];
                if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    property.SetValue(columnInfo, Convert.ToBoolean(value));
                }
                else if (property.PropertyType == typeof(Type))
                {
                    property.SetValue(columnInfo,
                        _dbSchemaHelper.MapToSystemType(Convert.ToString(value) ?? string.Empty));
                }
                else
                {
                    property.SetValue(columnInfo, Convert.ToString(value));
                }
            }

            result.Add(columnInfo);
        }

        return result;
    }

    internal async Task<ExecutionNode> CreateExecutionTree(string tableName)
    {
        var newExecutionNode = new ExecutionNode
        {
            TableName = tableName,
            ColumnInfos = await GetColumnInfo(tableName)
        };

        ParallelOptions parallelOptions = new()
        {
            // TODO let user change this when creating new instance
            MaxDegreeOfParallelism = 3
        };
        await Parallel.ForEachAsync(newExecutionNode.ColumnInfos.Where(c => c.IsForeignKey), parallelOptions,
            async (columnInfo, token) =>
            {
                token.ThrowIfCancellationRequested();
                newExecutionNode.Children.Add(
                    await CreateExecutionTree(columnInfo.ForeignTableName ?? throw new InvalidOperationException()));
            });

        return newExecutionNode;
    }

    public void ExecuteExecutionTree(ExecutionNode executionNode)
    {
    }
}

public class ExecutionNode
{
    public string TableName { get; set; }
    public string InsertStatement { get; set; }
    public ConcurrentBag<ExecutionNode> Children { get; set; } = [];
    public object DbEntityId { get; set; }
    public List<ColumnInfo> ColumnInfos { get; set; }
}