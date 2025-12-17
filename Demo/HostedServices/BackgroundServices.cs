using NetWeb.Extensions.Hosting;

namespace NetWeb.Demo.HostedServices;

/// <summary>
/// 心跳服务 - 每 30 秒打印一次心跳
/// </summary>
public class HeartbeatService : TimedBackgroundService
{
    protected override TimeSpan Interval => TimeSpan.FromSeconds(30);
    protected override bool ExecuteImmediately => false;

    protected override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"[Heartbeat] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Server is alive");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 清理服务 - 每 5 分钟执行一次清理任务
/// </summary>
public class CleanupService : TimedBackgroundService
{
    protected override TimeSpan Interval => TimeSpan.FromMinutes(5);
    protected override bool ExecuteImmediately => true;

    protected override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"[Cleanup] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Running cleanup task...");
        // 在这里执行清理逻辑，如清理过期缓存、临时文件等
        return Task.CompletedTask;
    }
}
