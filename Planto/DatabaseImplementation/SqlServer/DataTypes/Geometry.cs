namespace Planto.DatabaseImplementation.SqlServer.DataTypes;

internal class Geometry : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "geometry::STGeomFromText('POINT(0 0)', 0)";
    }

    public string GetRandomValue(int size)
    {
        throw new NotImplementedException();
    }
}