using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Planto.DatabaseImplementation;

namespace Planto;

public class Planto
{
    private readonly string _connectionString;
    private readonly IDatabaseSchemaHelper _dbSchemaHelper;

    public Planto(string connectionString, DbmsType dbmsType)
    {
        _connectionString = connectionString;
        _dbSchemaHelper = dbmsType switch
        {
            DbmsType.NpgSql => new NpgSql(),
            DbmsType.MsSql => new MsSql(),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + dbmsType)
        };
    }

    public string CreateInsertStatement(List<ColumnInfo> columns, string tableName)
    {
        return _dbSchemaHelper.CreateInsertStatement(columns, tableName);
    }

    public List<ColumnInfo> GetColumnInfo(string tableName)
    {
        using var connection = _dbSchemaHelper.GetOpenConnection(_connectionString);

        using var command = connection.CreateCommand();
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