using System.Text.RegularExpressions;
using Planto.Exceptions;

namespace Planto.Column.ColumnCheckSolver;

/// <summary>
/// most of the Parser needs to be rewritten,
/// supports only some basic Check Clause cases
/// </summary>
internal class ColumnCheckSyntaxParser
{
    private readonly List<string> _returnValueOperations = [GreaterOrEqual, LessOrEqual, Equal];

    public ColumnCheckExpression Parse(string checkClause, ColumnCheckExpression? parent = null)
    {
        // BUG: should only count brackets which are not surrounded by '...'
        checkClause = OffsetBrackets(checkClause);

        var expressionParts = checkClause.Split([And, Or], StringSplitOptions.RemoveEmptyEntries);
        var columnCheckExpression = new ColumnCheckExpression
        {
            Parent = parent,
            Expression = checkClause,
            Value = SolveExpression(checkClause)
        };

        if (expressionParts.Length > 1)
        {
            columnCheckExpression.Children = expressionParts.Select(ep =>
                Parse(ep.Trim(), columnCheckExpression)).ToList();
        }

        return columnCheckExpression;
    }

    private static string OffsetBrackets(string checkClause)
    {
        var countOpeningBrackets = checkClause.Count(c => c == '(');
        var countClosingBrackets = checkClause.Count(c => c == ')');
        while (countOpeningBrackets != countClosingBrackets)
        {
            if (countClosingBrackets > countOpeningBrackets)
                checkClause = checkClause.Remove(checkClause.LastIndexOf(')'), 1);
            if (countOpeningBrackets > countClosingBrackets)
                checkClause = checkClause.Remove(checkClause.IndexOf('('), 1);
            countOpeningBrackets = checkClause.Count(c => c == '(');
            countClosingBrackets = checkClause.Count(c => c == ')');
        }

        return checkClause;
    }

    private object? SolveExpression(string expression)
    {
        expression = expression.Trim();
        // Only supports expressions where the value for a single column is to be checked
        // Columns are represented like "[column_name]"
        if (expression.Count(c => c == '[') != 1)
            return null;

        foreach (var operation in _returnValueOperations)
        {
            if (!expression.Contains(operation)) continue;

            var parts = expression.Split(operation);
            if (parts.Length != 2)
                throw new ColumnCheckException("Expression could not be splitted into 2 parts: " + expression);
            var checkValue = parts[0].Contains('[') ? parts[1] : parts[0];
            if (checkValue.Contains('\''))
                return checkValue.TrimStart('(').TrimEnd(')');
            if (checkValue.Contains('('))
                return checkValue.TrimStart('(').TrimEnd(')');
        }

        if (expression.Contains(IsNull))
            return "NULL";

        if (expression.Contains(Like))
        {
            var parts = expression.Split(Like);
            if (parts.Length != 2)
                throw new ColumnCheckException("Expression could not be splitted into 2 parts: " + expression);
            var checkValue = parts[0].Contains('[') ? parts[1] : parts[0];

            return Regex.Replace(checkValue, @"[%_\[\]^\-]", "", RegexOptions.Compiled).TrimStart('(').TrimEnd(')')
                .Trim();
        }

        return null;
    }

    // ReSharper disable UnusedMember.Local
    private const string And = "AND";
    private const string Or = "OR";
    private const string GreaterOrEqual = ">=";
    private const string LessOrEqual = "<=";
    private const string NotEqual = "<>";
    private const string Equal = "=";
    private const string Greater = ">";
    private const string Less = "<";
    private const string Like = "like";
    private const string Length = "len";
    private const string IsNull = "IS NULL";

    private const string IsNotNull = "IS NOT NULL";
    // ReSharper restore UnusedMember.Local
}