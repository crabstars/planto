using FluentAssertions;
using Planto.DatabaseImplementation;
using Planto.OptionBuilder;
using Planto.Test.Helper;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class SchemaTests : IAsyncLifetime
{
    private const string TableName = "TestTable";
    private const string FirstSchemaName = "firstSchema";
    private const string SecondSchemaName = "secondSchema";

    private const string CreateFirstSchema = $" CREATE SCHEMA {FirstSchemaName};";
    private const string CreateSecondSchema = $" CREATE SCHEMA {SecondSchemaName}";

    private const string CreateTwoTables = $"""
                                             CREATE TABLE {FirstSchemaName}.{TableName} (
                                                 id VARCHAR(100) PRIMARY KEY,
                                                 name VARCHAR(100) NOT NULL
                                             ); 
                                             CREATE TABLE {SecondSchemaName}.{TableName} (
                                                 id VARCHAR(100) PRIMARY KEY,
                                                 name VARCHAR(100) NOT NULL
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
        var res = await _msSqlContainer.ExecScriptAsync(CreateFirstSchema).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        res = await _msSqlContainer.ExecScriptAsync(CreateSecondSchema).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        res = await _msSqlContainer.ExecScriptAsync(CreateTwoTables).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task TwoSchema_SpecifyAndUseCorrectSchema()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet(), DbmsType.MsSql,
            options => options.SetValueGeneration(ValueGeneration.Random).SetDefaultSchema(FirstSchemaName));

        // Act
        var id = await planto.CreateEntity<string>(TableName);

        // Assert
        id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TwoSchema_WithSameTableName_DontSetSchema_Throws()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet(), DbmsType.MsSql,
            options => options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var act = async () => await planto.CreateEntity<string>(TableName);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Exception occured, rollback done: Sequence contains more than one matching element");
    }
}