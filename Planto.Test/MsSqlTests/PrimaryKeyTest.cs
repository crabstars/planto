using FluentAssertions;
using Planto.Column;
using Planto.OptionBuilder;
using Planto.Test.Helper;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class PrimaryKeyTest : IAsyncLifetime
{
    private const string TableName = "TestTable";

    private const string TableWithVarcharPk = $"""
                                                CREATE TABLE {TableName} (
                                                    id VARCHAR(100) PRIMARY KEY,
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
    public async Task ColumnInfoFor_TableWithVarcharPk()
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
                DataType = typeof(string),
                ColumnName = "id",
                IsNullable = false,
                IsIdentity = false,
                MaxCharLen = 100,
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
            }
        }, options => options.Excluding(x => x.ColumnConstraints[0].ConstraintName));
    }

    [Fact]
    public async Task InsertRandomEntries_TableWithVarcharPk()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet(), DbmsType.MsSql,
            options => options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var id1 = await planto.CreateEntity<string>(TableName);
        var id2 = await planto.CreateEntity<string>(TableName);
        var countRes = await _msSqlContainer.ExecScriptAsync($"Select Count(*) From {TableName}").ConfigureAwait(true);

        // Assert
        id1.Should().NotBe(string.Empty);
        id2.Should().NotBe(string.Empty);
        id1.Should().NotBe(id2);
        countRes.Stderr.Should().BeEmpty();
        countRes.Stdout.Should().Contain("2");
    }
}

// TODO
//var retres = await _msSqlContainer.ExecScriptAsync($"Select Count(*) as c from {TableName}");
// """ Handle following 
// CREATE TABLE Example (
//     ID VARCHAR(100) PRIMARY KEY,
//     Name VARCHAR(50)
// ); 
//
// CREATE TABLE Example (
//     ID INT PRIMARY KEY,
//     Name VARCHAR(50)
// );
//
//
// CREATE TABLE Example (
//     ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
//     Name VARCHAR(50)
// );
//
// CREATE TABLE Example (
//     ID1 INT,
//     ID2 INT,
//     Name VARCHAR(50),
//     PRIMARY KEY (ID1, ID2)
// );
//
// CREATE SEQUENCE ExampleSequence AS INT START WITH 1 INCREMENT BY 1;
//
// CREATE TABLE Example (
//     ID INT PRIMARY KEY DEFAULT (NEXT VALUE FOR ExampleSequence),
//     Name VARCHAR(50)
// );
//
//
// SELECT 
//     t.name AS TableName,
//     c.name AS ColumnName,
//     tp.name AS DataType,
//     CASE 
//         WHEN ic.object_id IS NOT NULL THEN 'Identity'
//         WHEN tp.name = 'uniqueidentifier' THEN 'GUID'
//         WHEN s.object_id IS NOT NULL THEN 'Sequence-based'
//         ELSE 'Standard'
//     END AS PKType,
//     CASE 
//         WHEN COUNT(*) OVER (PARTITION BY ic.object_id) > 1 THEN 'Composite'
//         ELSE 'Single Column'
//     END AS PKComposition
// FROM 
//     sys.tables t
// INNER JOIN sys.indexes i ON t.object_id = i.object_id
// INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
// INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
// INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
// LEFT JOIN sys.identity_columns idc ON c.object_id = idc.object_id AND c.column_id = idc.column_id
// LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
// LEFT JOIN sys.sequences s ON dc.definition = ('(NEXT VALUE FOR [' + OBJECT_SCHEMA_NAME(s.object_id) + '].[' + OBJECT_NAME(s.object_id) + '])')
// WHERE 
//     i.is_primary_key = 1
// ORDER BY 
//     t.name, ic.key_ordinal;
//
// """