using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.CompilerServices;
using Planto.Column;
using Planto.DatabaseImplementation;
using Planto.DatabaseImplementation.NpgSql;
using Planto.DatabaseImplementation.SqlServer;
using Planto.Exceptions;
using Planto.OptionBuilder;

[assembly: InternalsVisibleTo("Planto.Test")]

namespace Planto;

/// <summary>
/// Initializes a new instance of the Planto class
/// </summary>
public class Planto : IAsyncDisposable
{
    private readonly IDatabaseProviderHelper _dbProviderHelper;
    private readonly PlantoOptions _options;

    public Planto(string connectionString, DbmsType dbmsType, Action<PlantoOptionBuilder>? configureOptions = null)
    {
        var optionsBuilder = new PlantoOptionBuilder();
        configureOptions?.Invoke(optionsBuilder);
        var connectionHandler = new DatabaseConnectionHandler.DatabaseConnectionHandler(connectionString);
        _options = optionsBuilder.Build();
        _dbProviderHelper = dbmsType switch
        {
            DbmsType.NpgSql => new NpgSql(),
            DbmsType.MsSql => new MsSql(connectionHandler, _options.TableSchema),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + dbmsType)
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _dbProviderHelper.DisposeAsync();
    }

    /// <summary>
    /// Creates an entity in the given table
    /// </summary>
    /// <param name="tableName">table in which an entity should be created</param>
    /// <typeparam name="TCast">return type of the primary key</typeparam>
    /// <returns>PrimaryKey of the created entity</returns>
    public async Task<TCast> CreateEntity<TCast>(string tableName)
    {
        try
        {
            return await _dbProviderHelper.CreateEntity<TCast>(await CreateExecutionTree(tableName, null), _options);
        }
        catch (Exception e)
        {
            throw new PlantoDbException("Exception occured, rollback done", e);
        }
    }

    internal async Task<TableInfo> GetTableInfo(string tableName)
    {
        var tableInfo = new TableInfo(tableName);

        var columInfos = (await GetColumInfos(tableName).ConfigureAwait(false)).ToList();

        await AddColumConstraints(columInfos, tableName).ConfigureAwait(false);
        tableInfo.ColumnInfos.AddRange(columInfos);

        // Validate table
        if (tableInfo.ColumnInfos.Any(c => c.ColumnConstraints.Where(cc => cc.IsForeignKey).GroupBy(cc => cc.ColumnName)
                .Any(gc => gc.Count() > 1)))
            throw new NotSupportedException("Only tables with single foreign key constraints are supported.");
        return tableInfo;
    }

    private async Task AddColumConstraints(List<ColumnInfo> columnInfos, string tableName)
    {
        await using var dataReader = await _dbProviderHelper.GetColumnConstraints(tableName);

        while (await dataReader.ReadAsync().ConfigureAwait(false))
        {
            var columnConstraint = new ColumnConstraint();
            var properties = typeof(ColumnConstraint).GetProperties();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;
                try
                {
                    if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                }
                catch (Exception e)
                {
                    throw new PlantoDbException("Could not found column name: " + columnName, e);
                }

                var value = dataReader[columnName];
                if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    property.SetValue(columnConstraint, Convert.ToBoolean(value));
                }
                else if (columnName == "constraint_type")
                {
                    switch (value)
                    {
                        case "FOREIGN KEY":
                            property.SetValue(columnConstraint, ConstraintType.ForeignKey);
                            break;
                        case "PRIMARY KEY":
                            property.SetValue(columnConstraint, ConstraintType.PrimaryKey);
                            break;
                        case "UNIQUE":
                            property.SetValue(columnConstraint, ConstraintType.Unique);
                            break;
                        case "CHECK":
                            property.SetValue(columnConstraint, ConstraintType.Check);
                            Console.WriteLine("Warning: CHECK constraints are not yet supported");
                            break;
                        default:
                            throw new NotSupportedException($"The type {value.GetType()} is not supported");
                    }
                }
                else
                {
                    property.SetValue(columnConstraint, Convert.ToString(value));
                }
            }

            columnInfos.Single(c => c.ColumnName == columnConstraint.ColumnName).ColumnConstraints
                .Add(columnConstraint);
        }
    }

    private async Task<IEnumerable<ColumnInfo>> GetColumInfos(string tableName)
    {
        var columnInfos = new List<ColumnInfo>();

        await using var dataReader = await _dbProviderHelper.GetColumInfos(tableName);

        // read one column info at the time
        while (await dataReader.ReadAsync().ConfigureAwait(false))
        {
            var columnInfo = new ColumnInfo();
            var properties = typeof(ColumnInfo).GetProperties();

            // add db vlaues to column info
            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;
                try
                {
                    if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                }
                catch (Exception e)
                {
                    throw new PlantoDbException("Could not found column name: " + columnName, e);
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
                        _dbProviderHelper.MapToSystemType(Convert.ToString(value) ?? string.Empty));
                }
                else
                {
                    property.SetValue(columnInfo, Convert.ToString(value));
                }
            }

            columnInfos.Add(columnInfo);
        }

        return columnInfos;
    }

    internal async Task<ExecutionNode> CreateExecutionTree(string tableName, ExecutionNode? parent)
    {
        var newExecutionNode = new ExecutionNode
        {
            TableName = tableName,
            TableInfo = await GetTableInfo(tableName),
            Parent = parent
        };
        CheckCircularDependency(tableName, parent);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism ?? 3
        };

        // IsNullable false ignores fk for self referencing tables or non-hierarchical tables
        await Parallel.ForEachAsync(
            newExecutionNode.TableInfo.ColumnInfos.Where(c => c is { IsForeignKey: true, IsNullable: false }),
            parallelOptions,
            async (columnInfo, token) =>
            {
                token.ThrowIfCancellationRequested();
                // TODO Because a column can be a fk for multiple tables we should create n nodes but the ids has to be same
                newExecutionNode.Children.Add(
                    await CreateExecutionTree(
                        columnInfo.ColumnConstraints.Where(cc => cc.IsForeignKey)
                            .Select(cc => cc.ForeignTableName).FirstOrDefault()
                        ?? throw new InvalidOperationException("No Matching column_name when creating executionNode"),
                        newExecutionNode));
            }).ConfigureAwait(false);

        return newExecutionNode;
    }


    private static void CheckCircularDependency(string tableName, ExecutionNode? parent)
    {
        var prevNode = parent;
        while (prevNode is not null)
        {
            if (prevNode.TableName == tableName)
                throw new CircularDependencyException(
                    $"Circular dependency detected for table '{tableName}' where foreign key is not nullable");
            prevNode = prevNode.Parent;
        }
    }
}