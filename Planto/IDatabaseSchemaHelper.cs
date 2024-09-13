using System.Data.Common;

namespace Planto;

public interface IDatabaseSchemaHelper
{
    public Type MapToSystemType(string sqlType);

    public string GetColumnInfoSql(string tableName);

    public object? CreateDefaultValue(Type type);

    public string CreateInsertStatement(List<ColumnInfo> columns, string tableName);

    public DbConnection GetOpenConnection(string connectionString);
}