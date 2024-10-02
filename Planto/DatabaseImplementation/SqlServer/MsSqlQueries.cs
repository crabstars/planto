using Microsoft.IdentityModel.Tokens;

namespace Planto.DatabaseImplementation.SqlServer;

internal class MsSqlQueries(string? optionsTableSchema)
{
    public string GetColumnInfoSql(string tableName)
    {
        return $"""
                SELECT
                    c.column_name,
                    c.data_type,
                    c.character_maximum_length,
                case
                    when c.is_nullable = 'YES' then 1
                    else 0
                end as is_nullable,
                CASE
                     WHEN COLUMNPROPERTY(object_id(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN 1
                     ELSE 0
                end AS is_identity
                FROM
                    INFORMATION_SCHEMA.COLUMNS c
                WHERE
                    c.TABLE_NAME = '{tableName}'
                """ + FilterSchema() + ";";
    }

    public string GetColumnConstraintsSql(string tableName)
    {
        return $"""
                SELECT
                    tc.CONSTRAINT_NAME as constraint_name,
                    tc.CONSTRAINT_TYPE as constraint_type,
                    c.column_name,
                    CASE WHEN tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE') THEN 1 ELSE 0 END AS is_unique,
                    CASE WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN 1 ELSE 0 END AS is_foreign_key,
                    CASE WHEN tc.CONSTRAINT_TYPE = 'PRIMARY KEY' THEN 1 ELSE 0 END AS is_primary_key,
                    CASE 
                        WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN OBJECT_NAME(fk.referenced_object_id)
                        ELSE NULL
                    END AS foreign_table_name,
                    CASE 
                        WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN COL_NAME(fk.referenced_object_id, fkc.referenced_column_id)
                        ELSE NULL
                    END AS foreign_column_name
                FROM 
                    INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                LEFT JOIN 
                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON kcu.table_name = tc.table_name AND kcu.constraint_name = tc.constraint_name
                LEFT JOIN 
                    INFORMATION_SCHEMA.COLUMNS c ON c.table_name = kcu.table_name AND c.column_name = kcu.column_name                    
                LEFT JOIN 
                    sys.foreign_keys fk ON tc.CONSTRAINT_NAME = fk.name
                LEFT JOIN 
                    sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                WHERE 
                    c.TABLE_NAME = '{tableName}'
                """ + FilterSchema() + ";";
    }

    private string FilterSchema()
    {
        return !optionsTableSchema.IsNullOrEmpty() ? $" AND c.TABLE_SCHEMA = '{optionsTableSchema}'" : string.Empty;
    }
}