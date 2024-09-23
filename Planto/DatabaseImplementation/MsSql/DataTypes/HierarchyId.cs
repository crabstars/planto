namespace Planto.DatabaseImplementation.MsSql.DataTypes;

public class HierarchyId : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "0x";
    }

    public string GetRandomValue(int size)
    {
        throw new NotImplementedException();
    }
}