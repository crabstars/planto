namespace Planto.Attributes;

public class ColumnNameAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
}