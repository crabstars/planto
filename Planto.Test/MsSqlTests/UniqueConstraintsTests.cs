using FluentAssertions;
using Planto.DatabaseImplementation;
using Planto.OptionBuilder;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class UniqueConstraintsTests : IAsyncLifetime
{
    private const string TableName = "Employees";

    private const string TableWithUniqueAndNullableColumn = $"""
                                                              CREATE TABLE {TableName} (
                                                                  EmployeeID INT IDENTITY PRIMARY KEY,
                                                                  ManagerID INT UNIQUE
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
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task CreateEntity_UniqueAndNullableColumn_Success()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(TableWithUniqueAndNullableColumn).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var id = await planto.CreateEntity<int?>(TableName);
        var id2 = await planto.CreateEntity<int?>(TableName);

        // Assert
        id2.Should().NotBe(id);
    }

}
