namespace Planto.DatabaseImplementation.SqlServer.DataTypes;

internal class Geography : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "geography::Point(0,0, 4326)";
    }

    public string GetRandomValue(int size)
    {
        throw new NotImplementedException();
    }
}