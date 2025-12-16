namespace MiniGin.Extensions.Data;

/// <summary>
/// Context 扩展方法 - 从请求上下文获取数据库服务
/// </summary>
public static class ContextDataExtensions
{
    /// <summary>
    /// 获取数据库连接工厂
    /// </summary>
    public static IDbConnectionFactory? GetDbConnectionFactory(this Context ctx)
    {
        return ctx.Get<IDbConnectionFactory>("__DbConnectionFactory__") 
            ?? GetServiceFromProvider<IDbConnectionFactory>(ctx);
    }

    /// <summary>
    /// 获取工作单元
    /// </summary>
    public static IUnitOfWork? GetUnitOfWork(this Context ctx)
    {
        return GetServiceFromProvider<IUnitOfWork>(ctx);
    }

    /// <summary>
    /// 获取 SQL 方言
    /// </summary>
    public static ISqlDialect? GetSqlDialect(this Context ctx)
    {
        return GetServiceFromProvider<ISqlDialect>(ctx);
    }

    /// <summary>
    /// 获取仓储
    /// </summary>
    public static TRepository? GetRepository<TRepository>(this Context ctx) where TRepository : class
    {
        return GetServiceFromProvider<TRepository>(ctx);
    }

    private static T? GetServiceFromProvider<T>(Context ctx) where T : class
    {
        var provider = ctx.Get<IServiceProvider>("__ServiceProvider__");
        return provider?.GetService(typeof(T)) as T;
    }
}
