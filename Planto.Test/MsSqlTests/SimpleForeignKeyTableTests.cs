using FluentAssertions;
using Planto.Column;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class SimpleForeignKeyTableTests : IAsyncLifetime
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
            DataType = typeof(int),
            ColumnName = "id",
            IsNullable = false,
            IsIdentity = true,
            ColumnConstraints =
            [
                new ColumnConstraint
                {
                    ColumnName = "id",
                    ConstraintName = "PK",
                    ConstraintType = ConstraintType.PrimaryKey,
                    IsUnique = true,
                    IsForeignKey = false,
                    IsPrimaryKey = true,
                    ForeignTableName = null,
                    ForeignColumnName = null
                }
            ]
        },
        new()
        {
            DataType = typeof(int),
            ColumnName = "refTable",
            IsNullable = false,
            IsIdentity = false,
            ColumnConstraints =
            [
                new ColumnConstraint
                {
                    ConstraintName = "FK_table1_table_with_foreign_key",
                    ConstraintType = ConstraintType.ForeignKey,
                    IsUnique = false,
                    IsForeignKey = true,
                    IsPrimaryKey = false,
                    ForeignTableName = "table1",
                    ForeignColumnName = "id",
                    ColumnName = "refTable"
                }
            ]
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
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var res = await planto.GetTableInfo(TableName);

        // Assert
        res.ColumnInfos.Should().HaveCount(_columnInfos.Count);
        res.ColumnInfos.Sum(c => c.ColumnConstraints.Count).Should()
            .Be(_columnInfos.Sum(c => c.ColumnConstraints.Count));
        res.ColumnInfos.Should().BeEquivalentTo(_columnInfos, options => options.Excluding(x => x.ColumnConstraints));
        res.ColumnInfos.SelectMany(c => c.ColumnConstraints).Should()
            .BeEquivalentTo(_columnInfos.SelectMany(c => c.ColumnConstraints),
                options => options.Excluding(x => x.ConstraintName));
        res.TableName.Should().Be(TableName);
    }

    [Fact]
    public async Task CreateExecutionTree_For2TableDb()
    {
        // Arrange
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var resExecutionNode = await planto.CreateExecutionTree(TableName, null);

        // Assert
        resExecutionNode.Children.Count.Should().Be(1);
        resExecutionNode.TableName.Should().Be(TableName);
        resExecutionNode.Children.Single().TableName.Should().Be(ReferenceTableName);
        resExecutionNode.Children.Single().Children.Count.Should().Be(0);
    }
}