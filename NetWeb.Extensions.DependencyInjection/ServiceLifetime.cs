namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// 服务生命周期
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// 单例：整个应用程序生命周期内只创建一个实例
    /// </summary>
    Singleton,

    /// <summary>
    /// 作用域：每个请求创建一个实例
    /// </summary>
    Scoped,

    /// <summary>
    /// 瞬态：每次获取都创建新实例
    /// </summary>
    Transient
}
