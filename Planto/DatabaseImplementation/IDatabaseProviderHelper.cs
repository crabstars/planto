using System.Data.Common;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation;

internal interface IDatabaseProviderHelper : IAsyncDisposable
{
    public Type MapToSystemType(string sqlType);

    public Task<DbDataReader> GetColumInfos(string tableName);

    public Task<DbDataReader> GetColumnConstraints(string tableName);

    public Task<DbDataReader> GetColumnChecks(string tableName);

    Task<TCast> CreateEntity<TCast>(ExecutionNode executionNode, PlantoOptions plantoOptions, params object?[] data);
}