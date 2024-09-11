using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace Planto;

public class Planto
{
    private string GetColumnInfoSql(string tableName)
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

    public static string CreateInsertStatement(List<ColumnInfo> columns, string tableName)
    {
        var builder = new StringBuilder();
        builder.Append($"Insert into {tableName} ");

        builder.Append('(');
        builder.AppendJoin(",", columns.Select(c => c.Name));
        builder.Append(')');
        builder.Append("Values");
        builder.Append('(');
        builder.AppendJoin(",", columns.Select(c => c.IsPrimaryKey ? "default" : CreateDefaultValue(c.DataType)));
        builder.Append(')');
        return builder.ToString();
    }

    private static object MapToSystemType(string pgType) => pgType.ToLower() switch
    {
        "integer" => typeof(int),
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

    private static object? CreateDefaultValue(Type type) => type switch
    {
        _ when type == typeof(string) => "''",
        _ when type == typeof(int) => 0,
        _ when type == typeof(long) => 0L,
        _ when type == typeof(float) => 0.0f,
        _ when type == typeof(double) => 0.0d,
        _ when type == typeof(decimal) => 0.0m,
        _ when type == typeof(bool) => false,
        _ when type == typeof(DateTime) => DateTime.MinValue,
        _ when type == typeof(DateTimeOffset) => DateTimeOffset.MinValue,
        _ when type == typeof(TimeSpan) => TimeSpan.Zero,
        _ when type == typeof(Guid) => Guid.Empty,
        _ when type == typeof(byte[]) => Array.Empty<byte>(),
        _ when type.IsValueType => Activator.CreateInstance(type),
        _ => null
    };


    public List<ColumnInfo> GetColumnInfo(string tableName, DbConnection connection)
    {
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = GetColumnInfoSql(tableName);
        var dataReader = command.ExecuteReader();

        var result = new List<ColumnInfo>();

        while (dataReader.Read())
        {
            var columnInfo = new ColumnInfo();
            var properties = typeof(ColumnInfo).GetProperties();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;
                if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                var value = dataReader[columnName];
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(columnInfo, Convert.ToBoolean(value));
                }
                else if (property.PropertyType == typeof(Type))
                {
                    property.SetValue(columnInfo, MapToSystemType(Convert.ToString(value) ?? string.Empty));
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
}

public class ColumnInfo
{
    [Column("column_name")] public string Name { get; set; }

    // public string DataType { get; set; }
    [Column("data_type")] public Type DataType { get; set; }

    [Column("is_nullable")] public bool IsNullable { get; set; }

    [Column("is_primary_key")] public bool IsPrimaryKey { get; set; }

    [Column("is_foreign_key")] public bool IsForeignKey { get; set; }

    [Column("foreign_table_name")] public string? ForeignTableName { get; set; }

    [Column("foreign_column_name")] public string? ForeignColumnName { get; set; }
}