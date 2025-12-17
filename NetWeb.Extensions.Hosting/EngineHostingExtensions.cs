using NetWeb.Extensions.DependencyInjection;

namespace NetWeb.Extensions.Hosting;

/// <summary>
/// Engine 扩展方法 - 添加后台服务支持
/// </summary>
public static class EngineHostingExtensions
{
    private static readonly string HostedServiceManagerKey = "__NetWeb_HostedServiceManager__";
    private static readonly Dictionary<Engine, HostedServiceManager> _managers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 添加后台服务
    /// </summary>
    public static Engine AddHostedService<T>(this Engine engine) where T : IHostedService, new()
    {
        var manager = GetOrCreateManager(engine);
        manager.Add<T>();
        return engine;
    }

    /// <summary>
    /// 添加后台服务实例
    /// </summary>
    public static Engine AddHostedService(this Engine engine, IHostedService service)
    {
        var manager = GetOrCreateManager(engine);
        manager.Add(service);
        return engine;
    }

    /// <summary>
    /// 添加后台服务（工厂方法）
    /// </summary>
    public static Engine AddHostedService(this Engine engine, Func<IHostedService> factory)
    {
        var manager = GetOrCreateManager(engine);
        manager.Add(factory);
        return engine;
    }

    /// <summary>
    /// 配置后台服务
    /// </summary>
    public static Engine ConfigureHostedServices(this Engine engine, Action<HostedServiceManager> configure)
    {
        var manager = GetOrCreateManager(engine);
        configure(manager);
        return engine;
    }

    /// <summary>
    /// 获取后台服务管理器
    /// </summary>
    public static HostedServiceManager? GetHostedServiceManager(this Engine engine)
    {
        lock (_lock)
        {
            return _managers.TryGetValue(engine, out var manager) ? manager : null;
        }
    }

    /// <summary>
    /// 运行应用（包含后台服务）
    /// </summary>
    public static async Task RunWithHostedServicesAsync(this Engine engine, string address = "http://localhost:5000/", CancellationToken cancellationToken = default)
    {
        var manager = GetOrCreateManager(engine);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 处理 Ctrl+C
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            // 启动所有后台服务
            await manager.StartAllAsync(cts.Token);

            // 运行 HTTP 服务器
            await engine.Run(address, cts.Token);
        }
        finally
        {
            // 停止所有后台服务
            using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await manager.StopAllAsync(stopCts.Token);
            manager.Dispose();
        }
    }

    private static HostedServiceManager GetOrCreateManager(Engine engine)
    {
        lock (_lock)
        {
            if (!_managers.ContainsKey(engine))
            {
                _managers[engine] = new HostedServiceManager();
            }
            return _managers[engine];
        }
    }
}
