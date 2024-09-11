using System.Data;
using System.Data.Common;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Planto.Test;

public class SimpleTableTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .Build();

    public Task InitializeAsync()
    {
        return _postgreSqlContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public void ConnectionStateReturnsOpen()
    {
        // Given
        using DbConnection connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        var script = File.ReadAllText("../../../Infrastructure/init.sql");
        _postgreSqlContainer.ExecScriptAsync(script).ConfigureAwait(true);
        
        var planto = new Planto();
        var res = planto.GetColumnInfo2("orders", connection);
        var insertStatement = planto.CreateInsertStatement(res, "customers");

        // Then
        Assert.Equal(ConnectionState.Open, connection.State);
    }

}