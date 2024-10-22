using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Planto.Column.ColumnCheckSolver;
using Planto.DatabaseImplementation;
using Planto.Exceptions;

namespace Planto.Column;

/// <summary>
/// Helper class for managing column-related operations, such as adding column checks and constraints.
/// </summary>
internal class ColumnHelper
{
    private const string ForeignKey = "FOREIGN KEY";
    private const string PrimaryKey = "PRIMARY KEY";
    private const string Unique = "UNIQUE";
    private const string Check = "CHECK";

    internal readonly IDictionary<string, IEnumerable<ColumnInfo>>? CachedColumns;
    private readonly IDatabaseProviderHelper _dbProviderHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnHelper"/> class.
    /// </summary>
    /// <param name="dbProviderHelper">The database provider helper used to interact with the database.</param>
    /// <param name="optionsCacheColumns"></param>
    internal ColumnHelper(IDatabaseProviderHelper dbProviderHelper, bool optionsCacheColumns)
    {
        if (optionsCacheColumns)
            CachedColumns = new Dictionary<string, IEnumerable<ColumnInfo>>();
        _dbProviderHelper = dbProviderHelper;
    }

    /// <summary>
    /// Asynchronously adds column checks to the provided list of column information for the specified table.
    /// </summary>
    /// <param name="columnInfos">The list of column information to which checks will be added.</param>
    /// <param name="tableName">The name of the table for which the column checks are being retrieved.</param>
    /// <returns>task</returns>
    /// <exception cref="PlantoDbException">Thrown when a column name cannot be found in the data reader.</exception>
    internal async Task AddColumnChecks(List<ColumnInfo> columnInfos, string tableName)
    {
        await using var dataReader = await _dbProviderHelper.GetColumnChecks(tableName);
        var columnCheckParser = new ColumnCheckSyntaxParser();

        while (await dataReader.ReadAsync().ConfigureAwait(false))
        {
            var columnCheck = new ColumnCheck();
            var properties = typeof(ColumnCheck).GetProperties();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;
                try
                {
                    if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                }
                catch (Exception e)
                {
                    throw new PlantoDbException("Could not found column name: " + columnName, e);
                }

                var value = dataReader[columnName];
                property.SetValue(columnCheck, Convert.ToString(value));
            }

            columnCheck.ParsedColumnCheck = columnCheckParser.Parse(columnCheck.CheckClause);
            columnInfos.Single(c => c.ColumnName == columnCheck.ColumnName).ColumnChecks
                .Add(columnCheck);
        }
    }

    /// <summary>
    /// Asynchronously adds column constraints to the provided list of column information for the specified table.
    /// </summary>
    /// <param name="columnInfos">The list of column information to which constraints will be added.</param>
    /// <param name="tableName">The name of the table for which the column constraints are being retrieved.</param>
    /// <returns>task</returns>
    /// <exception cref="PlantoDbException">Thrown when a column name cannot be found in the data reader.</exception>
    /// <exception cref="NotSupportedException">Thrown when an unsupported constraint type is encountered.</exception>
    internal async Task AddColumConstraints(List<ColumnInfo> columnInfos, string tableName)
    {
        await using var dataReader = await _dbProviderHelper.GetColumnConstraints(tableName);

        while (await dataReader.ReadAsync().ConfigureAwait(false))
        {
            var columnConstraint = new ColumnConstraint();
            var properties = typeof(ColumnConstraint).GetProperties();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;
                try
                {
                    if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                }
                catch (Exception e)
                {
                    throw new PlantoDbException("Could not found column name: " + columnName, e);
                }

                var value = dataReader[columnName];
                if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    property.SetValue(columnConstraint, Convert.ToBoolean(value));
                }
                else if (columnName == "constraint_type")
                {
                    switch (value)
                    {
                        case ForeignKey:
                            property.SetValue(columnConstraint, ConstraintType.ForeignKey);
                            break;
                        case PrimaryKey:
                            property.SetValue(columnConstraint, ConstraintType.PrimaryKey);
                            break;
                        case Unique:
                            property.SetValue(columnConstraint, ConstraintType.Unique);
                            break;
                        case Check:
                            property.SetValue(columnConstraint, ConstraintType.Check);
                            Console.WriteLine("Warning: CHECK constraints are not yet supported");
                            break;
                        default:
                            throw new NotSupportedException($"The type {value.GetType()} is not supported");
                    }
                }
                else
                {
                    property.SetValue(columnConstraint, Convert.ToString(value));
                }
            }

            columnInfos.Single(c => c.ColumnName == columnConstraint.ColumnName).ColumnConstraints
                .Add(columnConstraint);
        }
    }

    internal async Task<IEnumerable<ColumnInfo>> GetColumInfos(string tableName)
    {
        if (CachedColumns != null &&
            CachedColumns.TryGetValue(tableName, out var cachedColumns))
            return cachedColumns;

        var columnInfos = new List<ColumnInfo>();

        await using var dataReader = await _dbProviderHelper.GetColumInfos(tableName);

        // read one column info at the time
        while (await dataReader.ReadAsync().ConfigureAwait(false))
        {
            var columnInfo = new ColumnInfo();
            var properties = typeof(ColumnInfo).GetProperties();

            // add db values to column info
            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute == null) continue;
                var columnName = columnAttribute.Name ?? string.Empty;
                try
                {
                    if (dataReader.IsDBNull(dataReader.GetOrdinal(columnName))) continue;
                }
                catch (Exception e)
                {
                    throw new PlantoDbException("Could not found column name: " + columnName, e);
                }

                var value = dataReader[columnName];
                if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    property.SetValue(columnInfo, Convert.ToBoolean(value));
                }
                else if (property.PropertyType == typeof(int))
                {
                    property.SetValue(columnInfo, Convert.ToInt32(value));
                }
                else if (property.PropertyType == typeof(Type))
                {
                    property.SetValue(columnInfo,
                        _dbProviderHelper.MapToSystemType(Convert.ToString(value) ?? string.Empty));
                }
                else
                {
                    property.SetValue(columnInfo, Convert.ToString(value));
                }
            }

            columnInfos.Add(columnInfo);
        }

        CachedColumns?.Add(tableName, columnInfos);

        return columnInfos;
    }
}