using Planto.Column;

namespace Planto.Table;

internal class TableInfo(string tableName)
{
    public string TableName { get; set; } = tableName;

    public List<ColumnInfo> ColumnInfos { get; set; } = [];
}