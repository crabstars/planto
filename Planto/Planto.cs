using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.CompilerServices;
using Planto.DatabaseImplementation;
using Planto.DatabaseImplementation.MsSql;
using Planto.DatabaseImplementation.NpgSql;
using Planto.OptionBuilder;

[assembly: InternalsVisibleTo("Planto.Test")]

namespace Planto;

public class Planto
{
    private readonly IDatabaseSchemaHelper _dbSchemaHelper;
    private readonly PlantoOptions _options;

    public Planto(string connectionString, DbmsType dbmsType, Action<PlantoOptionBuilder>? configureOptions = null)
    {
        var optionsBuilder = new PlantoOptionBuilder();
        configureOptions?.Invoke(optionsBuilder);
        _options = optionsBuilder.Build();
        _dbSchemaHelper = dbmsType switch
        {
            DbmsType.NpgSql => new NpgSql(connectionString),
            DbmsType.MsSql => new MsSql(connectionString),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + dbmsType)
        };
    }


    public async Task<object> CreateEntity(string tableName)
    {
        return await _dbSchemaHelper.Insert(await CreateExecutionTree(tableName), _options.ValueGeneration);
    }

    internal async Task<List<ColumnInfo>> GetColumnInfo(string tableName)
    {
        await using var connection = await _dbSchemaHelper.GetOpenConnection();

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
                else if (property.PropertyType == typeof(int))
                {
                    property.SetValue(columnInfo, Convert.ToInt32(value));
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

        // Check if Columns are valid
        // TODO add table name
        if (result.DistinctBy(c => c.Name).Count() != result.Count)
        {
            throw new InvalidOperationException(
                "You probably have at least one table with a foreign key which is connected to more than one table. " +
                "This feature is not yet supported.");
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
}