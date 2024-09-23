using FluentAssertions;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class MultiLevelForeignKeyTableTest : IAsyncLifetime
{
    private const string TableName = "TableA";

    private const string TablesWithFkSql = $"""
                                            -- Create Table C (no foreign keys)
                                            CREATE TABLE TableC (
                                                TableC_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameC NVARCHAR(100),
                                            );

                                            -- Create Table E (no foreign keys)
                                            CREATE TABLE TableE (
                                                TableE_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameE NVARCHAR(100),
                                            );

                                            -- Create Table D (has FK to E)
                                            CREATE TABLE TableD (
                                                TableD_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameD NVARCHAR(100),
                                                TableE_ID INT,
                                                FOREIGN KEY (TableE_ID) REFERENCES TableE(TableE_ID),
                                            );

                                            -- Create Table B (has FK to C and D)
                                            CREATE TABLE TableB (
                                                TableB_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameB NVARCHAR(100),
                                                TableC_ID INT,
                                                TableD_ID INT,
                                                FOREIGN KEY (TableC_ID) REFERENCES TableC(TableC_ID),
                                                FOREIGN KEY (TableD_ID) REFERENCES TableD(TableD_ID)
                                            );

                                            -- Create Table A (has FK to B)
                                            CREATE TABLE {TableName} (
                                                TableA_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameA NVARCHAR(100),
                                                TableB_ID INT,
                                                FOREIGN KEY (TableB_ID) REFERENCES TableB(TableB_ID)
                                            );
                                            """;


    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .Build();

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        var res = await _msSqlContainer.ExecScriptAsync(TablesWithFkSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task CreateExecutionTree_ForMultiLevelFkTables()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var tableA = await planto.CreateExecutionTree(TableName);

        // Assert
        tableA.Children.Count.Should().Be(1);
        var tableB = tableA.Children.Single(en => en.TableName == "TableB");
        tableB.Children.Count.Should().Be(2);
        var tableC = tableB.Children.Single(en => en.TableName == "TableC");
        var tableD = tableB.Children.Single(en => en.TableName == "TableD");
        tableC.Children.Count.Should().Be(0);
        tableD.Children.Count.Should().Be(1);
        var tableE = tableD.Children.Single(en => en.TableName == "TableE");
        tableE.Children.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateMultipleEntities_ForMultiLevelFkTables()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var id1 = await planto.CreateEntity(TableName);
        var id2 = await planto.CreateEntity(TableName);

        // Assert
        id1.Should().Be(1);
        id2.Should().Be(2);
    }
}