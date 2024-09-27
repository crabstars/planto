using System.ComponentModel.DataAnnotations.Schema;

namespace Planto.Column;

public class ColumnConstraint
{
    [Column("column_name")] public string ColumnName { get; set; } = string.Empty;

    [Column("constraint_type")] public ConstraintType ConstraintType { get; set; }

    [Column("constraint_name")] public string ConstraintName { get; set; } = string.Empty;

    [Column("is_primary_key")] public bool IsPrimaryKey { get; set; }

    [Column("is_foreign_key")] public bool IsForeignKey { get; set; }

    [Column("is_unique")] public bool IsUnique { get; set; }

    [Column("foreign_table_name")] public string? ForeignTableName { get; set; }

    [Column("foreign_column_name")] public string? ForeignColumnName { get; set; }
}