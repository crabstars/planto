using System.Data.Common;
using Planto.OptionBuilder;

namespace Planto.DatabaseImplementation;

public interface IDatabaseProviderHelper : IAsyncDisposable
{
    public Type MapToSystemType(string sqlType);

    public Task<DbDataReader> GetColumInfos(string tableName);

    public Task<DbDataReader> GetColumnConstraints(string tableName);

    Task<TCast> CreateEntity<TCast>(ExecutionNode executionNode, PlantoOptions plantoOptions);
}