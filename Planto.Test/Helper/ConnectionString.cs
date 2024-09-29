using Testcontainers.MsSql;

namespace Planto.Test.Helper;

public static class ConnectionString
{
    public static string GetConnectionStringWithMultipleActiveResultSet(this MsSqlContainer container)
    {
        return container.GetConnectionString() + ";MultipleActiveResultSets=True";
    }
}