using System.Runtime.CompilerServices;
using Planto.Column;
using Planto.DatabaseImplementation;
using Planto.DatabaseImplementation.SqlServer;
using Planto.Exceptions;
using Planto.ExecutionTree;
using Planto.OptionBuilder;

[assembly: InternalsVisibleTo("Planto.Test")]

namespace Planto;

/// <summary>
/// Initializes a new instance of the Planto class
/// </summary>
public class Planto : IAsyncDisposable
{
    private readonly IDatabaseProviderHelper _dbProviderHelper;
    private readonly ExecutionTreeBuilder _executionTreeBuilder;
    private readonly PlantoOptions _options;

    public Planto(string connectionString, DbmsType dbmsType, Action<PlantoOptionBuilder>? configureOptions = null)
    {
        var optionsBuilder = new PlantoOptionBuilder();
        configureOptions?.Invoke(optionsBuilder);
        var connectionHandler = new DatabaseConnectionHandler.DatabaseConnectionHandler(connectionString);
        _options = optionsBuilder.Build();
        _dbProviderHelper = dbmsType switch
        {
            DbmsType.MsSql => new MsSql(connectionHandler, _options.TableSchema),
            _ => throw new ArgumentException(
                "Only NpgsqlConnection and SqlConnection are supported right now.\nConnection Type: "
                + dbmsType)
        };
        var columnHelper = new ColumnHelper(_dbProviderHelper, _options.CacheColumns);
        _executionTreeBuilder = new ExecutionTreeBuilder(_options.MaxDegreeOfParallelism, columnHelper,
            _options.ColumnCheckValueGenerator);
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
            return await _dbProviderHelper.CreateEntity<TCast>(
                await _executionTreeBuilder.CreateExecutionTree(tableName, null),
                _options, data);
        }
        catch (Exception e)
        {
            throw new PlantoDbException("Exception occured, rollback done: " + e.Message, e);
        }
    }

    /// <summary>
    /// Can be used to determine which custom data should be provided
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>Returns check clauses to all related tables which are needed to create an entry for the given table name</returns>
    public async Task<IEnumerable<ColumnCheckClause>> AnalyzeColumnChecks(string tableName)
    {
        var executionTree = await _executionTreeBuilder.CreateExecutionTree(tableName, null);
        return GetColumnChecks(executionTree);
    }

    private static IEnumerable<ColumnCheckClause> GetColumnChecks(ExecutionNode executionNode)
    {
        return executionNode.TableInfo.ColumnInfos.SelectMany(ci =>
                ci.ColumnChecks.Select(cc =>
                    new ColumnCheckClause(cc.CheckClause, cc.ColumnName, executionNode.TableName)))
            .Concat(executionNode.Children.SelectMany(GetColumnChecks));
    }
}