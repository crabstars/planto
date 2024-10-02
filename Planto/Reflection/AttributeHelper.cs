using System.ComponentModel.DataAnnotations.Schema;
using Planto.Attributes;

namespace Planto.Reflection;

internal static class AttributeHelper
{
    public static bool CustomDataMatchesCurrentTable(object? data, string tableName)
    {
        if (data is null) return false;

        if (data.GetType().Name == tableName)
            return true;

        foreach (var customAttribute in Attribute.GetCustomAttributes(data.GetType()))
        {
            if (customAttribute is TableNameAttribute ta)
            {
                return tableName == ta.Name;
            }
        }

        return false;
    }

    public static object? GetValueToCustomData(object? data, string columnName)
    {
        if (data is null) return false;

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

        foreach (var customAttribute in Attribute.GetCustomAttributes(data.GetType()))
        {
            if (customAttribute is TableAttribute ta)
            {
                return columnName == ta.Name;
            }
        }

        return false;
    }
}