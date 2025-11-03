using System.Data;
using Npgsql;
using AuctionService.Dal.Interfaces;
using AuctionService.Dal.Repositories;

namespace AuctionService.Dal;

/// <summary>
/// Unit of Work для керування транзакціями та репозиторіями з PostgreSQL
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private NpgsqlConnection? _connection;
    private IDbTransaction? _transaction;

    private IUserRepository? _users;
    private IAuctionRepository? _auctions;
    private IBidRepository? _bids;
    private IPaymentRepository? _payments;

    public UnitOfWork(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IUserRepository Users
    {
        get
        {
            _users ??= new UserRepository(_connectionString, _transaction);
            return _users;
        }
    }

    public IAuctionRepository Auctions
    {
        get
        {
            _auctions ??= new AuctionRepository(_connectionString, _transaction);
            return _auctions;
        }
    }

    public IBidRepository Bids
    {
        get
        {
            _bids ??= new BidRepository(_connectionString, _transaction);
            return _bids;
        }
    }

    public IPaymentRepository Payments
    {
        get
        {
            _payments ??= new PaymentRepository(_connectionString, _transaction);
            return _payments;
        }
    }

    /// <summary>
    /// Початок транзакції
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        if (_connection != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync();
        _transaction = _connection.BeginTransaction();

        // Оновлюємо репозиторії з новою транзакцією
        _users = new UserRepository(_connectionString, _transaction);
        _auctions = new AuctionRepository(_connectionString, _transaction);
        _bids = new BidRepository(_connectionString, _transaction);
        _payments = new PaymentRepository(_connectionString, _transaction);
    }

    /// <summary>
    /// Підтвердження транзакції
    /// </summary>
    public async Task CommitAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit");
        }

        try
        {
            _transaction.Commit();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
            
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }

    /// <summary>
    /// Відкат транзакції
    /// </summary>
    public async Task RollbackAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback");
        }

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
            
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}
