namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// Engine 扩展方法 - 添加依赖注入支持
/// </summary>
public static class EngineExtensions
{
    private static readonly string ServicesKey = "__NetWeb_Services__";
    private static readonly string ServiceProviderKey = "__NetWeb_ServiceProvider__";

    /// <summary>
    /// 配置服务
    /// </summary>
    public static Engine ConfigureServices(this Engine engine, Action<IServiceCollection> configure)
    {
        var services = GetOrCreateServices(engine);
        configure(services);
        return engine;
    }

    /// <summary>
    /// 获取服务集合
    /// </summary>
    public static IServiceCollection GetServices(this Engine engine)
    {
        return GetOrCreateServices(engine);
    }

    /// <summary>
    /// 构建服务提供者并启用依赖注入中间件
    /// </summary>
    public static Engine BuildServices(this Engine engine)
    {
        var services = GetOrCreateServices(engine);
        var provider = services.BuildServiceProvider();

        // 存储到引擎
        SetEngineData(engine, ServiceProviderKey, provider);

        // 添加 DI 中间件
        engine.Use(ctx =>
        {
            // 为每个请求创建作用域
            var scope = provider.CreateScope();
            ctx.Set("__ServiceProvider__", scope.ServiceProvider);
            ctx.Set("__ServiceScope__", scope);  // 保存 scope 引用，用于后续释放
            return Task.CompletedTask;
        });

        return engine;
    }

    /// <summary>
    /// 释放请求作用域的中间件（应该在所有处理完成后调用）
    /// </summary>
    public static Engine UseScopeDisposal(this Engine engine)
    {
        engine.Use(async ctx =>
        {
            // 这个中间件应该放在最后，用于清理 scope
            // 但由于中间件的执行顺序，我们需要在 Engine 层面处理
            await Task.CompletedTask;
        });
        return engine;
    }

    /// <summary>
    /// 获取根服务提供者
    /// </summary>
    public static ServiceProvider? GetServiceProvider(this Engine engine)
    {
        return GetEngineData<ServiceProvider>(engine, ServiceProviderKey);
    }

    private static IServiceCollection GetOrCreateServices(Engine engine)
    {
        var services = GetEngineData<IServiceCollection>(engine, ServicesKey);
        if (services == null)
        {
            services = new ServiceCollection();
            SetEngineData(engine, ServicesKey, services);
        }
        return services;
    }

    // 使用反射存储数据到 Engine（因为 Engine 没有公开的存储机制）
    private static readonly Dictionary<Engine, Dictionary<string, object>> _engineData = new();
    private static readonly object _lock = new();

    private static T? GetEngineData<T>(Engine engine, string key) where T : class
    {
        lock (_lock)
        {
            if (_engineData.TryGetValue(engine, out var data) && data.TryGetValue(key, out var value))
                return value as T;
            return null;
        }
    }

    private static void SetEngineData(Engine engine, string key, object value)
    {
        lock (_lock)
        {
            if (!_engineData.ContainsKey(engine))
                _engineData[engine] = new Dictionary<string, object>();
            _engineData[engine][key] = value;
        }
    }
}
