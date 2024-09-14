using System.Data.Common;

namespace Planto.DatabaseImplementation;

public interface IDatabaseSchemaHelper
{
    public Type MapToSystemType(string sqlType);

    public string GetColumnInfoSql(string tableName);

    public object? CreateDefaultValue(Type type);

    public Task<object> Insert(ExecutionNode executionNode);

    public Task<DbConnection> GetOpenConnection();
}