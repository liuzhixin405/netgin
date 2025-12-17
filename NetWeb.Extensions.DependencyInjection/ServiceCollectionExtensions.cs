namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    #region Singleton

    /// <summary>
    /// 注册单例服务
    /// </summary>
    public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
        return services;
    }

    /// <summary>
    /// 注册单例服务（自身类型）
    /// </summary>
    public static IServiceCollection AddSingleton<TService>(this IServiceCollection services)
        where TService : class
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TService), ServiceLifetime.Singleton));
        return services;
    }

    /// <summary>
    /// 注册单例服务（工厂方法）
    /// </summary>
    public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), sp => factory(sp), ServiceLifetime.Singleton));
        return services;
    }

    /// <summary>
    /// 注册单例服务（实例）
    /// </summary>
    public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, TService instance)
        where TService : class
    {
        services.Add(ServiceDescriptor.Singleton(typeof(TService), instance));
        return services;
    }

    #endregion

    #region Scoped

    /// <summary>
    /// 注册作用域服务
    /// </summary>
    public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
        return services;
    }

    /// <summary>
    /// 注册作用域服务（自身类型）
    /// </summary>
    public static IServiceCollection AddScoped<TService>(this IServiceCollection services)
        where TService : class
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TService), ServiceLifetime.Scoped));
        return services;
    }

    /// <summary>
    /// 注册作用域服务（工厂方法）
    /// </summary>
    public static IServiceCollection AddScoped<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), sp => factory(sp), ServiceLifetime.Scoped));
        return services;
    }

    #endregion

    #region Transient

    /// <summary>
    /// 注册瞬态服务
    /// </summary>
    public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient));
        return services;
    }

    /// <summary>
    /// 注册瞬态服务（自身类型）
    /// </summary>
    public static IServiceCollection AddTransient<TService>(this IServiceCollection services)
        where TService : class
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), typeof(TService), ServiceLifetime.Transient));
        return services;
    }

    /// <summary>
    /// 注册瞬态服务（工厂方法）
    /// </summary>
    public static IServiceCollection AddTransient<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        services.Add(ServiceDescriptor.Describe(typeof(TService), sp => factory(sp), ServiceLifetime.Transient));
        return services;
    }

    #endregion

    /// <summary>
    /// 构建服务提供者
    /// </summary>
    public static ServiceProvider BuildServiceProvider(this IServiceCollection services)
    {
        return new ServiceProvider(services);
    }
}
