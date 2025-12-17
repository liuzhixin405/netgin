using System.Data;

namespace NetWeb.Extensions.Data;

/// <summary>
/// 工作单元接口
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 获取数据库连接
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// 获取当前事务
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>
    /// 开始事务
    /// </summary>
    void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

    /// <summary>
    /// 提交事务
    /// </summary>
    void Commit();

    /// <summary>
    /// 回滚事务
    /// </summary>
    void Rollback();

    /// <summary>
    /// 异步执行带事务的操作
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}

/// <summary>
/// 工作单元实现
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = _connectionFactory.CreateConnection();
                _connection.Open();
            }
            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_transaction != null)
            throw new InvalidOperationException("事务已经开始。");

        _transaction = Connection.BeginTransaction(isolationLevel);
    }

    public void Commit()
    {
        if (_transaction == null)
            throw new InvalidOperationException("没有活动的事务。");

        try
        {
            _transaction.Commit();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        if (_transaction == null)
            return;

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        BeginTransaction(isolationLevel);

        try
        {
            var result = await action(Connection, Transaction!);
            Commit();
            return result;
        }
        catch
        {
            Rollback();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _transaction?.Dispose();
        _connection?.Dispose();

        GC.SuppressFinalize(this);
    }
}
