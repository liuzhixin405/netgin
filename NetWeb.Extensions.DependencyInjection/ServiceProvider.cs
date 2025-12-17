using System.Collections.Concurrent;

namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// 服务提供者
/// </summary>
public class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly IReadOnlyList<ServiceDescriptor> _descriptors;
    private readonly ConcurrentDictionary<Type, object> _singletons = new();
    private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> _factories = new();
    private bool _disposed;

    internal ServiceProvider(IEnumerable<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors.ToList();

        // 预处理工厂方法
        foreach (var descriptor in _descriptors)
        {
            if (descriptor.Instance != null)
            {
                _singletons[descriptor.ServiceType] = descriptor.Instance;
            }
        }
    }

    /// <summary>
    /// 获取服务
    /// </summary>
    public object? GetService(Type serviceType)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServiceProvider));

        // 特殊处理：返回自身
        if (serviceType == typeof(IServiceProvider))
            return this;

        var descriptor = _descriptors.LastOrDefault(d => d.ServiceType == serviceType);
        if (descriptor == null)
            return null;

        return descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => GetOrCreateSingleton(descriptor),
            ServiceLifetime.Transient => CreateInstance(descriptor),
            ServiceLifetime.Scoped => throw new InvalidOperationException(
                "Scoped services must be resolved from a scope. Use CreateScope() first."),
            _ => throw new InvalidOperationException($"Unknown lifetime: {descriptor.Lifetime}")
        };
    }

    private object GetOrCreateSingleton(ServiceDescriptor descriptor)
    {
        return _singletons.GetOrAdd(descriptor.ServiceType, _ => CreateInstance(descriptor));
    }

    private object CreateInstance(ServiceDescriptor descriptor)
    {
        if (descriptor.Instance != null)
            return descriptor.Instance;

        if (descriptor.Factory != null)
            return descriptor.Factory(this);

        if (descriptor.ImplementationType != null)
            return ActivateType(descriptor.ImplementationType);

        throw new InvalidOperationException($"Unable to create instance for {descriptor.ServiceType}");
    }

    private object ActivateType(Type type)
    {
        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
            throw new InvalidOperationException($"No public constructor found for {type}");

        // 选择参数最多的构造函数
        var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            args[i] = GetService(paramType);

            if (args[i] == null && !parameters[i].HasDefaultValue)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve service for type '{paramType}' while attempting to activate '{type}'.");
            }

            args[i] ??= parameters[i].DefaultValue;
        }

        return ctor.Invoke(args)!;
    }

    /// <summary>
    /// 创建作用域
    /// </summary>
    public IServiceScope CreateScope()
    {
        return new ServiceScope(this, _descriptors);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var singleton in _singletons.Values)
        {
            if (singleton is IDisposable disposable)
                disposable.Dispose();
        }

        _singletons.Clear();
    }
}

/// <summary>
/// 服务作用域接口
/// </summary>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// 作用域内的服务提供者
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}

/// <summary>
/// 服务作用域实现
/// </summary>
internal class ServiceScope : IServiceScope
{
    private readonly ServiceProvider _rootProvider;
    private readonly ScopedServiceProvider _scopedProvider;
    private bool _disposed;

    public IServiceProvider ServiceProvider => _scopedProvider;

    internal ServiceScope(ServiceProvider rootProvider, IReadOnlyList<ServiceDescriptor> descriptors)
    {
        _rootProvider = rootProvider;
        _scopedProvider = new ScopedServiceProvider(rootProvider, descriptors);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _scopedProvider.Dispose();
    }
}

/// <summary>
/// 作用域服务提供者
/// </summary>
internal class ScopedServiceProvider : IServiceProvider, IDisposable
{
    private readonly ServiceProvider _rootProvider;
    private readonly IReadOnlyList<ServiceDescriptor> _descriptors;
    private readonly Dictionary<Type, object> _scopedInstances = new();
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    internal ScopedServiceProvider(ServiceProvider rootProvider, IReadOnlyList<ServiceDescriptor> descriptors)
    {
        _rootProvider = rootProvider;
        _descriptors = descriptors;
    }

    public object? GetService(Type serviceType)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ScopedServiceProvider));

        if (serviceType == typeof(IServiceProvider))
            return this;

        var descriptor = _descriptors.LastOrDefault(d => d.ServiceType == serviceType);
        if (descriptor == null)
            return null;

        return descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => _rootProvider.GetService(serviceType),
            ServiceLifetime.Scoped => GetOrCreateScoped(descriptor),
            ServiceLifetime.Transient => CreateInstance(descriptor),
            _ => throw new InvalidOperationException($"Unknown lifetime: {descriptor.Lifetime}")
        };
    }

    private object GetOrCreateScoped(ServiceDescriptor descriptor)
    {
        if (_scopedInstances.TryGetValue(descriptor.ServiceType, out var instance))
            return instance;

        instance = CreateInstance(descriptor);
        _scopedInstances[descriptor.ServiceType] = instance;

        if (instance is IDisposable disposable)
            _disposables.Add(disposable);

        return instance;
    }

    private object CreateInstance(ServiceDescriptor descriptor)
    {
        if (descriptor.Factory != null)
            return descriptor.Factory(this);

        if (descriptor.ImplementationType != null)
            return ActivateType(descriptor.ImplementationType);

        throw new InvalidOperationException($"Unable to create instance for {descriptor.ServiceType}");
    }

    private object ActivateType(Type type)
    {
        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
            throw new InvalidOperationException($"No public constructor found for {type}");

        var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            args[i] = GetService(paramType);

            if (args[i] == null && !parameters[i].HasDefaultValue)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve service for type '{paramType}' while attempting to activate '{type}'.");
            }

            args[i] ??= parameters[i].DefaultValue;
        }

        var instance = ctor.Invoke(args)!;

        if (instance is IDisposable disposable)
            _disposables.Add(disposable);

        return instance;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var disposable in _disposables)
        {
            try { disposable.Dispose(); }
            catch { /* 忽略释放异常 */ }
        }

        _disposables.Clear();
        _scopedInstances.Clear();
    }
}
