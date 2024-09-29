namespace Planto.DatabaseImplementation.SqlServer.DataTypes;

internal class HexByteArray : IMsSqlDataType
{
    public string GetDefaultValue()
    {
        return "0x";
    }

    public string GetRandomValue(int size)
    {
        var random = new Random();
        size = size > 100 ? 100 : size;
        var byteArray = new byte[size];
        random.NextBytes(byteArray);
        return "0x" + BitConverter.ToString(byteArray).Replace("-", "");
    }
}