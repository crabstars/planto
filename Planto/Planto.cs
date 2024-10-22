using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.CompilerServices;
using Planto.Column;
using Planto.DatabaseImplementation;
using Planto.DatabaseImplementation.SqlServer;
using Planto.Exceptions;
using Planto.ExecutionTree;
using Planto.OptionBuilder;
using Planto.Table;

[assembly: InternalsVisibleTo("Planto.Test")]

namespace Planto;

/// <summary>
/// Initializes a new instance of the Planto class
/// </summary>
public class Planto : IAsyncDisposable
{
    private readonly IDatabaseProviderHelper _dbProviderHelper;
    private readonly PlantoOptions _options;
    private readonly IDictionary<string, IEnumerable<ColumnInfo>>? _cachedColumns;
    private readonly ColumnHelper _columnHelper;
    private readonly ExecutionTreeBuilder _executionTreeBuilder;

    public Planto(string connectionString, DbmsType dbmsType, Action<PlantoOptionBuilder>? configureOptions = null)
    {
        var optionsBuilder = new PlantoOptionBuilder();
        configureOptions?.Invoke(optionsBuilder);
        var connectionHandler = new DatabaseConnectionHandler.DatabaseConnectionHandler(connectionString);
        _options = optionsBuilder.Build();
        if (_options.CacheColumns)
            _cachedColumns = new Dictionary<string, IEnumerable<ColumnInfo>>();
        _dbProviderHelper = dbmsType switch
        {
            DbmsType.MsSql => new MsSql(connectionHandler, _options.TableSchema),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + dbmsType)
        };
        _columnHelper = new ColumnHelper(_dbProviderHelper);
        _executionTreeBuilder = new ExecutionTreeBuilder(this, _options.MaxDegreeOfParallelism);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbProviderHelper.DisposeAsync();
    }

    /// <summary>
    /// Creates an entity in the given table
    /// </summary>
    /// <param name="tableName">table in which an entity should be created</param>
    /// <param name="data">custom values for insert</param>
    /// <typeparam name="TCast">return type of the primary key</typeparam>
    /// <returns>PrimaryKey of the created entity</returns>
    public async Task<TCast> CreateEntity<TCast>(string tableName, params object?[] data)
    {
        try
        {
            return await _dbProviderHelper.CreateEntity<TCast>(await _executionTreeBuilder.CreateExecutionTree(tableName, null),
                _options, data);
        }
        catch (Exception e)
        {
            throw new PlantoDbException("Exception occured, rollback done: " + e.Message, e);
        }
    }

}