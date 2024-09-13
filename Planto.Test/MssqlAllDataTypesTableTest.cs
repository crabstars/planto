using FluentAssertions;
using Planto.DatabaseImplementation;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test;

public class MssqlAllDataTypesTableTest : IAsyncLifetime
{
    private const string TableName = "all_datatypes_table";

    private const string AllDataTypesTableSql = $"""
                                                 CREATE TABLE {TableName} (
                                                     -- Numeric Data Types
                                                     id INT IDENTITY(1,1) PRIMARY KEY,
                                                     int_column INT NOT NULL,
                                                     int_column_null INT,
                                                     tinyint_column TINYINT NOT NULL,
                                                     smallint_column SMALLINT NOT NULL,
                                                     bigint_column BIGINT NOT NULL,
                                                     decimal_column DECIMAL(10, 2) NOT NULL,
                                                     numeric_column NUMERIC(10, 2) NOT NULL,
                                                     float_column FLOAT NOT NULL,
                                                     real_column REAL NOT NULL,
                                                     money_column MONEY NOT NULL,
                                                     smallmoney_column SMALLMONEY NOT NULL,
                                                 
                                                     -- String Data Types
                                                     char_column CHAR(10) NOT NULL,
                                                     varchar_column VARCHAR(100) NOT NULL,
                                                     text_column TEXT NOT NULL,
                                                     nchar_column NCHAR(10) NOT NULL,
                                                     nvarchar_column NVARCHAR(100) NOT NULL,
                                                     ntext_column NTEXT NOT NULL,
                                                 
                                                     -- Date and Time Data Types
                                                     date_column DATE NOT NULL,
                                                     datetime_column DATETIME NOT NULL,
                                                     datetime2_column DATETIME2 NOT NULL,
                                                     smalldatetime_column SMALLDATETIME NOT NULL,
                                                     time_column TIME NOT NULL,
                                                     datetimeoffset_column DATETIMEOFFSET NOT NULL,
                                                 
                                                     -- Binary Data Types
                                                     binary_column BINARY(50) NOT NULL,
                                                     varbinary_column VARBINARY(50) NOT NULL,
                                                     image_column IMAGE NOT NULL,
                                                 
                                                     -- Other Data Types
                                                     bit_column BIT NOT NULL,
                                                     uniqueidentifier_column UNIQUEIDENTIFIER NOT NULL,
                                                     xml_column XML NOT NULL,
                                                     json_column NVARCHAR(MAX) NOT NULL,
                                                     hierarchyid_column HIERARCHYID NOT NULL,
                                                     geography_column GEOGRAPHY NOT NULL,
                                                     geometry_column GEOMETRY NOT NULL,
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
            IsForeignKey = false,
            DataType = typeof(int),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "int_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(int),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "int_column_null",
            IsNullable = true,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(byte),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "tinyint_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(short),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "smallint_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(long),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "bigint_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(decimal),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "decimal_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(decimal),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "numeric_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(double),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "float_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(float),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "real_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(decimal),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "money_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(decimal),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "smallmoney_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "char_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "varchar_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "text_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "nchar_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "nvarchar_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "ntext_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(DateTime),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "date_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(DateTime),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "datetime_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(DateTime),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "datetime2_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(DateTime),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "smalldatetime_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(TimeSpan),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "time_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(DateTimeOffset),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "datetimeoffset_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(byte[]),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "binary_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(byte[]),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "varbinary_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(byte[]),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "image_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(bool),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "bit_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(Guid),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "uniqueidentifier_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "xml_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(string),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "json_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(MsSql.HierarchyId),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "hierarchyid_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(MsSql.Geography),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "geography_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(MsSql.Geometry),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "geometry_column",
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        }
    ];

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .Build();

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        await _msSqlContainer.ExecScriptAsync(AllDataTypesTableSql).ConfigureAwait(true);
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public void AllTypesTable_CheckColumnInfo()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var res = planto.GetColumnInfo(TableName);

        // Assert
        res.Should().HaveCount(_columnInfos.Count);
        res.Should().BeEquivalentTo(_columnInfos);
    }

    [Fact]
    public async Task CreateInsertForSimpleTable_FromColumnInfo()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var insertStatement = planto.CreateInsertStatement(_columnInfos, TableName);

        // Assert
        var insertRes = await _msSqlContainer.ExecScriptAsync(insertStatement);
        insertRes.Stderr.Should().BeEmpty();
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
// CREATE TABLE Example (
//     ID INT PRIMARY KEY IDENTITY(1,1),
//     Name VARCHAR(50)
// );
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