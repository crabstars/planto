namespace Planto.DatabaseImplementation.SqlServer.DataTypes;

internal interface IMsSqlDataType
{
    public string GetDefaultValue();

    public string GetRandomValue(int size);
}