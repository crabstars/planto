using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Planto.DatabaseConnectionHandler;

public class DatabaseConnectionHandler(string connectionString) : IDatabaseConnectionHandler
{
    private DbConnection? _dbConnection;
    private DbTransaction? _dbTransaction;

    public async Task<DbConnection> GetOpenConnection()
    {
        if (_dbConnection is not null && _dbConnection.State == ConnectionState.Open)
        {
            return _dbConnection;
        }

        _dbConnection = new SqlConnection(connectionString);
        await _dbConnection.OpenAsync().ConfigureAwait(false);
        return _dbConnection;
    }

    public async Task StartTransaction()
    {
        _dbConnection ??= await GetOpenConnection().ConfigureAwait(false);

        _dbTransaction = await _dbConnection.BeginTransactionAsync().ConfigureAwait(false);
    }

    public async Task CommitTransaction()
    {
        if (_dbTransaction != null)
        {
            await _dbTransaction.CommitAsync().ConfigureAwait(false);
            await _dbTransaction.DisposeAsync().ConfigureAwait(false);
            _dbTransaction = null;
            await CloseConnection().ConfigureAwait(false);
        }
    }

    public async Task RollbackTransaction()
    {
        if (_dbTransaction != null)
        {
            await _dbTransaction.RollbackAsync().ConfigureAwait(false);
            await _dbTransaction.DisposeAsync().ConfigureAwait(false);
            _dbTransaction = null;
            await CloseConnection().ConfigureAwait(false);
        }
    }

    public DbTransaction GetDbTransaction()
    {
        return _dbTransaction ?? throw new InvalidOperationException("Call StartTransaction before Get");
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbConnection != null) await _dbConnection.DisposeAsync().ConfigureAwait(false);
        if (_dbTransaction != null) await _dbTransaction.DisposeAsync().ConfigureAwait(false);
        await CloseConnection().ConfigureAwait(false);
    }

    public async Task CloseConnection()
    {
        if (_dbConnection is not null && _dbConnection.State != ConnectionState.Closed)
        {
            await _dbConnection.CloseAsync().ConfigureAwait(false);
        }

        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync().ConfigureAwait(false);
            _dbConnection = null;
        }
    }
}