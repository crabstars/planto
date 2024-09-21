namespace Planto.DatabaseImplementation.MsSql.DataTypes;

public class HierarchyId : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "0x";
    }

    public string GetRandomValue()
    {
        throw new NotImplementedException();
    }
}