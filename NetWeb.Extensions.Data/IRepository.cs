using System.Data;

namespace NetWeb.Extensions.Data;

/// <summary>
/// 通用仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 根据 ID 获取实体
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// 添加实体
    /// </summary>
    Task<TKey> AddAsync(TEntity entity);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<bool> UpdateAsync(TEntity entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task<bool> DeleteAsync(TKey id);

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    Task<bool> ExistsAsync(TKey id);

    /// <summary>
    /// 获取记录数
    /// </summary>
    Task<int> CountAsync();
}

/// <summary>
/// 仓储基类（提供基础实现参考）
/// </summary>
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected RepositoryBase(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// 获取表名
    /// </summary>
    protected abstract string TableName { get; }

    /// <summary>
    /// 主键列名
    /// </summary>
    protected virtual string KeyColumn => "Id";

    /// <summary>
    /// 创建连接
    /// </summary>
    protected IDbConnection CreateConnection() => ConnectionFactory.CreateConnection();

    public abstract Task<TEntity?> GetByIdAsync(TKey id);
    public abstract Task<IEnumerable<TEntity>> GetAllAsync();
    public abstract Task<TKey> AddAsync(TEntity entity);
    public abstract Task<bool> UpdateAsync(TEntity entity);
    public abstract Task<bool> DeleteAsync(TKey id);
    public abstract Task<bool> ExistsAsync(TKey id);
    public abstract Task<int> CountAsync();

    /// <summary>
    /// 执行带连接的操作
    /// </summary>
    protected async Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> action)
    {
        using var connection = CreateConnection();
        connection.Open();
        return await action(connection);
    }

    /// <summary>
    /// 执行无返回值的操作
    /// </summary>
    protected async Task ExecuteAsync(Func<IDbConnection, Task> action)
    {
        using var connection = CreateConnection();
        connection.Open();
        await action(connection);
    }
}
