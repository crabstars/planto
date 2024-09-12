namespace Planto.DatabaseImplementation;

public class NpgSql : IDatabaseSchemaHelper
{
    public string GetColumnInfoSql(string tableName)
    {
        return $"""
                       SELECT
                           c.column_name,
                           c.data_type,
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
}