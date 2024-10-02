namespace Planto.Attributes;

public class TableNameAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
}