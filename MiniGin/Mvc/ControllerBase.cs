namespace MiniGin.Mvc;

/// <summary>
/// 控制器基类
/// </summary>
public abstract class ControllerBase
{
    /// <summary>
    /// 当前请求上下文
    /// </summary>
    public Context Context { get; internal set; } = null!;

    /// <summary>
    /// 返回 JSON 响应
    /// </summary>
    protected Task Json(object data, int statusCode = 200)
        => Context.JSON(data);

    /// <summary>
    /// 返回成功响应 (200)
    /// </summary>
    protected Task Ok(object? data = null)
        => data == null ? Context.Status(200) : Context.OK(data);

    /// <summary>
    /// 返回创建成功响应 (201)
    /// </summary>
    protected Task Created(object data)
        => Context.Created(data);

    /// <summary>
    /// 返回无内容响应 (204)
    /// </summary>
    protected Task NoContent()
        => Context.Status(204);

    /// <summary>
    /// 返回错误请求响应 (400)
    /// </summary>
    protected Task BadRequest(object? error = null)
        => Context.BadRequest(error ?? new { error = "Bad Request" });

    /// <summary>
    /// 返回未授权响应 (401)
    /// </summary>
    protected Task Unauthorized(object? error = null)
        => Context.Status(401);

    /// <summary>
    /// 返回禁止访问响应 (403)
    /// </summary>
    protected Task Forbidden(object? error = null)
        => Context.Status(403);

    /// <summary>
    /// 返回未找到响应 (404)
    /// </summary>
    protected Task NotFound(object? error = null)
        => Context.NotFound(error ?? new { error = "Not Found" });

    /// <summary>
    /// 返回字符串响应
    /// </summary>
    protected Task String(string content, int statusCode = 200)
        => Context.String(statusCode, content);

    /// <summary>
    /// 获取路由参数
    /// </summary>
    protected string? Param(string key)
        => Context.Param(key);

    /// <summary>
    /// 获取查询参数
    /// </summary>
    protected T? Query<T>(string key) where T : struct
        => Context.Query<T>(key);

    /// <summary>
    /// 获取查询参数（字符串）
    /// </summary>
    protected string? Query(string key)
        => Context.Query(key);

    /// <summary>
    /// 绑定请求体到对象
    /// </summary>
    protected Task<T?> BindAsync<T>() where T : class
        => Context.BindAsync<T>();

    /// <summary>
    /// 获取服务
    /// </summary>
    protected T? GetService<T>() where T : class
    {
        var provider = Context.Get<IServiceProvider>("__ServiceProvider__");
        return provider?.GetService(typeof(T)) as T;
    }
}
