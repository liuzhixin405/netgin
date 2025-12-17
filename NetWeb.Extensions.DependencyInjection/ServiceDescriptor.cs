namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// 服务描述符
/// </summary>
public class ServiceDescriptor
{
    /// <summary>
    /// 服务类型（接口或抽象类）
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// 实现类型
    /// </summary>
    public Type? ImplementationType { get; private set; }

    /// <summary>
    /// 工厂方法
    /// </summary>
    public Func<IServiceProvider, object>? Factory { get; private set; }

    /// <summary>
    /// 单例实例
    /// </summary>
    public object? Instance { get; private set; }

    /// <summary>
    /// 生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    private ServiceDescriptor(Type serviceType, ServiceLifetime lifetime)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        Lifetime = lifetime;
    }

    /// <summary>
    /// 使用实现类型创建
    /// </summary>
    public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(serviceType, lifetime);
        descriptor.ImplementationType = implementationType;
        return descriptor;
    }

    /// <summary>
    /// 使用工厂方法创建
    /// </summary>
    public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(serviceType, lifetime);
        descriptor.Factory = factory;
        return descriptor;
    }

    /// <summary>
    /// 使用实例创建（仅限 Singleton）
    /// </summary>
    public static ServiceDescriptor Singleton(Type serviceType, object instance)
    {
        var descriptor = new ServiceDescriptor(serviceType, ServiceLifetime.Singleton);
        descriptor.Instance = instance;
        return descriptor;
    }
}
