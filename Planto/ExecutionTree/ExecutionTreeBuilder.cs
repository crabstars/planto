using Planto.Exceptions;

namespace Planto.ExecutionTree;

/// <summary>
/// Builds execution trees for managing relational dependencies between database tables.
/// </summary>
internal class ExecutionTreeBuilder
{
    private readonly int? _maxDegreeOfParallelism;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionTreeBuilder"/> class.
    /// </summary>
    /// <param name="planto">The <see cref="Planto"/> instance used to access database-related operations.</param>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism for asynchronous operations.</param>
    public ExecutionTreeBuilder(Planto planto, int? maxDegreeOfParallelism)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
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
            TableInfo = await _planto.CreateTableInfo(tableName),
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