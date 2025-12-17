namespace NetWeb.Extensions.Hosting;

/// <summary>
/// 托管服务接口 - 类似 ASP.NET Core 的 IHostedService
/// </summary>
public interface IHostedService
{
    /// <summary>
    /// 服务启动时调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 服务停止时调用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopAsync(CancellationToken cancellationToken);
}
