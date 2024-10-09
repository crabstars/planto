using FluentAssertions;
using Planto.Column;
using Planto.DatabaseImplementation;
using Planto.DatabaseImplementation.SqlServer.DataTypes;
using Planto.OptionBuilder;
using Planto.Test.Helper;
using Testcontainers.MsSql;
using Xunit;

namespace Planto.Test.MsSqlTests;

public class AllDataTypesTableTests : IAsyncLifetime
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
                                                     
                                                     -- Computed
                                                     computed_column as char_column,
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
            IsComputed = false,
            ColumnConstraints =
            [
                new ColumnConstraint
                {
                    ConstraintType = ConstraintType.PrimaryKey,
                    IsUnique = true,
                    IsForeignKey = false,
                    IsPrimaryKey = true,
                    ForeignTableName = null,
                    ForeignColumnName = null,
                    ColumnName = "id"
                }
            ]
        },
        new()
        {
            DataType = typeof(int),
            ColumnName = "int_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(int),
            ColumnName = "int_column_null",
            IsNullable = true,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(byte),
            ColumnName = "tinyint_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(short),
            ColumnName = "smallint_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(long),
            ColumnName = "bigint_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(decimal),
            ColumnName = "decimal_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(decimal),
            ColumnName = "numeric_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(double),
            ColumnName = "float_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(float),
            ColumnName = "real_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(decimal),
            ColumnName = "money_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(short),
            ColumnName = "smallmoney_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "char_column",
            MaxCharLen = 10,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "varchar_column",
            MaxCharLen = 100,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "varchar_max_column",
            MaxCharLen = -1,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "text_column",
            MaxCharLen = 2147483647,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "nchar_column",
            MaxCharLen = 10,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "nvarchar_column",
            MaxCharLen = 100,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "ntext_column",
            MaxCharLen = 1073741823,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(DateTime),
            ColumnName = "date_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(DateTime),
            ColumnName = "datetime_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(DateTime),
            ColumnName = "datetime2_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(DateTime),
            ColumnName = "smalldatetime_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(TimeSpan),
            ColumnName = "time_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(DateTimeOffset),
            ColumnName = "datetimeoffset_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(byte[]),
            ColumnName = "binary_column",
            MaxCharLen = 50,
            IsNullable = false,
            IsComputed = false,
            IsIdentity = false,
        },
        new()
        {
            DataType = typeof(byte[]),
            ColumnName = "varbinary_column",
            MaxCharLen = 50,
            IsNullable = false,
            IsComputed = false,
            IsIdentity = false,
        },
        new()
        {
            DataType = typeof(byte[]),
            ColumnName = "image_column",
            MaxCharLen = 2147483647,
            IsComputed = false,
            IsNullable = false,
            IsIdentity = false,
        },
        new()
        {
            DataType = typeof(bool),
            ColumnName = "bit_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(Guid),
            ColumnName = "uniqueidentifier_column",
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "xml_column",
            MaxCharLen = -1,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "json_column",
            MaxCharLen = -1,
            IsNullable = false,
            IsComputed = false,
            IsIdentity = false,
        },
        new()
        {
            DataType = typeof(HierarchyId),
            ColumnName = "hierarchyid_column",
            MaxCharLen = 892,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(Geography),
            ColumnName = "geography_column",
            MaxCharLen = -1,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(Geometry),
            ColumnName = "geometry_column",
            MaxCharLen = -1,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = false,
        },
        new()
        {
            DataType = typeof(string),
            ColumnName = "computed_column",
            MaxCharLen = 10,
            IsNullable = false,
            IsIdentity = false,
            IsComputed = true,
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
        await using var planto =
            new Planto(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet(), DbmsType.MsSql);

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
        var planto = new Planto(_msSqlContainer.GetConnectionStringWithMultipleActiveResultSet(), DbmsType.MsSql,
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