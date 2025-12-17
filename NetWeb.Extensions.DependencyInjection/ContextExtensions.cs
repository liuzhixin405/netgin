namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// Context 扩展方法 - 从请求上下文获取服务
/// </summary>
public static class ContextExtensions
{
    /// <summary>
    /// 获取服务
    /// </summary>
    public static T? GetService<T>(this Context ctx) where T : class
    {
        var provider = ctx.Get<IServiceProvider>("__ServiceProvider__");
        return provider?.GetService(typeof(T)) as T;
    }

    /// <summary>
    /// 获取必需服务（如果不存在则抛异常）
    /// </summary>
    public static T GetRequiredService<T>(this Context ctx) where T : class
    {
        var service = ctx.GetService<T>();
        if (service == null)
            throw new InvalidOperationException($"Service of type {typeof(T)} is not registered.");
        return service;
    }

    /// <summary>
    /// 获取服务（按类型）
    /// </summary>
    public static object? GetService(this Context ctx, Type serviceType)
    {
        var provider = ctx.Get<IServiceProvider>("__ServiceProvider__");
        return provider?.GetService(serviceType);
    }
}
