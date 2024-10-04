using FluentAssertions;
using Planto.Column.ColumnCheckSolver;
using Planto.DatabaseImplementation;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class ColumnCheckTests : IAsyncLifetime
{
    private const string TableName = "TestTable";

    private const string TableWithSimpleColumnChecks = $"""
                                                        CREATE TABLE {TableName} (
                                                             id INT PRIMARY KEY,
                                                             Age INT NOT NULL CHECK (Age >= 18 AND Age <= 65),
                                                             StartDate DATE NOT NULL CHECK (StartDate >= '2000-01-01'),
                                                             EndDate DATE,
                                                             CHECK (EndDate > StartDate OR EndDate IS NULL),
                                                         ); 
                                                        """;

    private const string TableWithComplexColumnChecks = $"""
                                                         CREATE TABLE {TableName} (
                                                              id INT PRIMARY KEY,
                                                              Salary DECIMAL(10, 2), 
                                                              Bonus DECIMAL(10, 2),
                                                              Email NVARCHAR(100) CHECK (Email LIKE '%@%'),
                                                              CHECK (0 >= Bonus AND (Bonus != Salary * 0.2 AND (Bonus >= 100 OR Bonus = 50)))
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
    public async Task ParseConstraint_Simple()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(TableWithSimpleColumnChecks).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var tableInfo = await planto.GetTableInfo(TableName);
        var columnCheckTreeCreator = new ColumnCheckSyntaxParser();
        var treeAgeCheckConstraint = columnCheckTreeCreator.Parse(tableInfo.ColumnInfos[1].ColumnChecks[0].CheckClause);
        var treeStartAndEndDateCheckConstraint =
            columnCheckTreeCreator.Parse(tableInfo.ColumnInfos[2].ColumnChecks[0].CheckClause);
        var treeStartDateCheckConstraint =
            columnCheckTreeCreator.Parse(tableInfo.ColumnInfos[2].ColumnChecks[1].CheckClause);
        var treeEndDateConstraint = columnCheckTreeCreator.Parse(tableInfo.ColumnInfos[3].ColumnChecks[0].CheckClause);

        // Assert
        treeAgeCheckConstraint.Expression.Should().Be("([Age]>=(18) AND [Age]<=(65))");
        treeAgeCheckConstraint.Value.Should().BeNull();
        treeAgeCheckConstraint.Children[0].Expression.Should().Be("[Age]>=(18)");
        treeAgeCheckConstraint.Children[0].Value.Should().Be("18");
        treeAgeCheckConstraint.Children[1].Expression.Should().Be("[Age]<=(65)");
        treeAgeCheckConstraint.Children[1].Value.Should().Be("65");

        treeStartDateCheckConstraint.Expression.Should().Be("([StartDate]>='2000-01-01')");
        treeStartDateCheckConstraint.Value.Should().Be("'2000-01-01'");
        treeStartDateCheckConstraint.Children.Should().BeEmpty();

        treeStartAndEndDateCheckConstraint.Expression.Should().Be("([EndDate]>[StartDate] OR [EndDate] IS NULL)");
        treeStartAndEndDateCheckConstraint.Value.Should().BeNull();
        treeStartAndEndDateCheckConstraint.Children[0].Expression.Should().Be("[EndDate]>[StartDate]");
        treeStartAndEndDateCheckConstraint.Children[0].Value.Should().BeNull();
        treeStartAndEndDateCheckConstraint.Children[1].Expression.Should().Be("[EndDate] IS NULL");
        treeStartAndEndDateCheckConstraint.Children[1].Value.Should().Be("NULL");


        treeEndDateConstraint.Expression.Should().Be("([EndDate]>[StartDate] OR [EndDate] IS NULL)");
        treeEndDateConstraint.Value.Should().BeNull();
        treeEndDateConstraint.Children[0].Expression.Should().Be("[EndDate]>[StartDate]");
        treeEndDateConstraint.Children[0].Value.Should().BeNull();
        treeEndDateConstraint.Children[1].Expression.Should().Be("[EndDate] IS NULL");
        treeEndDateConstraint.Children[1].Value.Should().Be("NULL");
    }

    [Fact]
    public async Task ParseConstraint_Complex()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(TableWithComplexColumnChecks).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var tableInfo = await planto.GetTableInfo(TableName);
        var columnCheckTreeCreator = new ColumnCheckSyntaxParser();
        var bonusAndSalaryCheck = columnCheckTreeCreator.Parse(tableInfo.ColumnInfos[1].ColumnChecks[0].CheckClause);

        // Assert
        bonusAndSalaryCheck.Expression.Should()
            .Be("((0)>=[Bonus] AND ([Bonus]<>[Salary]*(0.2) AND ([Bonus]>=(100) OR [Bonus]=(50))))");
        bonusAndSalaryCheck.Value.Should().BeNull();
        bonusAndSalaryCheck.Children[0].Expression.Should().Be("(0)>=[Bonus]");
        bonusAndSalaryCheck.Children[0].Value.Should().Be("0");
        bonusAndSalaryCheck.Children[1].Expression.Should().Be("[Bonus]<>[Salary]*(0.2)");
        bonusAndSalaryCheck.Children[1].Value.Should().BeNull();
        bonusAndSalaryCheck.Children[2].Expression.Should().Be("[Bonus]>=(100)");
        bonusAndSalaryCheck.Children[2].Value.Should().Be("100");
        bonusAndSalaryCheck.Children[3].Expression.Should().Be("[Bonus]=(50)");
        bonusAndSalaryCheck.Children[3].Value.Should().Be("50");
    }

    [Fact]
    public async Task ParseConstraint_LikeCheck()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(TableWithComplexColumnChecks).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var tableInfo = await planto.GetTableInfo(TableName);
        var columnCheckTreeCreator = new ColumnCheckSyntaxParser();
        var emailCheck = columnCheckTreeCreator.Parse(tableInfo.ColumnInfos[3].ColumnChecks[0].CheckClause);

        // Assert
        emailCheck.Expression.Should().Be("([Email] like '%@%')");
        emailCheck.Value.Should().Be("'@'");
    }

    [Fact]
    public async Task GenerateEntryFor_SimpleCheck()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(TableWithSimpleColumnChecks).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var id = await planto.CreateEntity<int>(TableName);
        var selectRes = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        selectRes.Stdout.Should().Contain(id.ToString());
    }

    [Fact]
    public void ColumnCheckExpression_GetAllValues()
    {
        // Arrange
        var columnCheckExpression = new ColumnCheckExpression
        {
            Value = null,
            Children =
            [
                new ColumnCheckExpression
                {
                    Value = 1,
                    Children =
                    [
                        new ColumnCheckExpression()
                        {
                            Value = 3
                        },
                    ]
                },
                new ColumnCheckExpression()
                {
                    Value = 4
                }
            ]
        };

        // Act && Assert
        columnCheckExpression.GetAllValues().Should().BeEquivalentTo(new List<object?> { null, 1, 3, 4 });
    }
}