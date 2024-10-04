using System.ComponentModel.DataAnnotations.Schema;
using Planto.Column.ColumnCheckSolver;

namespace Planto.Column;

internal class ColumnCheck
{
    [Column("column_name")] public string ColumnName { get; set; } = string.Empty;

    [Column("check_clause")] public string CheckClause { get; set; } = string.Empty;

    public ColumnCheckExpression? ParsedColumnCheck { get; set; }
}