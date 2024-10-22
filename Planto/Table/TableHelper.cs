using Planto.Column;

namespace Planto.Table;

public class TableHelper
{
    private readonly ColumnHelper _columnHelper;
    public TableHelper()
    {
        _columnHelper = new ColumnHelper();
    }
    
    internal async Task<TableInfo> CreateTableInfo(string tableName)
    {
        var tableInfo = new TableInfo(tableName);

        var columInfos = (await _columnHelper.GetColumInfos(tableName).ConfigureAwait(false)).ToList();
        await _columnHelper.AddColumConstraints(columInfos, tableName).ConfigureAwait(false);
        if (_options.ColumnCheckValueGenerator)
            await _columnHelper.AddColumnChecks(columInfos, tableName).ConfigureAwait(false);
        tableInfo.ColumnInfos.AddRange(columInfos);

        // Validate table
        if (tableInfo.ColumnInfos.Any(c => c.ColumnConstraints.Where(cc => cc.IsForeignKey).GroupBy(cc => cc.ColumnName)
                .Any(gc => gc.Count() > 1)))
            throw new NotSupportedException("Only tables with single foreign key constraints are supported.");
        return tableInfo;
    }
}