using System.Data.Common;

namespace Planto.DatabaseConnectionHandler;

public interface IDatabaseConnectionHandler : IAsyncDisposable
{
    public Task<DbConnection> GetOpenConnection();
    public Task StartTransaction();
    public Task CommitTransaction();
    public Task RollbackTransaction();
    public DbTransaction GetDbTransaction();
}