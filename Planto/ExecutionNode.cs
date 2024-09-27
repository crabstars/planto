using System.Collections.Concurrent;
using Planto.Column;

namespace Planto;

public class ExecutionNode
{
    public required string TableName { get; set; }
    public string? InsertStatement { get; set; }
    public ConcurrentBag<ExecutionNode> Children { get; set; } = [];
    public ExecutionNode? Parent { get; set; }
    public object? DbEntityId { get; set; }
    public required TableInfo TableInfo { get; set; }
}