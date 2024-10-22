using Planto.Column;
using Planto.Exceptions;
using Planto.Table;

namespace Planto.ExecutionTree;

/// <summary>
/// Builds execution trees for managing relational dependencies between database tables.
/// </summary>
internal class ExecutionTreeBuilder
{
    private readonly ColumnHelper _columnHelper;
    private readonly int? _maxDegreeOfParallelism;
    private readonly bool _optionsColumnCheckValueGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionTreeBuilder"/> class.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism for asynchronous operations.</param>
    /// <param name="columnHelper"></param>
    /// <param name="optionsColumnCheckValueGenerator"></param>
    public ExecutionTreeBuilder(int? maxDegreeOfParallelism, ColumnHelper columnHelper,
        bool optionsColumnCheckValueGenerator)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _columnHelper = columnHelper;
        _optionsColumnCheckValueGenerator = optionsColumnCheckValueGenerator;
    }

    /// <summary>
    /// Asynchronously creates an execution tree for the specified table, managing its relationships to other tables.
    /// </summary>
    /// <param name="tableName">The name of the table for which to create the execution tree.</param>
    /// <param name="parent">The parent execution node, if any, representing the parent table in the hierarchy.</param>
    /// <returns>A task with a result of <see cref="ExecutionNode"/></returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching column name is found while creating an execution node.
    /// </exception>
    /// <exception cref="CircularDependencyException">
    /// Thrown when a circular dependency is detected in the execution tree.
    /// </exception>
    internal async Task<ExecutionNode> CreateExecutionTree(string tableName, ExecutionNode? parent)
    {
        var newExecutionNode = new ExecutionNode
        {
            TableName = tableName,
            TableInfo = await CreateTableInfo(tableName),
            Parent = parent
        };
        CheckCircularDependency(tableName, parent);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism ?? 3
        };

        // IsNullable false ignores fk for self referencing tables or non-hierarchical tables
        await Parallel.ForEachAsync(
            newExecutionNode.TableInfo.ColumnInfos.Where(c => c is { IsForeignKey: true, IsNullable: false }),
            parallelOptions,
            async (columnInfo, token) =>
            {
                token.ThrowIfCancellationRequested();
                // TODO Because a column can be a fk for multiple tables we should create n nodes but the ids has to be same
                newExecutionNode.Children.Add(
                    await CreateExecutionTree(
                        columnInfo.ColumnConstraints.Where(cc => cc.IsForeignKey)
                            .Select(cc => cc.ForeignTableName).FirstOrDefault()
                        ?? throw new InvalidOperationException("No Matching column_name when creating executionNode"),
                        newExecutionNode));
            }).ConfigureAwait(false);

        return newExecutionNode;
    }

    internal async Task<TableInfo> CreateTableInfo(string tableName)
    {
        var tableInfo = new TableInfo(tableName);

        var columInfos = (await _columnHelper.GetColumInfos(tableName).ConfigureAwait(false)).ToList();
        await _columnHelper.AddColumConstraints(columInfos, tableName).ConfigureAwait(false);
        if (_optionsColumnCheckValueGenerator)
            await _columnHelper.AddColumnChecks(columInfos, tableName).ConfigureAwait(false);
        tableInfo.ColumnInfos.AddRange(columInfos);

        // Validate table
        if (tableInfo.ColumnInfos.Any(c => c.ColumnConstraints.Where(cc => cc.IsForeignKey).GroupBy(cc => cc.ColumnName)
                .Any(gc => gc.Count() > 1)))
            throw new NotSupportedException("Only tables with single foreign key constraints are supported.");
        return tableInfo;
    }


    /// <summary>
    /// Checks for circular dependencies between tables in the execution tree.
    /// </summary>
    /// <param name="tableName">The name of the table to check for circular dependencies.</param>
    /// <param name="parent">The parent node in the current execution tree structure.</param>
    /// <exception cref="CircularDependencyException">
    /// Thrown when a circular dependency is detected for the specified table.
    /// </exception>
    private static void CheckCircularDependency(string tableName, ExecutionNode? parent)
    {
        var prevNode = parent;
        while (prevNode is not null)
        {
            if (prevNode.TableName == tableName)
                throw new CircularDependencyException(
                    $"Circular dependency detected for table '{tableName}' where foreign key is not nullable");
            prevNode = prevNode.Parent;
        }
    }
}