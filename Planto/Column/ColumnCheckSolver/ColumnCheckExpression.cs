namespace Planto.Column.ColumnCheckSolver;

/// <summary>
/// Only supports expressions where the value for a single column is to be checked
/// </summary>
internal class ColumnCheckExpression
{
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// value which solves the expression, null represents that the expression can not be solved right now
    /// </summary>
    public object? Value { get; set; }

    public ColumnCheckExpression? Parent { get; set; }
    public List<ColumnCheckExpression> Children { get; set; } = [];

    public List<object?> GetAllValues()
    {
        return new List<object?> { Value }.Concat(
            Children.SelectMany(c => c.GetAllValues())).ToList();
    }
}