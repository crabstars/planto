namespace Planto.DatabaseImplementation.SqlServer.DataTypes;

internal class HierarchyId : IMsSqlDataType
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