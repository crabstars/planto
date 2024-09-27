using FluentAssertions;
using Planto.Exceptions;
using Planto.OptionBuilder;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class CircualDependencyTableTest : IAsyncLifetime
{
    private const string TableName = "Employees";
    private const string RelationTableName = "Departments";

    private const string SelfReferencingTableFkNullableSql = $"""
                                                              CREATE TABLE {TableName} (
                                                                  EmployeeID INT PRIMARY KEY,
                                                                  ManagerID INT, 
                                                                  FOREIGN KEY (ManagerID) REFERENCES {TableName}(EmployeeID)
                                                              );
                                                              """;

    private const string SelfReferencingTableFkNotNullableSql = $"""
                                                                 CREATE TABLE {TableName} (
                                                                     EmployeeID INT PRIMARY KEY,
                                                                     ManagerID INT NOT NULL, 
                                                                     FOREIGN KEY (ManagerID) REFERENCES {TableName}(EmployeeID)
                                                                 );
                                                                 """;

    private const string CircularDepTablesFkNullableSql = $"""
                                                           CREATE TABLE {TableName} (
                                                               EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
                                                               DepartmentID INT,
                                                           );

                                                           CREATE TABLE {RelationTableName} (
                                                               DepartmentID INT IDENTITY(1,1) PRIMARY KEY,
                                                               ManagerID INT,
                                                           );

                                                           ALTER TABLE {RelationTableName}
                                                           ADD CONSTRAINT FK_Departments_ManagerID
                                                           FOREIGN KEY (ManagerID) REFERENCES {TableName}(EmployeeID);

                                                           ALTER TABLE {TableName}
                                                           ADD CONSTRAINT FK_Employees_DepartmentID
                                                           FOREIGN KEY (DepartmentID) REFERENCES {RelationTableName}(DepartmentID);
                                                           """;

    private const string CircularDepTablesFkInSecondTableNullableSql = $"""
                                                                        CREATE TABLE {TableName} (
                                                                            EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
                                                                            DepartmentID INT NOT NULL,
                                                                        );

                                                                        CREATE TABLE {RelationTableName} (
                                                                            DepartmentID INT IDENTITY(1,1) PRIMARY KEY,
                                                                            ManagerID INT,
                                                                        );

                                                                        ALTER TABLE {RelationTableName}
                                                                        ADD CONSTRAINT FK_Departments_ManagerID
                                                                        FOREIGN KEY (ManagerID) REFERENCES {TableName}(EmployeeID);

                                                                        ALTER TABLE {TableName}
                                                                        ADD CONSTRAINT FK_Employees_DepartmentID
                                                                        FOREIGN KEY (DepartmentID) REFERENCES {RelationTableName}(DepartmentID);
                                                                        """;

    private const string CircularDepTablesFksNotNullableSql = $"""
                                                               CREATE TABLE {TableName} (
                                                                   EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
                                                                   DepartmentID INT NOT NULL,
                                                               );

                                                               CREATE TABLE {RelationTableName} (
                                                                   DepartmentID INT IDENTITY(1,1) PRIMARY KEY,
                                                                   ManagerID INT NOT NULL,
                                                               );

                                                               ALTER TABLE {RelationTableName}
                                                               ADD CONSTRAINT FK_Departments_ManagerID
                                                               FOREIGN KEY (ManagerID) REFERENCES {TableName}(EmployeeID);

                                                               ALTER TABLE {TableName}
                                                               ADD CONSTRAINT FK_Employees_DepartmentID
                                                               FOREIGN KEY (DepartmentID) REFERENCES {RelationTableName}(DepartmentID);
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
    public async Task SelfReferencingTableFkNullable_CanInsert()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(SelfReferencingTableFkNullableSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var id = await planto.CreateEntity<int?>(TableName);

        // Assert
        id.Should().NotBe(null);
    }


    [Fact]
    public async Task SelfReferencingTableFkNotNullable_ThrowsException()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(SelfReferencingTableFkNotNullableSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random).SetMaxDegreeOfParallelism(2));

        // Act
        var act = async () => await planto.CreateEntity<int>(TableName);

        // Assert
        await act.Should().ThrowAsync<CircularDependencyException>();
    }

    [Fact]
    public async Task CircularDepTablesFkInSecondTableNullable_CanInsert()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(CircularDepTablesFkInSecondTableNullableSql)
            .ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var id = await planto.CreateEntity<int?>(TableName);
        var countRes = await _msSqlContainer.ExecScriptAsync($"Select Count(*) From {RelationTableName}")
            .ConfigureAwait(true);


        // Assert
        id.Should().NotBe(null);
        countRes.Stderr.Should().BeEmpty();
        countRes.Stdout.Count(c => c == '1').Should().Be(2);
    }


    [Fact]
    public async Task CircularDepTablesFkNullable_CanInsert()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(CircularDepTablesFkNullableSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var id = await planto.CreateEntity<int?>(TableName);
        var countRes = await _msSqlContainer.ExecScriptAsync($"Select Count(*) From {RelationTableName}")
            .ConfigureAwait(true);


        // Assert
        id.Should().NotBe(null);
        countRes.Stderr.Should().BeEmpty();
        countRes.Stdout.Should().Contain("0");
    }


    [Fact]
    public async Task CircularDepTablesFksNotNullable_ThrowsException()
    {
        // Arrange
        var res = await _msSqlContainer.ExecScriptAsync(CircularDepTablesFksNotNullableSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var act = async () => await planto.CreateEntity<int>(TableName);

        // Assert
        await act.Should().ThrowAsync<CircularDependencyException>();
    }
}