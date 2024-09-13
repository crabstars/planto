namespace Planto.DatabaseImplementation.DataTypes;

public class HierarchyId : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "0x0";
    }
}