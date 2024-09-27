namespace Planto.Column;

public class TableInfo(string tableName)
{
    public string TableName { get; set; } = tableName;

    public List<ColumnInfo> ColumnInfos { get; set; } = [];
}