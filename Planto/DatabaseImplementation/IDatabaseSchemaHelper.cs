using System.Data.Common;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation;

public interface IDatabaseSchemaHelper
{
    public Type MapToSystemType(string sqlType);

    public string GetColumnInfoSql(string tableName);

    public string GetColumnConstraintsSql(string tableName);

    public Task<object> Insert(ExecutionNode executionNode, ValueGeneration valueGeneration);

    public Task<DbConnection> GetOpenConnection();
}