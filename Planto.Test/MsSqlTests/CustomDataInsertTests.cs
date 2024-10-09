using FluentAssertions;
using Planto.Attributes;
using Planto.DatabaseImplementation;
using Testcontainers.MsSql;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming

namespace Planto.Test.MsSqlTests;

public class CustomDataInsertTests : IAsyncLifetime
{
    private const string TableName = "TestTable";

    private const string TestTableSql = $"""
                                          CREATE TABLE {TableName} (
                                              id INT IDENTITY PRIMARY KEY,
                                              name varchar(100) NOT NULL,
                                              age int NOT NULL,
                                          ); 
                                         """;
    private const string TestTableWithOnlyNullableFieldsSql = $"""
                                          CREATE TABLE {TableName} (
                                              id INT IDENTITY PRIMARY KEY,
                                              name varchar(100),
                                              age int,
                                          ); 
                                         """;

    private const string TableNameEmployee = "EmployeeTable";

    private const string TwoConnectedTableSql = $"""
                                                 CREATE TABLE CompanyTable (
                                                     CompanyId INT IDENTITY(1,1) PRIMARY KEY,
                                                     Name NVARCHAR(100) NOT NULL,
                                                     EmployeeCount INT NOT NULL
                                                 );

                                                 CREATE TABLE {TableNameEmployee} (
                                                     EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
                                                     Name NVARCHAR(100) NOT NULL,
                                                     Age INT NOT NULL, 
                                                     CompanyFk INT NOT NULL,
                                                     CONSTRAINT FK_Employee_Company FOREIGN KEY (CompanyFk) 
                                                         REFERENCES CompanyTable (CompanyId)
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
    public async Task CreateEntity_WithGivenDataAndClassWithAttributes_Matches()
    {
        // Arrange
        var initRes = await _msSqlContainer.ExecScriptAsync(TestTableSql).ConfigureAwait(true);
        initRes.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTableDb { MyName = myName, TestAge = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(myName).And.Contain(age.ToString());
    }

    [Theory]
    [InlineData(TestTableSql)]
    [InlineData(TestTableWithOnlyNullableFieldsSql)]
    public async Task CreateEntity_WithGivenDataAndClassWithoutAttributes_Matches(string tableSql)
    {
        // Arrange
        var initRes = await _msSqlContainer.ExecScriptAsync(tableSql).ConfigureAwait(true);
        initRes.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTable { name = myName, age = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(myName).And.Contain(age.ToString());
    }

    [Fact]
    public async Task CreateEntity_WithGivenDataAndClassWithoutAttributes_DoesNotMatch()
    {
        // Arrange
        var initRes = await _msSqlContainer.ExecScriptAsync(TestTableSql).ConfigureAwait(true);
        initRes.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTableDbWrong { Name = myName, Age = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().NotContain(myName).And.NotContain(age.ToString());
    }

    [Fact]
    public async Task CreateEntity_WithGivenMultipleTables_Matches()
    {
        // Arrange
        var initRes = await _msSqlContainer.ExecScriptAsync(TwoConnectedTableSql).ConfigureAwait(true);
        initRes.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const int age = 19;
        const int employeeCount = 123;
        await planto.CreateEntity<int>(TableNameEmployee, new Company { EmployeeCount = employeeCount },
            new Employee { Age = age });
        var res = await _msSqlContainer
            .ExecScriptAsync($"SELECT * FROM {TableNameEmployee} e JOIN CompanyTable c ON e.CompanyFk = c.CompanyId; ")
            .ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(employeeCount.ToString()).And.Contain(age.ToString());
    }
    
    [Fact]
    public async Task CreateEntity_ForTestTableWithOnlyNullableFieldsSql_InsertCorrectData()
    {
        // Arrange
        var initRes = await _msSqlContainer.ExecScriptAsync(TestTableWithOnlyNullableFieldsSql).ConfigureAwait(true);
        initRes.Stderr.Should().BeEmpty();
        await using var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        const string myName = "TestName";
        const int age = 19;
        await planto.CreateEntity<int>(TableName, new TestTable { name = myName, age = age });
        var res = await _msSqlContainer.ExecScriptAsync($"Select * FROM {TableName}").ConfigureAwait(true);

        // Assert
        res.Stderr.Should().BeEmpty();
        res.Stdout.Should().Contain(myName).And.Contain(age.ToString());
    }

    [TableName("EmployeeTable")]
    private class Employee
    {
        [ColumnName("Age")] public int Age { get; set; }
    }

    [TableName("CompanyTable")]
    private class Company
    {
        [ColumnName("EmployeeCount")] public int EmployeeCount { get; set; }
    }

    [TableName("TestTable")]
    private class TestTableDb
    {
        [ColumnName("name")] public string? MyName { get; set; }
        [ColumnName("age")] public int TestAge { get; set; }
    }

    private class TestTable
    {
        public string? name { get; set; }
        public int age { get; set; }
    }

    private class TestTableDbWrong
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}