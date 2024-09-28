using System.Data.Common;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation.NpgSql;

public class NpgSql : IDatabaseProviderHelper
{
    public Task<DbDataReader> GetColumInfos(string tableName)
    {
        throw new NotImplementedException();
    }

    public Task<DbDataReader> GetColumnConstraints(string tableName)
    {
        throw new NotImplementedException();
    }

    public Task<TCast> CreateEntity<TCast>(ExecutionNode executionNode, PlantoOptions plantoOptions)
    {
        throw new NotImplementedException();
    }

    public Type MapToSystemType(string pgType) => pgType.ToLower() switch
    {
        "integer" => typeof(int),
        "int" => typeof(int),
        "bigint" => typeof(long),
        "real" => typeof(float),
        "double precision" => typeof(double),
        "numeric" or "money" => typeof(decimal),
        "text" or "character varying" or "varchar" => typeof(string),
        "boolean" => typeof(bool),
        "date" => typeof(DateTime),
        "timestamp" or "timestamp without time zone" => typeof(DateTime),
        "timestamp with time zone" => typeof(DateTimeOffset),
        "time" or "time without time zone" => typeof(TimeSpan),
        "bytea" => typeof(byte[]),
        "uuid" => typeof(Guid),
        _ => typeof(object)
    };

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public string GetColumnInfoSql(string tableName)
    {
        return $"""
                       SELECT
                           c.column_name,
                           c.data_type,
                           c.character_maximum_length,
                       case 
                           when c.is_nullable = 'YES' then true
                           else false
                       end as is_nullable,
                       CASE
                           WHEN tc.constraint_type = 'FOREIGN KEY' THEN true
                           ELSE false
                       END AS is_foreign_key,
                       CASE
                           WHEN tc.constraint_type = 'PRIMARY KEY' THEN true
                           ELSE false
                       END AS is_primary_key,
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
                       WHERE 
                           c.table_name = '{tableName}';
                """;
    }

    public string GetColumnConstraintsSql(string tableName)
    {
        throw new NotImplementedException();
    }


    public async Task<object> Insert(ExecutionNode executionNode, ValueGeneration valueGeneration)
    {
        // var builder = new StringBuilder();
        // var columns = executionNode.ColumnInfos;
        // builder.Append($"Insert into {executionNode.TableName} ");
        //
        // builder.Append('(');
        // builder.AppendJoin(",", columns.Select(c => c.Name));
        // builder.Append(')');
        // builder.Append("Values");
        // builder.Append('(');
        // // builder.AppendJoin(",",
        // //     columns.Select(c => c.IsPrimaryKey ? "default" : CreateDefaultValue(c.DataType)));
        // builder.Append(')');
        // return builder.ToString();
        throw new NotImplementedException();
    }


    public async Task<DbConnection> GetOpenConnection()
    {
        throw new NotImplementedException();
    }

    public Task CloseConnection()
    {
        throw new NotImplementedException();
    }

    public Task StartTransaction()
    {
        throw new NotImplementedException();
    }

    public Task CommitTransaction()
    {
        throw new NotImplementedException();
    }

    public Task RollbackTransaction()
    {
        throw new NotImplementedException();
    }

    public DbTransaction GetDbTransaction()
    {
        throw new NotImplementedException();
    }

    public object? CreateDefaultValue(Type type) => type switch
    {
        _ when type == typeof(string) => "''",
        _ when type == typeof(int) => default(int),
        _ when type == typeof(long) => default(long),
        _ when type == typeof(float) => default(float),
        _ when type == typeof(double) => default(double),
        _ when type == typeof(decimal) => default(decimal),
        _ when type == typeof(bool) => default(bool),
        _ when type == typeof(DateTime) => default(DateTime),
        _ when type == typeof(DateTimeOffset) => default(DateTimeOffset),
        _ when type == typeof(TimeSpan) => default(TimeSpan),
        _ when type == typeof(Guid) => Guid.Empty,
        _ when type == typeof(byte[]) => Array.Empty<byte>(),
        _ when type.IsValueType => Activator.CreateInstance(type),
        _ => null
    };

    public object? CreateRandomValue(Type type, int size)
    {
        throw new NotImplementedException();
    }
}