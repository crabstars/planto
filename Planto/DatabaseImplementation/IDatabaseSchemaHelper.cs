using System.Data.Common;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation;

public interface IDatabaseSchemaHelper
{
    public Type MapToSystemType(string sqlType);

    public string GetColumnInfoSql(string tableName);

    public object? CreateDefaultValue(Type type);

    public Task<object> Insert(ExecutionNode executionNode, ValueGeneration optionsValueGeneration);

    public Task<DbConnection> GetOpenConnection();
}