using FluentAssertions;
using Planto.Column.ColumnCheckSolver;
using Xunit;

namespace Planto.Test;

public class PlantoColumnCheckTests
{
    [Fact]
    public void ColumnCheckExpression_GetAllValues()
    {
        // Arrange
        var columnCheckExpression = new ColumnCheckExpression
        {
            Value = null,
            Children =
            [
                new ColumnCheckExpression
                {
                    Value = 1,
                    Children =
                    [
                        new ColumnCheckExpression()
                        {
                            Value = 3
                        },
                    ]
                },
                new ColumnCheckExpression()
                {
                    Value = 4
                }
            ]
        };

        // Act && Assert
        columnCheckExpression.GetAllValues().Should().BeEquivalentTo(new List<object?> { null, 1, 3, 4 });
    }
}