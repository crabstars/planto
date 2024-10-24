using System.Collections.Concurrent;
using Planto.Table;

namespace Planto.ExecutionTree;

internal class ExecutionNode
{
    public required string TableName { get; set; }
    public string? InsertStatement { get; set; }
    public ConcurrentBag<ExecutionNode> Children { get; set; } = [];
    public ExecutionNode? Parent { get; set; }
    public object? DbEntityId { get; set; }
    public required TableInfo TableInfo { get; set; }
}