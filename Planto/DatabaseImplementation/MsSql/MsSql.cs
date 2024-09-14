using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Planto.DatabaseImplementation.DataTypes;

namespace Planto.DatabaseImplementation;

public class MsSql : IDatabaseSchemaHelper
{
    private const string LastIdSql = "SELECT SCOPE_IDENTITY() AS GeneratedID;";
    private readonly string _connectionString;

    public MsSql(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string GetColumnInfoSql(string tableName)
    {
        return $"""
                       SELECT
                           c.column_name,
                           c.data_type,
                       case 
                           when c.is_nullable = 'YES' then 1
                           else 0
                       end as is_nullable,
                       CASE
                           WHEN tc.constraint_type = 'FOREIGN KEY' THEN 1
                           ELSE 0
                       END AS is_foreign_key,
                       CASE
                           WHEN tc.constraint_type = 'PRIMARY KEY' THEN 1
                           ELSE 0
                       END AS is_primary_key,
                       CASE
                            WHEN ic.object_id IS NOT NULL THEN 1
                            ELSE 0
                       end AS is_identity,
                       ccu.table_name AS foreign_table_name,
                       ccu.column_name AS foreign_column_name
                       FROM 
                           information_schema.columns c
                       LEFT JOIN information_schema.key_column_usage kcu 
                           ON c.table_name = kcu.table_name 
                           AND c.column_name = kcu.column_name
                       LEFT JOIN information_schema.table_constraints tc 
                           ON kcu.constraint_name = tc.constraint_name
                           AND tc.table_name = c.table_name
                       LEFT JOIN information_schema.referential_constraints rc
                           ON tc.constraint_name = rc.constraint_name
                       LEFT JOIN information_schema.constraint_column_usage ccu
                           ON rc.unique_constraint_name = ccu.constraint_name
                       LEFT JOIN sys.identity_columns ic
                            ON OBJECT_ID(c.table_name) = ic.object_id AND c.column_name = ic.name
                       WHERE 
                           c.table_name = '{tableName}';
                """;
    }

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

    public object? CreateDefaultValue(Type type) => type switch
    {
        _ when type == typeof(string) => "''",
        _ when type == typeof(int) => default(int),
        _ when type == typeof(long) => default(long),
        _ when type == typeof(float) => default(float),
        _ when type == typeof(double) => default(double),
        _ when type == typeof(decimal) => default(decimal),
        _ when type == typeof(bool) => 0,
        _ when type == typeof(DateTime) => $"'{DateTime.Now:yyyy-MM-dd}'",
        _ when type == typeof(DateTimeOffset) => $"'{DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}'",
        _ when type == typeof(TimeSpan) => $"'{DateTime.Now:hh\\:mm\\:ss}'",
        _ when type == typeof(Guid) => $"'{Guid.Empty}'",
        _ when type == typeof(byte[]) => "0x",
        _ when type == typeof(HierarchyId) => new HierarchyId().GetDefaultValue(),
        _ when type == typeof(Geography) => new Geography().GetDefaultValue(),
        _ when type == typeof(Geometry) => new Geometry().GetDefaultValue(),
        _ when type.IsValueType => Activator.CreateInstance(type),
        _ => null
    };

    public async Task<object> Insert(ExecutionNode executionNode)
    {
        var columns = executionNode.ColumnInfos.Where(c => !c.IsIdentity.HasValue || !c.IsIdentity.Value).ToList();
        var builder = new StringBuilder();
        builder.Append($"Insert into {executionNode.TableName} ");

        if (columns.All(c => c.IsPrimaryKey))
        {
            builder.Append("DEFAULT VALUES;");
        }
        else
        {
            builder.Append('(');
            builder.AppendJoin(",", columns.Select(c => c.Name));
            builder.Append(')');
            builder.Append("Values");
            builder.Append('(');
            var values = new List<object?>();
            foreach (var c in columns)
            {
                if (c.IsForeignKey)
                {
                    var foreignKey =
                        await Insert(executionNode.Children.Single(child => child.TableName == c.ForeignTableName));
                    values.Add(foreignKey);
                }
                else
                {
                    values.Add(CreateDefaultValue(c.DataType));
                }
            }

            builder.AppendJoin(",", values);
            builder.Append(");");
        }

        builder.Append(LastIdSql);

        var insertStatement = builder.ToString();
        await using var connection = await GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = insertStatement;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(reader.GetOrdinal("GeneratedID")))
                {
                    var value = reader["GeneratedID"];
                    await reader.CloseAsync();
                    return value ?? throw new InvalidOperationException("GeneratedID was not found");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }

        throw new InvalidOperationException("Could not get GeneratedID. See logs");
    }

    public async Task<DbConnection> GetOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}