using FluentAssertions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Planto.Test.NpgSqlTests;

public class NpgSqlSimpleTableTest : IAsyncLifetime
{
    private const string TableName = "customers";

    private const string SimpleTableSql = $"""
                                                   CREATE TABLE {TableName} (
                                                      customer_id SERIAL PRIMARY KEY,
                                                      customer_name VARCHAR(100),
                                                      email VARCHAR(100) UNIQUE NOT NULL
                                                   );
                                           """;

    private const string CustomerIdColumn = "customer_id";
    private const string CustomerNameColumn = "customer_name";
    private const string EmailColumn = "email";

    private readonly List<ColumnInfo> _columnInfos =
    [
        new()
        {
            IsForeignKey = false,
            DataType = typeof(int),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = CustomerIdColumn,
            IsNullable = false,
            IsPrimaryKey = true
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = CustomerNameColumn,
            IsNullable = true,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = EmailColumn,
            IsNullable = false,
            IsPrimaryKey = false
        }
    ];

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .Build();

    public Task InitializeAsync()
    {
        return _postgreSqlContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task SimpleTable_CheckColumnInfo()
    {
        // Arrange
        await _postgreSqlContainer.ExecScriptAsync(SimpleTableSql).ConfigureAwait(true);
        var planto = new Planto(_postgreSqlContainer.GetConnectionString(), DbmsType.NpgSql);

        // Act
        var res = await planto.GetColumnInfo(TableName);

        // Assert
        res.Should().HaveCount(3);
        res.Should().BeEquivalentTo(_columnInfos);
    }

    [Fact]
    public void CreateInsertForSimpleTable_FromColumnInfo()
    {
        // Arrange
        var planto = new Planto(_postgreSqlContainer.GetConnectionString(), DbmsType.NpgSql);

        // Act
        var insertStatement = planto.CreateInsertStatement(_columnInfos, TableName);

        // Assert
        insertStatement.Should().Be("Insert into customers (customer_id,customer_name,email)Values(default,'','')");
    }
}