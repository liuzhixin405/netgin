namespace MiniGin.Extensions.Hosting;

/// <summary>
/// 定时后台服务 - 定期执行任务
/// </summary>
public abstract class TimedBackgroundService : BackgroundService
{
    /// <summary>
    /// 执行间隔
    /// </summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>
    /// 是否立即执行第一次（默认 true）
    /// </summary>
    protected virtual bool ExecuteImmediately => true;

    /// <summary>
    /// 执行定时任务
    /// </summary>
    protected abstract Task DoWorkAsync(CancellationToken stoppingToken);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ExecuteImmediately)
        {
            await SafeExecute(stoppingToken);
        }

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeExecute(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常取消，退出循环
                break;
            }
        }
    }

    private async Task SafeExecute(CancellationToken stoppingToken)
    {
        try
        {
            await DoWorkAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnError(ex);
        }
    }

    /// <summary>
    /// 任务执行出错时调用
    /// </summary>
    protected virtual void OnError(Exception exception)
    {
        Console.WriteLine($"[{GetType().Name}] Error: {exception.Message}");
    }
}
