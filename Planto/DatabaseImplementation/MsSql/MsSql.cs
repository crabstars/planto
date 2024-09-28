using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Planto.DatabaseImplementation.MsSql.DataTypes;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation.MsSql;

public class MsSql(string connectionString) : IDatabaseProviderHelper
{
    private const string LastIdIntSql = "SELECT CAST(SCOPE_IDENTITY() AS INT) AS GeneratedID;";
    private const string LastIdDecimalSql = "SELECT SCOPE_IDENTITY() AS GeneratedID;";

    private DbConnection? _dbConnection;
    private DbTransaction? _dbTransaction;

    public Type MapToSystemType(string sqlType)
    {
        var sqlToCSharpMap = new Dictionary<string, Type>
        {
            { "int", typeof(int) },
            { "tinyint", typeof(byte) },
            { "smallint", typeof(short) },
            { "bigint", typeof(long) },
            { "decimal", typeof(decimal) },
            { "numeric", typeof(decimal) },
            { "float", typeof(double) },
            { "real", typeof(float) },
            { "money", typeof(decimal) },
            { "smallmoney", typeof(decimal) },
            { "char", typeof(string) },
            { "varchar", typeof(string) },
            { "text", typeof(string) },
            { "nchar", typeof(string) },
            { "nvarchar", typeof(string) },
            { "ntext", typeof(string) },
            { "date", typeof(DateTime) },
            { "datetime", typeof(DateTime) },
            { "datetime2", typeof(DateTime) },
            { "smalldatetime", typeof(DateTime) },
            { "time", typeof(TimeSpan) },
            { "datetimeoffset", typeof(DateTimeOffset) },
            { "binary", typeof(byte[]) },
            { "varbinary", typeof(byte[]) },
            { "image", typeof(byte[]) },
            { "bit", typeof(bool) },
            { "uniqueidentifier", typeof(Guid) },
            { "xml", typeof(string) },
            { "json", typeof(string) },
            { "hierarchyid", typeof(HierarchyId) },
            { "geography", typeof(Geography) },
            { "geometry", typeof(Geometry) }
        };

        if (sqlToCSharpMap.ContainsKey(sqlType.ToLower()))
        {
            return sqlToCSharpMap[sqlType.ToLower()];
        }

        throw new ArgumentException($"SQL Type '{sqlType}' not recognized.");
    }

    public async Task<DbDataReader> GetColumnConstraints(string tableName)
    {
        var connection = await GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = GetDbTransaction();
        command.CommandText = GetColumnConstraintsSql(tableName);
        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }

    public async Task<DbDataReader> GetColumInfos(string tableName)
    {
        var connection = await GetOpenConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = GetDbTransaction();
        command.CommandText = GetColumnInfoSql(tableName);
        return await command.ExecuteReaderAsync().ConfigureAwait(false);
    }

    public async Task<TCast> CreateEntity<TCast>(ExecutionNode executionNode, PlantoOptions plantoOptions)
    {
        try
        {
            await StartTransaction();
            var id = (TCast)await Insert(executionNode, plantoOptions.ValueGeneration);
            await CommitTransaction();
            return id;
        }
        catch (Exception)
        {
            await RollbackTransaction();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbConnection != null) await _dbConnection.DisposeAsync().ConfigureAwait(false);
        if (_dbTransaction != null) await _dbTransaction.DisposeAsync().ConfigureAwait(false);
        await CloseConnection().ConfigureAwait(false);
    }

    public async Task<object> Insert(ExecutionNode executionNode, ValueGeneration valueGeneration)
    {
        var connection = await GetOpenConnection();

        var columns = executionNode.TableInfo.ColumnInfos
            .Where(c => (!c.IsIdentity.HasValue || !c.IsIdentity.Value) && !c.IsNullable)
            .ToList();
        var builder = new StringBuilder();
        builder.Append($"Insert into {executionNode.TableName} ");

        object? pk = null;
        if (columns.All(c =>
                c is { IsPrimaryKey: true, IsIdentity: not null } && c.IsIdentity.Value))
        {
            builder.Append("DEFAULT VALUES;");
        }
        else
        {
            builder.Append('(');
            builder.AppendJoin(",", columns.Select(c => c.ColumnName));
            builder.Append(')');
            builder.Append("Values");
            builder.Append('(');
            var values = new List<object?>();
            foreach (var c in columns)
            {
                if (c is { IsForeignKey: true, IsNullable: false })
                {
                    var foreignKey =
                        await Insert(executionNode.Children.Single(child => c.ColumnConstraints.Any(cc =>
                            cc.ForeignTableName == child.TableName && c.ColumnName == cc.ForeignColumnName)
                        ), valueGeneration);
                    values.Add(foreignKey);
                }
                else
                {
                    var value = SqlValueGeneration.CreateValueForMsSql(c.DataType, valueGeneration, c.MaxCharLen);
                    values.Add(value);
                    if (c.IsPrimaryKey)
                        pk = value;
                }
            }

            builder.AppendJoin(",", values);
            builder.Append(");");
        }

        if (pk is null)
        {
            builder.Append(executionNode.TableInfo.ColumnInfos.Any(c => c.DataType != typeof(int)
                                                                        && c.IsPrimaryKey)
                ? LastIdDecimalSql
                : LastIdIntSql);
        }

        var insertStatement = builder.ToString();
        executionNode.InsertStatement = insertStatement;
        await using var command = connection.CreateCommand();
        command.Transaction = GetDbTransaction();
        command.CommandText = insertStatement;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            if (pk != null)
            {
                await reader.CloseAsync();
                return pk;
            }

            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(reader.GetOrdinal("GeneratedID")))
                {
                    pk = reader["GeneratedID"];
                    await reader.CloseAsync();
                    executionNode.DbEntityId = pk;
                    return pk ?? throw new InvalidOperationException("GeneratedID was not found");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        throw new InvalidOperationException("Could not get GeneratedID. See logs");
    }

