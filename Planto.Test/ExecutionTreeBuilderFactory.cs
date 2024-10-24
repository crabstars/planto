using Planto.Column;
using Planto.DatabaseImplementation.SqlServer;
using Planto.ExecutionTree;

namespace Planto.Test;

internal static class ExecutionTreeBuilderFactory
{
    internal static ExecutionTreeBuilder Create(string connectionString, bool columnCheckValueGenerator = false,
        bool cacheColumns = false)
    {
        var connectionHandler = new DatabaseConnectionHandler.DatabaseConnectionHandler(connectionString);
        var dbProviderHelper = new MsSql(connectionHandler, "");
        var columnHelper = new ColumnHelper(dbProviderHelper, cacheColumns);
        return new ExecutionTreeBuilder(null, columnHelper, columnCheckValueGenerator);
    }
}