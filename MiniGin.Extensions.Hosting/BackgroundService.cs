namespace MiniGin.Extensions.Hosting;

/// <summary>
/// 后台服务基类 - 用于长时间运行的后台任务
/// </summary>
public abstract class BackgroundService : IHostedService, IDisposable
{
    private Task? _executeTask;
    private CancellationTokenSource? _stoppingCts;

    /// <summary>
    /// 执行后台任务的抽象方法
    /// </summary>
    /// <param name="stoppingToken">当服务停止时会触发取消</param>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <inheritdoc />
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executeTask = ExecuteAsync(_stoppingCts.Token);

        // 如果任务已完成，直接返回
        if (_executeTask.IsCompleted)
        {
            return _executeTask;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executeTask == null)
        {
            return;
        }

        try
        {
            _stoppingCts?.Cancel();
        }
        finally
        {
            // 等待任务完成或超时
            var completedTask = await Task.WhenAny(_executeTask, Task.Delay(Timeout.Infinite, cancellationToken));

            if (completedTask == _executeTask)
            {
                // 获取任务结果以传播异常
                await _executeTask;
            }
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
