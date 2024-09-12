namespace Planto;

public interface IDatabaseSchemaHelper
{
    public Type MapToSystemType(string sqlType);

    public string GetColumnInfoSql(string tableName);

    public object? CreateDefaultValue(Type type);
}