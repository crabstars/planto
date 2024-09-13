namespace Planto.DatabaseImplementation.DataTypes;

public class Geography
{
    public static string GetDefaultValue => "geography::Point(0,0, 4326)";
}