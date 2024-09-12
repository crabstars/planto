using System.ComponentModel.DataAnnotations.Schema;

namespace Planto;

public class ColumnInfo
{
    [Column("column_name")] public string Name { get; set; }

    // public string DataType { get; set; }
    [Column("data_type")] public Type DataType { get; set; }

    [Column("is_nullable")] public bool IsNullable { get; set; }

    [Column("is_primary_key")] public bool IsPrimaryKey { get; set; }

    [Column("is_identity")] public bool? IsIdentity { get; set; }

    [Column("is_foreign_key")] public bool IsForeignKey { get; set; }

    [Column("foreign_table_name")] public string? ForeignTableName { get; set; }

    [Column("foreign_column_name")] public string? ForeignColumnName { get; set; }
}