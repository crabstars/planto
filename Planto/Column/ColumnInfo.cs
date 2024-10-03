using System.ComponentModel.DataAnnotations.Schema;

namespace Planto.Column;

internal class ColumnInfo
{
    [Column("column_name")] public string ColumnName { get; set; } = string.Empty;

    [Column("data_type")] public Type? DataType { get; set; }

    [Column("character_maximum_length")] public int MaxCharLen { get; set; }

    [Column("is_nullable")] public bool IsNullable { get; set; }

    [Column("is_identity")] public bool? IsIdentity { get; set; }

    [Column("is_computed")] public bool IsComputed { get; set; }

    public List<ColumnConstraint> ColumnConstraints { get; set; } = [];

    public List<ColumnCheck> ColumnChecks { get; set; } = [];

    public bool IsPrimaryKey => ColumnConstraints.Any(c => c.IsPrimaryKey);

    public bool IsForeignKey => ColumnConstraints.Any(c => c.IsForeignKey);
}