    public async Task<DbConnection> GetOpenConnection()
    {
        if (_dbConnection is not null && _dbConnection.State == ConnectionState.Open)
        {
            return _dbConnection;
        }

        _dbConnection = new SqlConnection(connectionString);
        await _dbConnection.OpenAsync().ConfigureAwait(false);
        return _dbConnection;
    }

    public async Task CloseConnection()
    {
        if (_dbConnection is not null && _dbConnection.State != ConnectionState.Closed)
        {
            await _dbConnection.CloseAsync().ConfigureAwait(false);
        }

        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync().ConfigureAwait(false);
            _dbConnection = null;
        }
    }

    public async Task StartTransaction()
    {
        _dbConnection ??= await GetOpenConnection().ConfigureAwait(false);

        // Bad Code because of !
        _dbTransaction = await _dbConnection.BeginTransactionAsync().ConfigureAwait(false);
    }

    public async Task CommitTransaction()
    {
        if (_dbTransaction != null)
        {
            await _dbTransaction.CommitAsync().ConfigureAwait(false);
            await _dbTransaction.DisposeAsync().ConfigureAwait(false);
            _dbTransaction = null;
            await CloseConnection().ConfigureAwait(false);
        }
    }

    public async Task RollbackTransaction()
    {
        if (_dbTransaction != null)
        {
            await _dbTransaction.RollbackAsync().ConfigureAwait(false);
            await _dbTransaction.DisposeAsync().ConfigureAwait(false);
            _dbTransaction = null;
            await CloseConnection().ConfigureAwait(false);
        }
    }

    public DbTransaction GetDbTransaction()
    {
        return _dbTransaction ?? throw new InvalidOperationException("Call StartTransaction before Get");
    }

    private string GetColumnInfoSql(string tableName)
    {
        return $"""
                SELECT
                    c.column_name,
                    c.data_type,
                    c.character_maximum_length,
                case
                    when c.is_nullable = 'YES' then 1
                    else 0
                end as is_nullable,
                CASE
                     WHEN COLUMNPROPERTY(object_id(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN 1
                     ELSE 0
                end AS is_identity
                FROM
                    INFORMATION_SCHEMA.COLUMNS c
                WHERE
                    c.table_name = '{tableName}';
                """;
    }

    private string GetColumnConstraintsSql(string tableName)
    {
        return $"""
                SELECT
                    tc.CONSTRAINT_NAME as constraint_name,
                    tc.CONSTRAINT_TYPE as constraint_type,
                    c.column_name,
                    CASE WHEN tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE') THEN 1 ELSE 0 END AS is_unique,
                    CASE WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN 1 ELSE 0 END AS is_foreign_key,
                    CASE WHEN tc.CONSTRAINT_TYPE = 'PRIMARY KEY' THEN 1 ELSE 0 END AS is_primary_key,
                    CASE 
                        WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN OBJECT_NAME(fk.referenced_object_id)
                        ELSE NULL
                    END AS foreign_table_name,
                    CASE 
                        WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN COL_NAME(fk.referenced_object_id, fkc.referenced_column_id)
                        ELSE NULL
                    END AS foreign_column_name
                FROM 
                    INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                LEFT JOIN 
                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON kcu.table_name = tc.table_name AND kcu.constraint_name = tc.constraint_name
                LEFT JOIN 
                    INFORMATION_SCHEMA.COLUMNS c ON c.table_name = kcu.table_name AND c.column_name = kcu.column_name                    
                LEFT JOIN 
                    sys.foreign_keys fk ON tc.CONSTRAINT_NAME = fk.name
                LEFT JOIN 
                    sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                WHERE 
                    c.table_name = '{tableName}';
                """;
    }
}