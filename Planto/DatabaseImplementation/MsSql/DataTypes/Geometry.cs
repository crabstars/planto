namespace Planto.DatabaseImplementation.DataTypes;

public class Geometry
{
    public static string GetDefaultValue => "geometry::STGeomFromText('POINT(0 0)', 0)";
}