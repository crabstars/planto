using Planto.Attributes;

namespace Planto.Reflection;

internal static class AttributeHelper
{
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