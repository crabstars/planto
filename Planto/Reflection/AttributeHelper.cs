using Planto.Attributes;

namespace Planto.Reflection;

/// <summary>
/// Helper methods for retrieving data based on table and column names.
/// </summary>
internal static class AttributeHelper
{
    /// <summary>
    /// Finds an object that matches the given table name or has a <see cref="TableNameAttribute"/> with a matching name.
    /// </summary>
    /// <param name="tableName">The table name to match.</param>
    /// <param name="data">Objects to search through.</param>
    /// <returns>The matching object or <c>null</c>.</returns>
    public static object? GetCustomDataMatchesCurrentTable(string tableName, params object?[] data)
    {
        if (data.Length == 0) return null;
        foreach (var o in data)
        {
            if (o is null) continue;
            if (o.GetType().Name == tableName)
                return o;

            foreach (var customAttribute in Attribute.GetCustomAttributes(o.GetType()))
            {
                if (customAttribute is TableNameAttribute ta && ta.Name == tableName)
                {
                    return o;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the value of a property by its name or <see cref="ColumnNameAttribute"/> from the given object.
    /// </summary>
    /// <param name="data">The object to inspect.</param>
    /// <param name="columnName">The column name to match.</param>
    /// <returns>The property's value or <c>null</c>.</returns>
    public static object? GetValueToCustomData(object? data, string columnName)
    {
        if (data is null) return null;

        var properties = data.GetType().GetProperties();

        foreach (var property in properties)
        {
            if (property.Name == columnName)
                return property.GetValue(data);

            foreach (var customAttribute in Attribute.GetCustomAttributes(property))
            {
                if (customAttribute is ColumnNameAttribute ca && ca.Name == columnName)
                {
                    return property.GetValue(data);
                }
            }
        }

        return null;
    }
}

