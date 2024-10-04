namespace Planto;

public class ColumnCheckClause(string checkClause, string columnName, string tableName)
{
    public string CheckClause { get; set; } = checkClause;
    public string ColumnName { get; set; } = columnName;

    public string TableName { get; set; } = tableName;

    public override string ToString()
    {
        return $"{TableName} | {ColumnName} | {CheckClause}";
    }
}