using System.Data.Common;

namespace Planto.DatabaseImplementation;

public interface IDatabaseConnectionHandler
{
    public Task<DbConnection> GetOpenConnection();
    public Task CloseConnection();
    public Task StartTransaction();
    public Task CommitTransaction();
    public Task RollbackTransaction();
    public DbTransaction GetDbTransaction();
}