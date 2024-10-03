using FluentAssertions;
using Planto.Column;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class ColumnCheckTests : IAsyncLifetime
{
    private const string TableName = "TestTable";

    private const string TableWithVarcharPk = $"""
                                               CREATE TABLE {TableName} (
                                                    id INT PRIMARY KEY,
                                                    Age INT CHECK (Age >= 18 AND Age <= 65),
                                                    StartDate DATE CHECK (StartDate >= '2000-01-01'),
                                                    EndDate DATE,
                                                    CHECK (EndDate > StartDate OR EndDate IS NULL),
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
        var res = await _msSqlContainer.ExecScriptAsync(TableWithVarcharPk).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task ColumnInfoFor_TableWithColumnChecks()
    {
        // Arrange
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var tableInfo = await planto.GetTableInfo(TableName);

        // Assert
        tableInfo.ColumnInfos.Should().BeEquivalentTo(new List<ColumnInfo>()
        {
            new()
            {
                DataType = typeof(int),
                ColumnName = "id",
                IsNullable = false,
                IsIdentity = false,
                ColumnConstraints =
                [
                    new ColumnConstraint
                    {
                        IsForeignKey = false,
                        ForeignColumnName = null,
                        ForeignTableName = null,
                        ColumnName = "id",
                        IsPrimaryKey = true,
                        IsUnique = true,
                        ConstraintType = ConstraintType.PrimaryKey
                    }
                ]
            },
            new()
            {
                DataType = typeof(int),
                ColumnName = "Age",
                IsNullable = true,
                IsIdentity = false,
                ColumnChecks =
                [
                    new ColumnCheck
                    {
                        ColumnName = "Age",
                        CheckClause = "([Age]>=(18) AND [Age]<=(65))",
                    }
                ]
            },
            new()
            {
                DataType = typeof(DateTime),
                ColumnName = "StartDate",
                IsNullable = true,
                IsIdentity = false,
                ColumnChecks =
                [
                    new ColumnCheck
                    {
                        ColumnName = "StartDate",
                        CheckClause = "([StartDate]>='2000-01-01')",
                    },
                    new ColumnCheck
                    {
                        ColumnName = "StartDate",
                        CheckClause = "([EndDate]>[StartDate] OR [EndDate] IS NULL)",
                    }
                ]
            },
            new()
            {
                DataType = typeof(DateTime),
                ColumnName = "EndDate",
                IsNullable = true,
                IsIdentity = false,
                ColumnChecks =
                [
                    new ColumnCheck
                    {
                        ColumnName = "EndDate",
                        CheckClause = "([EndDate]>[StartDate] OR [EndDate] IS NULL)",
                    }
                ]
            },
        }, options => options.Excluding(x => x.ColumnConstraints[0].ConstraintName));
    }
}