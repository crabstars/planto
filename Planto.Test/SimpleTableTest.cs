using System.Data.Common;
using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Planto.Test;

public class SimpleTableTest : IAsyncLifetime
{
    private const string SimpleTableSql = """
                                                  CREATE TABLE Customers (
                                                     customer_id SERIAL PRIMARY KEY,
                                                     customer_name VARCHAR(100) NOT NULL,
                                                     email VARCHAR(100) UNIQUE NOT NULL
                                                  );
                                          """;

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
    public async Task ConnectionStateReturnsOpen()
    {
        // Arrange
        await using DbConnection connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        await _postgreSqlContainer.ExecScriptAsync(SimpleTableSql).ConfigureAwait(true);


        // Act
        var planto = new Planto();
        var res = planto.GetColumnInfo("customers", connection);
        var insertStatement = Planto.CreateInsertStatement(res, "customers");

        // Assert
        res.Should().HaveCount(3);
        res.Single(x => x.Name == "customer_id").Should().BeEquivalentTo(
            new ColumnInfo
            {
                IsForeignKey = false,
                DataType = typeof(int),
                ForeignColumnName = null,
                ForeignTableName = null,
                Name = "customer_id",
                IsNullable = false,
                IsPrimaryKey = true
            });
        insertStatement.Should().Be("Insert into customers (customer_id,customer_name,email)Values(default,'','')");
    }
}