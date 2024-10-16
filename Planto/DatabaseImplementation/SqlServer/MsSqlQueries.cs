using Microsoft.IdentityModel.Tokens;

namespace Planto.DatabaseImplementation.SqlServer;

internal class MsSqlQueries(string? optionsTableSchema)
{
    internal string GetColumnInfoSql(string tableName)
    {
        return $"""
                SELECT
                    c.column_name,
                    c.data_type,
                    c.character_maximum_length,
                    CASE
                        WHEN c.is_nullable = 'YES' THEN 1
                        ELSE 0
                    END AS is_nullable,
                    CASE
                        WHEN COLUMNPROPERTY(object_id(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN 1
                        ELSE 0
                    END AS is_identity,
                    CASE
                        WHEN cc.is_computed = 1 THEN 1
                        ELSE 0
                    END AS is_computed
                FROM
                    INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN
                    sys.columns sc
                    ON sc.object_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME)
                    AND sc.name = c.COLUMN_NAME
                LEFT JOIN
                    sys.computed_columns cc
                    ON sc.object_id = cc.object_id
                    AND sc.column_id = cc.column_id
                WHERE
                    c.TABLE_NAME = '{tableName}'
                """ + FilterSchema("c") + ";";
    }

    internal string GetColumnConstraintsSql(string tableName)
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
                """ + FilterSchema("c") + ";";
    }

    internal string GetColumnChecksSql(string tableName)
    {
        return $"""
                SELECT 
                    DISTINCT
                    ccu.COLUMN_NAME AS column_name,
                    cc.CONSTRAINT_NAME AS constraint_name,
                    cc.CHECK_CLAUSE AS check_clause
                FROM 
                    INFORMATION_SCHEMA.CHECK_CONSTRAINTS cc
                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu 
                        ON cc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.TABLES t 
                        ON ccu.TABLE_NAME = t.TABLE_NAME
                WHERE 
                    t.TABLE_NAME = '{tableName}'
                """ + FilterSchema("t") + ";";
    }

    private string FilterSchema(string tableName)
    {
        return !optionsTableSchema.IsNullOrEmpty()
            ? $" AND {tableName}.TABLE_SCHEMA = '{optionsTableSchema}'"
            : string.Empty;
    }
}