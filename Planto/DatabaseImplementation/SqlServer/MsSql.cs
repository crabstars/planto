using System.Data.Common;
using System.Text;
using Planto.Column;
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

    public async Task<DbDataReader> GetColumnChecks(string tableName)
    {
        var connection = await connectionHandler.GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.GetColumnChecksSql(tableName);
        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }

    public async Task<DbDataReader> GetColumInfos(string tableName)
    {
        var connection = await connectionHandler.GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.GetColumnInfoSql(tableName);
        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }

    public async Task<TCast> CreateEntity<TCast>(ExecutionNode executionNode, PlantoOptions plantoOptions,
        params object?[] data)
    {
        try
        {
            await connectionHandler.StartTransaction();
            var id = (TCast)await Insert(executionNode, plantoOptions.ValueGeneration, data);
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

    private async Task<object> Insert(ExecutionNode executionNode, ValueGeneration valueGeneration,
        params object?[] data)
    {
        var connection = await connectionHandler.GetOpenConnection();
        var matchingUserData = AttributeHelper.GetCustomDataMatchesCurrentTable(executionNode.TableName, data);
        object? pk = null;
        var columns = GetColumnsForValueGeneration(executionNode);

        var builder = new StringBuilder();
        builder.Append("Insert into ");
        if (optionsTableSchema is not null)
            builder.Append($"{optionsTableSchema}.");
        builder.Append($"{executionNode.TableName} ");

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
                    values.Add(await GetForeignKey(executionNode, valueGeneration, data, c));
                }
                else
                {
                    var value = GetColumnValue(valueGeneration, matchingUserData, c);
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
            builder.Append(executionNode.TableInfo.ColumnInfos
                .Any(c => c.DataType != typeof(int) && c.IsPrimaryKey)
                ? LastIdDecimalSql
                : LastIdIntSql);
        }

        return await ExecuteInsert(executionNode, builder, connection, pk);
    }

    private static object? GetColumnValue(ValueGeneration valueGeneration, object? matchingUserData, ColumnInfo c)
    {
        var value = matchingUserData is not null
            ? SqlValueGeneration.MapValueForMsSql(AttributeHelper.GetValueToCustomData(matchingUserData, c.ColumnName))
            : null;
        value ??= c.ColumnChecks.SelectMany(cc => cc.ParsedColumnCheck?.GetAllValues() ?? [])
            .FirstOrDefault(v => v is not null && (string)v != "NULL");
        if (value is null || (value as string == "NULL" && !c.IsNullable))
        {
            value = SqlValueGeneration.CreateValueForMsSql(c.DataType, valueGeneration, c.MaxCharLen);
        }

        return value;
    }

    private async Task<object> GetForeignKey(ExecutionNode executionNode, ValueGeneration valueGeneration,
        object?[] data,
        ColumnInfo c)
    {
        // We could probably just check for ForeignTableName, but right now it improves my sanity
        var foreignKey =
            await Insert(executionNode.Children.Single(child =>
                c.ColumnConstraints.Where(cc => cc.IsForeignKey)
                    .Any(cc => cc.ForeignTableName == child.TableName
                               && child.TableInfo.ColumnInfos
                                   .Any(ci => ci.ColumnName == cc.ForeignColumnName))
            ), valueGeneration, data);
        return foreignKey;
    }

    private static List<ColumnInfo> GetColumnsForValueGeneration(ExecutionNode executionNode)
    {
        return executionNode.TableInfo.ColumnInfos
            .Where(c => !c.IsComputed && (!c.IsIdentity.HasValue || !c.IsIdentity.Value)
                                      && (!c.IsNullable || c.ColumnConstraints.Any(cc => cc.IsUnique))).ToList();
    }

    private async Task<object> ExecuteInsert(ExecutionNode executionNode, StringBuilder builder,
        DbConnection connection,
        object? pk)
    {
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