using System.Data.Common;
using Planto.ExecutionTree;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation;

internal interface IDatabaseProviderHelper : IAsyncDisposable
{
    /// <summary>
    /// returns the matching C# type to a sql type
    /// </summary>
    /// <param name="sqlType"></param>
    /// <returns>Type</returns>
    /// <exception cref="ArgumentException"></exception>
    Type MapToSystemType(string sqlType);

    /// <summary>
    /// Uses INFORMATION_SCHEMA to get ColumnName, DataType, CharMaxLen, Nullable, IsIdentity and IsComputed
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>DbDataReader</returns>
    Task<DbDataReader> GetColumInfos(string tableName);

    /// <summary>
    /// Uses INFORMATION_SCHEMA to get the column constraints for a table
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>DbDataReader</returns>
    Task<DbDataReader> GetColumnConstraints(string tableName);

    /// <summary>
    /// Uses INFORMATION_SCHEMA to get the column checks for a table
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>DbDataReader</returns>
    Task<DbDataReader> GetColumnChecks(string tableName);

    /// <summary>
    /// creates the insert statements and wraps them in a transaction
    /// </summary>
    /// <param name="executionNode"></param>
    /// <param name="plantoOptions"></param>
    /// <param name="data"></param>
    /// <typeparam name="TCast"></typeparam>
    /// <returns></returns>
    Task<TCast> CreateEntity<TCast>(ExecutionNode executionNode, PlantoOptions plantoOptions, params object?[] data);
}