using System.Data.Common;
using System.Text;
using Planto.DatabaseConnectionHandler;
using Planto.DatabaseImplementation.SqlServer.DataTypes;
using Planto.OptionBuilder;
using Planto.Reflection;

namespace Planto.DatabaseImplementation.SqlServer;

internal class MsSql(IDatabaseConnectionHandler connectionHandler, string? optionsTableSchema) : IDatabaseProviderHelper
{
    private const string LastIdIntSql = "SELECT CAST(SCOPE_IDENTITY() AS INT) AS GeneratedID;";
    private const string LastIdDecimalSql = "SELECT SCOPE_IDENTITY() AS GeneratedID;";
    private readonly MsSqlQueries _queries = new(optionsTableSchema);

    public Type MapToSystemType(string sqlType)
    {
        var sqlToCSharpMap = new Dictionary<string, Type>
        {
            { "int", typeof(int) },
            { "tinyint", typeof(byte) },
            { "smallint", typeof(short) },
            { "bigint", typeof(long) },
            { "decimal", typeof(decimal) },
            { "numeric", typeof(decimal) },
            { "float", typeof(double) },
            { "real", typeof(float) },
            { "money", typeof(decimal) },
            { "smallmoney", typeof(decimal) },
            { "char", typeof(string) },
            { "varchar", typeof(string) },
            { "text", typeof(string) },
            { "nchar", typeof(string) },
            { "nvarchar", typeof(string) },
            { "ntext", typeof(string) },
            { "date", typeof(DateTime) },
            { "datetime", typeof(DateTime) },
            { "datetime2", typeof(DateTime) },
            { "smalldatetime", typeof(DateTime) },
            { "time", typeof(TimeSpan) },
            { "datetimeoffset", typeof(DateTimeOffset) },
            { "binary", typeof(byte[]) },
            { "varbinary", typeof(byte[]) },
            { "image", typeof(byte[]) },
            { "bit", typeof(bool) },
            { "uniqueidentifier", typeof(Guid) },
            { "xml", typeof(string) },
            { "json", typeof(string) },
            { "hierarchyid", typeof(HierarchyId) },
            { "geography", typeof(Geography) },
            { "geometry", typeof(Geometry) }
        };

        if (sqlToCSharpMap.ContainsKey(sqlType.ToLower()))
        {
            return sqlToCSharpMap[sqlType.ToLower()];
        }

        throw new ArgumentException($"SQL Type '{sqlType}' not recognized.");
    }

    public async Task<DbDataReader> GetColumnConstraints(string tableName)
    {
        var connection = await connectionHandler.GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.GetColumnConstraintsSql(tableName);
        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }

    public async Task<DbDataReader> GetColumInfos(string tableName)
    {
        var connection = await connectionHandler.GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.GetColumnInfoSql(tableName);
        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }

    public async Task<TCast> CreateEntity<TCast>(object? data, ExecutionNode executionNode, PlantoOptions plantoOptions)
    {
        try
        {
            await connectionHandler.StartTransaction();
            var id = (TCast)await Insert(data, executionNode, plantoOptions.ValueGeneration);
            await connectionHandler.CommitTransaction();
            return id;
        }
        catch (Exception)
        {
            await connectionHandler.RollbackTransaction();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await connectionHandler.DisposeAsync();
    }

    private async Task<object> Insert(object? data, ExecutionNode executionNode, ValueGeneration valueGeneration)
    {
        var connection = await connectionHandler.GetOpenConnection();

        var columns = executionNode.TableInfo.ColumnInfos
            .Where(c => (!c.IsIdentity.HasValue || !c.IsIdentity.Value)
                        && (!c.IsNullable || c.ColumnConstraints.Any(cc => cc.IsUnique)))
            .ToList();
        var builder = new StringBuilder();
        builder.Append($"Insert into {executionNode.TableName} ");

        var matchesCurrentTable = AttributeHelper.CustomDataMatchesCurrentTable(data, executionNode.TableName);
        object? pk = null;
        if (columns.All(c =>
                c is { IsPrimaryKey: true, IsIdentity: not null } && c.IsIdentity.Value))
        {
            builder.Append("DEFAULT VALUES;");
        }
        else
        {
            builder.Append('(');
            builder.AppendJoin(",", columns.Select(c => c.ColumnName));
            builder.Append(')');
            builder.Append("Values");
            builder.Append('(');
            var values = new List<object?>();
            foreach (var c in columns)
            {
                if (c is { IsForeignKey: true, IsNullable: false })
                {
                    // We could probably just check for ForeignTableName, but right now it improves my sanity
                    var foreignKey =
                        await Insert(data, executionNode.Children.Single(child =>
                            c.ColumnConstraints.Where(cc => cc.IsForeignKey)
                                .Any(cc => cc.ForeignTableName == child.TableName
                                           && child.TableInfo.ColumnInfos
                                               .Any(ci => ci.ColumnName == cc.ForeignColumnName))
                        ), valueGeneration);
                    values.Add(foreignKey);
                }
                else
                {
                    var value = matchesCurrentTable
                        ? SqlValueGeneration.MapValueForMsSql(AttributeHelper.GetValueToCustomData(data, c.ColumnName))
                        : SqlValueGeneration.CreateValueForMsSql(c.DataType, valueGeneration, c.MaxCharLen);
                    values.Add(value);
                    if (c.IsPrimaryKey)
                        pk = value;
                }
            }

            builder.AppendJoin(",", values);
            builder.Append(");");
        }

        if (pk is null)
        {
            builder.Append(executionNode.TableInfo.ColumnInfos.Any(c => c.DataType != typeof(int)
                                                                        && c.IsPrimaryKey)
                ? LastIdDecimalSql
                : LastIdIntSql);
        }

        var insertStatement = builder.ToString();
        executionNode.InsertStatement = insertStatement;
        await using var command = connection.CreateCommand();
        command.Transaction = connectionHandler.GetDbTransaction();
        command.CommandText = insertStatement;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            if (pk != null)
            {
                await reader.CloseAsync();
                return pk;
            }

            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(reader.GetOrdinal("GeneratedID")))
                {
                    pk = reader["GeneratedID"];
                    await reader.CloseAsync();
                    executionNode.DbEntityId = pk;
                    return pk ?? throw new InvalidOperationException("GeneratedID was not found");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        throw new InvalidOperationException("Could not get GeneratedID. See logs");
    }
}