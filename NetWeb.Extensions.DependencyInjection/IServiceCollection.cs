using System.Collections;

namespace NetWeb.Extensions.DependencyInjection;

/// <summary>
/// 服务集合接口
/// </summary>
public interface IServiceCollection : IList<ServiceDescriptor>
{
}

/// <summary>
/// 服务集合实现
/// </summary>
public class ServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new();

    public ServiceDescriptor this[int index]
    {
        get => _descriptors[index];
        set => _descriptors[index] = value;
    }

    public int Count => _descriptors.Count;
    public bool IsReadOnly => false;

    public void Add(ServiceDescriptor item) => _descriptors.Add(item);
    public void Clear() => _descriptors.Clear();
    public bool Contains(ServiceDescriptor item) => _descriptors.Contains(item);
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _descriptors.CopyTo(array, arrayIndex);
    public IEnumerator<ServiceDescriptor> GetEnumerator() => _descriptors.GetEnumerator();
    public int IndexOf(ServiceDescriptor item) => _descriptors.IndexOf(item);
    public void Insert(int index, ServiceDescriptor item) => _descriptors.Insert(index, item);
    public bool Remove(ServiceDescriptor item) => _descriptors.Remove(item);
    public void RemoveAt(int index) => _descriptors.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
