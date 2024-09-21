namespace Planto.DatabaseImplementation.MsSql.DataTypes;

public interface IMsSqlDataType
{
    public string GetDefaultValue();

    public string GetRandomValue();
}