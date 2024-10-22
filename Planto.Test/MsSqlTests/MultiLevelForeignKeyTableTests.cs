using FluentAssertions;
using Planto.DatabaseImplementation;
using Planto.Test.Helper;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class MultiLevelForeignKeyTableTests : IAsyncLifetime
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
                                                TableE_ID INT NOT NULL,
                                                FOREIGN KEY (TableE_ID) REFERENCES TableE(TableE_ID),
                                            );

                                            -- Create Table B (has FK to C and D)
                                            CREATE TABLE TableB (
                                                TableB_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameB NVARCHAR(100),
                                                TableC_ID INT NOT NULL,
                                                TableD_ID INT NOT NULL,
                                                FOREIGN KEY (TableC_ID) REFERENCES TableC(TableC_ID),
                                                FOREIGN KEY (TableD_ID) REFERENCES TableD(TableD_ID)
                                            );

                                            -- Create Table A (has FK to B)
                                            CREATE TABLE {TableName} (
                                                TableA_ID INT PRIMARY KEY IDENTITY(1,1),
                                                NameA NVARCHAR(100),
                                                TableB_ID INT NOT NULL,
                                                FOREIGN KEY (TableB_ID) REFERENCES TableB(TableB_ID)
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
        var executionTreeBuilder =
            ExecutionTreeBuilderFactory.Create(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet());

        // Act
        var tableA = await executionTreeBuilder.CreateExecutionTree(TableName, null);

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
        var planto = new Planto(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet(), DbmsType.MsSql);

        // Act
        var id1 = await planto.CreateEntity<int>(TableName);
        var id2 = await planto.CreateEntity<int>(TableName);

        // Assert
        id1.Should().Be(1);
        id2.Should().Be(2);
    }
}
// Test this case => improve db a biut
// -- Table Products
//     CREATE TABLE Products (
//     ProductID INT PRIMARY KEY,
//     ProductName VARCHAR(100)
//     );
//
// -- Table Services
//     CREATE TABLE Services (
//     ServiceID INT PRIMARY KEY,
//     ServiceName VARCHAR(100)
//     );
//
// -- Transactions Table with two Foreign Keys
//     CREATE TABLE Transactions2 (
//     TransactionID INT PRIMARY KEY,
//     ProductID INT,
// ServiceID INT,
//     FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
// FOREIGN KEY (ProductID) REFERENCES Services(ServiceID),
// CHECK (
//     (ProductID IS NOT NULL AND ServiceID IS NULL) OR
//         (ProductID IS NULL AND ServiceID IS NOT NULL)
//     )
//     );