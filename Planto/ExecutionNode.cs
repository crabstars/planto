using System.Collections.Concurrent;

namespace Planto;

public class ExecutionNode
{
    public required string TableName { get; set; }
    public string? InsertStatement { get; set; }
    public ConcurrentBag<ExecutionNode> Children { get; set; } = [];
    public object? DbEntityId { get; set; }
    public required List<ColumnInfo> ColumnInfos { get; set; }
}