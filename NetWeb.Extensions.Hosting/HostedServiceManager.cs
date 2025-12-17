using System.Collections.Concurrent;

namespace NetWeb.Extensions.Hosting;

/// <summary>
/// 托管服务管理器 - 管理所有后台服务的生命周期
/// </summary>
public class HostedServiceManager : IDisposable
{
    private readonly List<IHostedService> _services = new();
    private readonly ConcurrentDictionary<IHostedService, bool> _runningServices = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>
    /// 添加托管服务
    /// </summary>
    public HostedServiceManager Add<T>() where T : IHostedService, new()
    {
        return Add(new T());
    }

    /// <summary>
    /// 添加托管服务实例
    /// </summary>
    public HostedServiceManager Add(IHostedService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        _services.Add(service);
        return this;
    }

    /// <summary>
    /// 添加托管服务（工厂方法）
    /// </summary>
    public HostedServiceManager Add(Func<IHostedService> factory)
    {
        return Add(factory());
    }

    /// <summary>
    /// 启动所有托管服务
    /// </summary>
    public async Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (var service in _services)
        {
            try
            {
                Console.WriteLine($"[Hosting] Starting {service.GetType().Name}...");
                await service.StartAsync(_cts.Token);
                _runningServices[service] = true;
                Console.WriteLine($"[Hosting] {service.GetType().Name} started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hosting] Failed to start {service.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 停止所有托管服务
    /// </summary>
    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();

        // 逆序停止服务
        var exceptions = new List<Exception>();

        foreach (var service in _services.AsEnumerable().Reverse())
        {
            if (!_runningServices.TryGetValue(service, out var running) || !running)
                continue;

            try
            {
                Console.WriteLine($"[Hosting] Stopping {service.GetType().Name}...");
                await service.StopAsync(cancellationToken);
                _runningServices[service] = false;
                Console.WriteLine($"[Hosting] {service.GetType().Name} stopped.");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                Console.WriteLine($"[Hosting] Error stopping {service.GetType().Name}: {ex.Message}");
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more hosted services failed to stop.", exceptions);
        }
    }

    /// <summary>
    /// 获取所有已注册的服务
    /// </summary>
    public IReadOnlyList<IHostedService> Services => _services.AsReadOnly();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        foreach (var service in _services)
        {
            if (service is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch { /* 忽略 */ }
            }
        }

        _services.Clear();
        _runningServices.Clear();
        GC.SuppressFinalize(this);
    }
}
