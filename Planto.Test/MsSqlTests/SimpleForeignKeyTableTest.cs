using FluentAssertions;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class SimpleForeignKeyTableTest : IAsyncLifetime
{
    private const string TableName = "table_with_foreign_key";
    private const string ReferenceTableName = "table1";

    private const string TablesWithFkSql = $"""
                                            CREATE TABLE {ReferenceTableName} (
                                                id INT IDENTITY(1,1) PRIMARY KEY
                                            );

                                            CREATE TABLE {TableName} (
                                                id INT IDENTITY(1,1) PRIMARY KEY,
                                                refTable INT NOT NULL,
                                                CONSTRAINT FK_{ReferenceTableName}_{TableName} FOREIGN KEY (refTable)
                                                   REFERENCES {ReferenceTableName}(id)
                                            );
                                            """;


    private readonly List<ColumnInfo> _columnInfos =
    [
        new()
        {
            IsForeignKey = false,
            DataType = typeof(int),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "id",
            IsNullable = false,
            IsIdentity = true,
            IsPrimaryKey = true
        },
        new()
        {
            IsForeignKey = true,
            DataType = typeof(int),
            ForeignColumnName = "id",
            ForeignTableName = ReferenceTableName,
            Name = "refTable",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        }
    ];

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
    public async Task TwoTablesConnectedWithFk_CheckColumnInfo()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var res = await planto.GetColumnInfo(TableName);

        // Assert
        res.Should().HaveCount(_columnInfos.Count);
        res.Should().BeEquivalentTo(_columnInfos);
    }

    [Fact]
    public async Task CreateExecutionTree_For2TableDb()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var resExecutionNode = await planto.CreateExecutionTree(TableName);

        // Assert
        resExecutionNode.Children.Count.Should().Be(1);
        resExecutionNode.TableName.Should().Be(TableName);
        resExecutionNode.Children.Single().TableName.Should().Be(ReferenceTableName);
        resExecutionNode.Children.Single().Children.Count.Should().Be(0);
    }
}