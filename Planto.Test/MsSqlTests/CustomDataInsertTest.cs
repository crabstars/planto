using FluentAssertions;
using Planto.Attributes;
using Testcontainers.MsSql;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming

namespace Planto.Test.MsSqlTests;

public class CustomDataInsertTest : IAsyncLifetime
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
    public async Task CreateEntity_WithGivenDataAndClassWithAttributes_Matches()
    {
        // Arrange
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTableDb { MyName = myName, TestAge = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(myName).And.Contain(age.ToString());
    }

    [Fact]
    public async Task CreateEntity_WithGivenDataAndClassWithoutAttributes_Matches()
    {
        // Arrange
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTable { name = myName, age = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(myName).And.Contain(age.ToString());
    }

    [Fact]
    public async Task CreateEntity_WithGivenDataAndClassWithoutAttributes_DoesNotMatch()
    {
        // Arrange
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTableDbWrong { Name = myName, Age = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().NotContain(myName).And.NotContain(age.ToString());
    }

    [TableName("TestTable")]
    private class TestTableDb
    {
        [ColumnName("name")] public string? MyName { get; set; }
        [ColumnName("age")] public int TestAge { get; set; }
    }

    private class TestTable
    {
        public string? name { get; set; }
        public int age { get; set; }
    }

    private class TestTableDbWrong
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}