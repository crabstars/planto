using System.Globalization;
using Planto.DatabaseImplementation.SqlServer.DataTypes;
using Planto.Extensions;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation;

internal static class SqlValueGeneration
{
    private static string CreateString(ValueGeneration valueGeneration, int length)
    {
        return valueGeneration == ValueGeneration.Default
            ? string.Empty
            : $"{Guid.NewGuid().ToString().Truncate(length)}";
    }
    
    private static object CreateShort(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : Convert.ToInt32(new Random().NextDouble() * 100);
    }

    private static int CreateInt(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : Convert.ToInt32(new Random().NextDouble() * 1000000);
    }

    private static long CreateLong(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : (long)(new Random().NextDouble() * 1000000);
    }

    private static double CreateDouble(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : new Random().NextDouble() * 1000000;
    }

    private static decimal CreateDecimal(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : Convert.ToDecimal(new Random().NextDouble() * 1000000);
    }

    private static float CreateFloat(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : (float)(new Random().NextDouble() * 1000000);
    }

    private static bool CreateBool(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? default
            : new Random().NextDouble() >= 0.5;
    }

    private static DateTime CreateDateTime(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? DateTime.Now
            : DateTime.Now.AddDays(new Random().Next(-100, 100));
    }

    private static TimeSpan CreateTimeSpan(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? TimeSpan.Zero
            : TimeSpan.FromMinutes(new Random().Next(0, 1440));
    }

    private static DateTimeOffset CreateDateTimeOffset(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default
            ? DateTime.Now
            : DateTimeOffset.Now.AddDays(new Random().Next(-100, 100));
    }

    private static Guid CreateGuid(ValueGeneration valueGeneration)
    {
        return valueGeneration == ValueGeneration.Default ? Guid.Empty : new Guid();
    }

    public static object? CreateValueForMsSql(Type? type, ValueGeneration valueGeneration, int size) => type switch
    {
        _ when type == typeof(string) => $"'{CreateString(valueGeneration, size)}'",
        _ when type == typeof(short) => CreateShort(valueGeneration),
        _ when type == typeof(int) => CreateInt(valueGeneration),
        _ when type == typeof(long) => CreateLong(valueGeneration),
        _ when type == typeof(float) => CreateFloat(valueGeneration).ToString(CultureInfo.InvariantCulture),
        _ when type == typeof(double) => CreateDouble(valueGeneration).ToString(CultureInfo.InvariantCulture),
        _ when type == typeof(decimal) => CreateDecimal(valueGeneration).ToString(CultureInfo.InvariantCulture),
        _ when type == typeof(bool) => CreateBool(valueGeneration) ? "0" : "1",
        _ when type == typeof(DateTime) => $"'{CreateDateTime(valueGeneration):yyyy-MM-dd HH:mm:ss}'",
        _ when type == typeof(DateTimeOffset) => $"'{CreateDateTimeOffset(valueGeneration):yyyy-MM-dd HH:mm:ss zzz}'",
        _ when type == typeof(TimeSpan) => $"'{CreateTimeSpan(valueGeneration):hh\\:mm\\:ss}'",
        _ when type == typeof(byte[]) => valueGeneration == ValueGeneration.Default
            ? new HexByteArray().GetDefaultValue()
            : new HexByteArray().GetRandomValue(size),
        _ when type == typeof(HierarchyId) => valueGeneration == ValueGeneration.Default
            ? new HexByteArray().GetDefaultValue()
            : new HexByteArray().GetRandomValue(size),
        _ when type == typeof(Geography) => new Geography().GetDefaultValue(), // TODO
        _ when type == typeof(Geometry) => new Geometry().GetDefaultValue(), // TODO
        _ when type == typeof(Guid) => $"'{CreateGuid(valueGeneration)}'",
        { IsValueType: true } => Activator.CreateInstance(type),
        _ => "NULL"
    };

    public static object MapValueForMsSql(object? value)
    {
        return value switch
        {
            string s => $"'{s}'",
            null => "NULL",
            bool b => b ? "1" : "0",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss"),
            TimeSpan timeSpan => $"'{timeSpan:hh\\:mm\\:ss}'",
            _ => value
        };
    }
}