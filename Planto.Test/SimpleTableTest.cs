using System.Data.Common;
using Xunit;

namespace Planto.Test;

public class SimpleTableTest
{
    [Fact]
    public void TestColumnTypes()
    {
        var planto = new Planto();

        var res = planto.GetColumnInfo("Host=localhost;Port=5433;Username=postgres;Password=example;Database=exampledb", "orders");
        Assert.NotNull(res);
    }
}