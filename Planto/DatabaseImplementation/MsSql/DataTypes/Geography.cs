namespace Planto.DatabaseImplementation.DataTypes;

public class Geography : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "geography::Point(0,0, 4326)";
    }
}