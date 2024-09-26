namespace Planto.Column;

public class TableInfo(string tableName)
{
    public string TableName { get; set; } = tableName;

    public List<ColumnInfo> ColumnInfos { get; set; } = [];

    public List<ColumnConstraint> ColumnConstraints { get; set; } = [];

    public bool ColumnIsPrimaryKey(string columnName)
    {
        return ColumnConstraints.Any(c => c.ColumnName == columnName && c.IsPrimaryKey);
    }

    public bool ColumnIsForeignKey(string columnName)
    {
        return ColumnConstraints.Any(c => c.ColumnName == columnName && c.IsForeignKey);
    }
}