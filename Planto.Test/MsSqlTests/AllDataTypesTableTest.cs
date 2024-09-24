using FluentAssertions;
using Planto.DatabaseImplementation.MsSql.DataTypes;
using Planto.OptionBuilder;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class AllDataTypesTableTest : IAsyncLifetime
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
                                                     varchar_max_column VARCHAR(MAX) NOT NULL,
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
            MaxCharLen = 10,
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
            MaxCharLen = 100,
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
            MaxCharLen = -1,
            Name = "varchar_max_column",
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
            MaxCharLen = 2147483647,
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
            MaxCharLen = 10,
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
            MaxCharLen = 100,
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
            MaxCharLen = 1073741823,
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
            MaxCharLen = 50,
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
            MaxCharLen = 50,
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
            MaxCharLen = 2147483647,
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
            MaxCharLen = -1,
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
            MaxCharLen = -1,
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(HierarchyId),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "hierarchyid_column",
            MaxCharLen = 892,
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(Geography),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "geography_column",
            MaxCharLen = -1,
            IsNullable = false,
            IsIdentity = false,
            IsPrimaryKey = false
        },
        new()
        {
            IsForeignKey = false,
            DataType = typeof(Geometry),
            ForeignColumnName = null,
            ForeignTableName = null,
            Name = "geometry_column",
            MaxCharLen = -1,
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
        var res = await _msSqlContainer.ExecScriptAsync(AllDataTypesTableSql).ConfigureAwait(true);
        res.Stderr.Should().BeEmpty();
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task AllTypesTable_CheckColumnInfo()
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
    public async Task InsertForAllDataTypes_UseDefaultValues()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

        // Act
        var insertedEntity = await planto.CreateEntity<int>(TableName);

        // Assert
        insertedEntity.Should().Be(1);
    }

    [Fact]
    public async Task InsertForAllDataTypes_UseRandomValues()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var insertedEntity = await planto.CreateEntity<int>(TableName);

        // Assert
        insertedEntity.Should().Be(1);
    }

    [Fact]
    public async Task InsertForAllDataTypes2Entities_UseRandomValues()
    {
        // Arrange
        var planto = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql,
            options =>
                options.SetValueGeneration(ValueGeneration.Random));

        // Act
        var insertedEntity1 = await planto.CreateEntity<int>(TableName);
        var insertedEntity2 = await planto.CreateEntity<int>(TableName);

        // Assert
        insertedEntity1.Should().Be(1);
        insertedEntity2.Should().Be(2);
    }
}