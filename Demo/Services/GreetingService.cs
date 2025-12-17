namespace NetWeb.Demo.Services;

/// <summary>
/// 问候服务接口
/// </summary>
public interface IGreetingService
{
    string GetGreeting(string name);
}

/// <summary>
/// 问候服务实现
/// </summary>
public class GreetingService : IGreetingService
{
    public string GetGreeting(string name) => $"Hello, {name}!";
}
