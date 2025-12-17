using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetWeb;

/// <summary>
/// 路由分组 - 支持前缀和中间件继承
/// </summary>
public class RouterGroup : IRouter
{
    private readonly Engine _engine;
    private readonly string _basePath;
    private readonly List<HandlerFunc> _handlers = new();
    private string? _tag;

    internal RouterGroup(Engine engine, string basePath, IEnumerable<HandlerFunc>? handlers = null, string? tag = null)
    {
        _engine = engine;
        _basePath = basePath;
        _tag = tag;
        if (handlers != null)
            _handlers.AddRange(handlers);
    }

    /// <summary>基础路径</summary>
    public string BasePath => _basePath;

    /// <summary>关联的引擎</summary>
    protected Engine Engine => _engine;

    /// <summary>当前组的处理器链</summary>
    protected IReadOnlyList<HandlerFunc> Handlers => _handlers;

    /// <summary>Swagger 分组标签</summary>
    public string? Tag => _tag;

    /// <summary>
    /// 设置当前分组的 Swagger 标签
    /// </summary>
    /// <param name="tag">标签名称</param>
    /// <returns>当前分组实例</returns>
    public RouterGroup WithTag(string tag)
    {
        _tag = tag;
        return this;
    }

    #region 中间件

    /// <inheritdoc />
    public IRouter Use(params IMiddleware[] middleware)
    {
        foreach (var m in middleware)
        {
            _handlers.Add(ctx => m.InvokeAsync(ctx, _ => Task.CompletedTask));
        }
        return this;
    }

    /// <inheritdoc />
    public IRouter Use(params HandlerFunc[] handlers)
    {
        _handlers.AddRange(handlers);
        return this;
    }

    #endregion

    #region 路由注册

    /// <inheritdoc />
    public IRouter GET(string path, params HandlerFunc[] handlers)
        => Handle("GET", path, handlers);

    /// <inheritdoc />
    public IRouter POST(string path, params HandlerFunc[] handlers)
        => Handle("POST", path, handlers);

    /// <inheritdoc />
    public IRouter PUT(string path, params HandlerFunc[] handlers)
        => Handle("PUT", path, handlers);

    /// <inheritdoc />
    public IRouter DELETE(string path, params HandlerFunc[] handlers)
        => Handle("DELETE", path, handlers);

    /// <inheritdoc />
    public IRouter PATCH(string path, params HandlerFunc[] handlers)
        => Handle("PATCH", path, handlers);

    /// <inheritdoc />
    public IRouter HEAD(string path, params HandlerFunc[] handlers)
        => Handle("HEAD", path, handlers);

    /// <inheritdoc />
    public IRouter OPTIONS(string path, params HandlerFunc[] handlers)
        => Handle("OPTIONS", path, handlers);

    /// <inheritdoc />
    public IRouter Handle(string method, string path, params HandlerFunc[] handlers)
    {
        var absolutePath = JoinPaths(_basePath, path);
        var mergedHandlers = _handlers.Concat(handlers).ToArray();
        _engine.AddRoute(method, absolutePath, mergedHandlers, _tag);
        return this;
    }

    #endregion

    #region 子分组

    /// <inheritdoc />
    public RouterGroup Group(string relativePath)
    {
        var newPath = JoinPaths(_basePath, relativePath);
        return new RouterGroup(_engine, newPath, _handlers, _tag);
    }

    /// <summary>
    /// 创建带标签的子分组
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <param name="tag">Swagger 分组标签</param>
    /// <returns>新的路由分组</returns>
    public RouterGroup Group(string relativePath, string tag)
    {
        var newPath = JoinPaths(_basePath, relativePath);
        return new RouterGroup(_engine, newPath, _handlers, tag);
    }

    #endregion

    #region 路径处理

    private static string JoinPaths(string basePath, string relativePath)
    {
        basePath = (basePath ?? "").Trim().TrimEnd('/');
        relativePath = (relativePath ?? "").Trim();

        if (!relativePath.StartsWith("/") && !string.IsNullOrEmpty(relativePath))
            relativePath = "/" + relativePath;

        if (relativePath.Length > 1)
            relativePath = relativePath.TrimEnd('/');

        if (string.IsNullOrEmpty(basePath))
            return string.IsNullOrEmpty(relativePath) ? "/" : relativePath;

        return basePath + relativePath;
    }

    #endregion
}
