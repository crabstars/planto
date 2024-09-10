using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Npgsql;

namespace Planto;

public class Planto
{

    private string GetColumnInfoSql = """
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
                                          c.table_name = 'orders';
                                      """;
     public List<ColumnInfo> GetColumnInfo(string connectionString, string tableName)
     {
            using var connection = new NpgsqlConnection(connectionString);
            connection.ConnectionString = connectionString;
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = GetColumnInfoSql;
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

     public string CreateInsertStatement(List<ColumnInfo> columns, string tableName)
     {
         var builder = new StringBuilder();
         builder.Append($"Insert into {tableName} ");
         
         builder.Append('(');
         builder.AppendJoin(",", columns.Select(c => c.Name));
         builder.Append(')');
         builder.Append("Values");
         builder.Append('(');
         builder.AppendJoin(",", columns.Select(c => Activator.CreateInstance(c.DataType)));
         builder.Append(')');
         return builder.ToString();
     }
     
     private static Type MapToSystemType(string pgType)
     {
         switch (pgType.ToLower())
         {
             case "integer":
                 return typeof(int);
             case "bigint":
                 return typeof(long);
             case "real":
                 return typeof(float);
             case "double precision":
                 return typeof(double);
             case "numeric":
             case "money":
                 return typeof(decimal);
             case "text":
             case "character varying":
             case "varchar":
                 return typeof(string);
             case "boolean":
                 return typeof(bool);
             case "date":
                 return typeof(DateTime);
             case "timestamp":
             case "timestamp without time zone":
                 return typeof(DateTime);
             case "timestamp with time zone":
                 return typeof(DateTimeOffset);
             case "time":
             case "time without time zone":
                 return typeof(TimeSpan);
             case "bytea":
                 return typeof(byte[]);
             case "uuid":
                 return typeof(Guid);
             default:
                 return typeof(object);
         }
     }

     
    public List<ColumnInfo> GetColumnInfo2(string tableName, DbConnection connection)
    {
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = GetColumnInfoSql;
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
    [Column("column_name")] 
    public string Name { get; set; }
    
    // public string DataType { get; set; }
    [Column("data_type")] 
    public Type DataType { get; set; }
    
    [Column("is_nullable")] 
    public bool IsNullable { get; set; }
    
    [Column("is_primary_key")] 
    public bool IsPrimaryKey { get; set; }
    
    [Column("is_foreign_key")] 
    public bool IsForeignKey { get; set; }

    [Column("foreign_table_name")] 
    public string ForeignTableName { get; set; }
    
    [Column("foreign_column_name")] 
    public string ForeignColumnName { get; set; }
}

/*

SELECT
    c.column_name ,
    c.data_type,
    CASE
        WHEN tc.constraint_type = 'FOREIGN KEY' THEN 'Yes'
        ELSE 'No'
    END AS is_foreign_key,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM 
    information_schema.columns c
LEFT JOIN information_schema.key_column_usage kcu 
    ON c.table_name = kcu.table_name 
    AND c.column_name = kcu.column_name
LEFT JOIN information_schema.table_constraints tc 
    ON kcu.constraint_name = tc.constraint_name
    AND tc.constraint_type = 'FOREIGN KEY'
LEFT JOIN information_schema.referential_constraints rc
    ON tc.constraint_name = rc.constraint_name
LEFT JOIN information_schema.constraint_column_usage ccu
    ON rc.unique_constraint_name = ccu.constraint_name
WHERE 
    c.table_name = 'orders';
    */