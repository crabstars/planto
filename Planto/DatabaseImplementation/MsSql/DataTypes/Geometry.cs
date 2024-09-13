namespace Planto.DatabaseImplementation.DataTypes;

public class Geometry : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "geometry::STGeomFromText('POINT(0 0)', 0)";
    }
}