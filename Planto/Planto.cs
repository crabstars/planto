using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;
using Npgsql;
using Planto.DatabaseImplementation;

namespace Planto;

public class Planto
{
    private readonly DbConnection _connection;
    private readonly IDatabaseSchemaHelper _dbSchemaHelper;

    public Planto(DbConnection connection)
    {
        _connection = connection;
        _dbSchemaHelper = connection switch
        {
            NpgsqlConnection => new NpgSql(),
            SqlConnection => new MsSql(),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + connection.GetType())
        };
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
        builder.AppendJoin(",",
            columns.Select(c => c.IsPrimaryKey ? "default" : _dbSchemaHelper.CreateDefaultValue(c.DataType)));
        builder.Append(')');
        return builder.ToString();
    }

    public List<ColumnInfo> GetColumnInfo(string tableName)
    {
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText = _dbSchemaHelper.GetColumnInfoSql(tableName);
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
}