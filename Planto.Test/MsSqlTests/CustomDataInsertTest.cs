using FluentAssertions;
using Planto.Attributes;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class CustomDataInsertTest : IAsyncLifetime
{
    private const string TableName = "TestTable";

    private const string TestTableSql = $"""
                                          CREATE TABLE {TableName} (
                                              id INT IDENTITY PRIMARY KEY,
                                              name varchar(100) NOT NULL,
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
    public async Task CreateEntity_WithGivenData()
    {
        // Arrange
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        await planto.CreateEntity<int>(TableName, new TestTableDb { MyName = myName });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(myName);
    }

    [TableName("TestTable")]
    class TestTableDb
    {
        [ColumnName("name")] public string? MyName { get; set; }
    }

    class TestTable
    {
        public string? name { get; set; }
    }
}