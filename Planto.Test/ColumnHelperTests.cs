using FluentAssertions;
using Planto.Column;
using Planto.DatabaseImplementation.SqlServer;
using Planto.Test.Helper;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test;

public class ColumnHelperTests : IAsyncLifetime
{
    
    private const string TableName = "TestTable";

    private const string TestTableSql = $"""
                                          CREATE TABLE {TableName} (
                                              id INT IDENTITY PRIMARY KEY,
                                              name varchar(100) NOT NULL,
                                              age int NOT NULL,
                                          ); 
                                         """;
    
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage(
            "mcr.microsoft.com/mssql/server:2022-latest"
        ).WithPortBinding(1433, true)
        .Build();
    
    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        var res = await _msSqlContainer.ExecScriptAsync(TestTableSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task ColumnHelper_GetColumInfos_CachedColumnsAreSet()
    {
        // Arrange
        var columnHelper = CreateColumnHelper();

        // Act
        var columInfos = await columnHelper.GetColumInfos(TableName);

        // Assert
        columnHelper.CachedColumns!.First().Value.Should().BeEquivalentTo(columInfos);
    }

    private ColumnHelper CreateColumnHelper()
    {
        var connectionHandler = new DatabaseConnectionHandler.DatabaseConnectionHandler(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet());
        var dbProviderHelper = new MsSql(connectionHandler, "");
        return new ColumnHelper(dbProviderHelper, true);
    }
}