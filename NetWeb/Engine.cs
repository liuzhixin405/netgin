using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetWeb.Diagnostics;
using NetWeb.Http;

namespace NetWeb;

/// <summary>
/// Gin 风格的 HTTP 引擎 - 核心入口（基于 Socket 的高性能实现）
/// </summary>
public class Engine : RouterGroup
{
    private readonly List<Route> _routes = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private HttpServer? _server;
    private CancellationTokenSource? _cts;

    private bool _swaggerEnabled;
    private string _swaggerTitle = "NetWeb API";
    private string _swaggerVersion = "v1";

    /// <summary>
    /// 创建新的引擎实例
    /// </summary>
    public Engine() : this(new JsonSerializerOptions(JsonSerializerDefaults.Web))
    {
    }

    /// <summary>
    /// 创建新的引擎实例（自定义 JSON 选项）
    /// </summary>
    public Engine(JsonSerializerOptions jsonOptions) : base(null!, "")
    {
        _jsonOptions = jsonOptions;
        SetEngine(this);
    }

    private void SetEngine(Engine engine)
    {
        var field = typeof(RouterGroup).GetField("_engine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, engine);
    }

    #region 配置

    /// <summary>
    /// 启用 Swagger UI 和 OpenAPI 文档
    /// </summary>
    /// <param name="title">API 标题</param>
    /// <param name="version">API 版本</param>
    public Engine UseSwagger(string title = "NetWeb API", string version = "v1")
    {
        _swaggerEnabled = true;
        _swaggerTitle = title;
        _swaggerVersion = version;
        return this;
    }

    /// <summary>
    /// 获取所有已注册的路由
    /// </summary>
    public IReadOnlyList<Route> Routes => _routes;

    /// <summary>
    /// JSON 序列化选项
    /// </summary>
    public JsonSerializerOptions JsonOptions => _jsonOptions;

    #endregion

    #region 路由注册（内部）

    internal void AddRoute(string method, string path, HandlerFunc[] handlers, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("HTTP method is required.", nameof(method));

        if (handlers == null || handlers.Length == 0)
            throw new ArgumentException("At least one handler is required.", nameof(handlers));

        var pattern = RoutePattern.Parse(path);
        var route = new Route(method.ToUpperInvariant(), path, pattern, handlers, tag);

        _routes.Add(route);
        _routes.Sort((a, b) => b.Pattern.LiteralCount.CompareTo(a.Pattern.LiteralCount));
    }

    #endregion

    #region 运行

    /// <summary>
    /// 启动 HTTP 服务器
    /// </summary>
    /// <param name="address">监听地址，如 http://localhost:5000/</param>
    public Task Run(string address = "http://localhost:5000/")
        => Run(address, CancellationToken.None);

    /// <summary>
    /// 启动 HTTP 服务器（支持取消）
    /// </summary>
    /// <param name="address">监听地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task Run(string address, CancellationToken cancellationToken)
    {
        // 解析地址
        var uri = new Uri(address.TrimEnd('/'));
        var host = uri.Host;
        var port = uri.Port;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _server = new HttpServer();
        _server.Start(host, port);

        Console.WriteLine($"[NetWeb] Listening on {address}");
        Console.WriteLine($"[NetWeb] Using high-performance Socket-based HTTP server");
        if (_swaggerEnabled)
            Console.WriteLine($"[NetWeb] Swagger UI: {address}/swagger");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var connection = await _server.AcceptAsync(_cts.Token);
                    if (connection != null)
                    {
                        _ = Task.Run(() => HandleConnectionAsync(connection, _cts.Token), _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NetWeb] Error accepting connection: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetWeb] Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
        finally
        {
            _server.Dispose();
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _server?.Stop();
    }

    private async Task HandleConnectionAsync(HttpConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            // 支持 HTTP/1.1 Keep-Alive
            var keepAliveCount = 0;
            const int maxKeepAliveRequests = 100;
            
            while (!cancellationToken.IsCancellationRequested && keepAliveCount < maxKeepAliveRequests)
            {
                HttpRequest? request;
                try
                {
                    // 设置读取超时（Keep-Alive 超时 30 秒）
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    
                    request = await connection.ReadRequestAsync(linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 超时或取消，正常关闭连接
                    break;
                }
                
                if (request == null)
                    break; // 连接关闭

                await HandleRequestAsync(connection, request);
                keepAliveCount++;

                // 检查是否保持连接
                var connectionHeader = request.Headers.GetValueOrDefault("Connection", "");
                if (request.HttpVersion == "HTTP/1.0" && 
                    !connectionHeader.Equals("keep-alive", StringComparison.OrdinalIgnoreCase))
                    break;
                if (connectionHeader.Equals("close", StringComparison.OrdinalIgnoreCase))
                    break;
            }
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
                Console.WriteLine($"[NetWeb] Connection error: {ex.Message}");
        }
        finally
        {
            connection.Dispose();
        }
    }

    private async Task HandleRequestAsync(HttpConnection connection, HttpRequest request)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        NetWebDiagnostics.HttpServerActiveRequests.Add(1);

        Activity? activity = null;
        if (NetWebDiagnostics.ActivitySource.HasListeners())
        {
            activity = NetWebDiagnostics.ActivitySource.StartActivity("NetWeb.HttpRequest", ActivityKind.Server);
            if (activity != null)
            {
                activity.SetTag("http.request.method", request.Method);
                activity.SetTag("url.path", request.Path);
                activity.SetTag("url.original", request.RawUrl);
                activity.SetTag("network.protocol.version", request.HttpVersion);
                activity.SetTag("client.address", connection.RemoteEndPoint?.ToString());
            }
        }

        string? routeTemplate = null;
        int statusCode = 0;

        try
        {
            var path = request.Path;
            var method = request.Method;

            // 处理 Swagger
            if (_swaggerEnabled && await TryHandleSwaggerAsync(connection, path))
            {
                routeTemplate = path;
                statusCode = 200;
                activity?.SetTag("http.route", routeTemplate);
                return;
            }

            // 查找路由
            var (route, routeParams) = FindRoute(method, path);
            if (route == null)
            {
                await WriteNotFound(connection);
                statusCode = 404;
                return;
            }

            routeTemplate = route.Path;
            activity?.SetTag("http.route", routeTemplate);

            // 创建上下文
            var ctx = new Context(connection, request, routeParams, _jsonOptions);

            try
            {
                // 执行处理器链
                await ExecuteHandlers(ctx, route.Handlers);
            }
            finally
            {
                // 释放 DI 作用域（如果存在）
                var scope = ctx.Get<IDisposable>("__ServiceScope__");
                scope?.Dispose();
            }

            statusCode = ctx.Response.StatusCode;
        }
        catch (Exception ex)
        {
            statusCode = 500;
            NetWebDiagnostics.ExceptionsCount.Add(1,
                new KeyValuePair<string, object?>("exception.type", ex.GetType().FullName),
                new KeyValuePair<string, object?>("exception.message", ex.Message));

            if (activity != null)
            {
                activity.SetTag("exception.type", ex.GetType().FullName);
                activity.SetTag("exception.message", ex.Message);
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }

            await WriteError(connection, ex);
        }
        finally
        {
            NetWebDiagnostics.HttpServerActiveRequests.Add(-1);

            if (statusCode == 0)
                statusCode = 200;

            var durationSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;

            var tags = new TagList
            {
                { "http.request.method", request.Method },
                { "url.path", request.Path },
                { "http.response.status_code", statusCode }
            };
            if (!string.IsNullOrWhiteSpace(routeTemplate))
                tags.Add("http.route", routeTemplate);

            NetWebDiagnostics.HttpServerRequestCount.Add(1, tags);
            NetWebDiagnostics.HttpServerRequestDuration.Record(durationSeconds, tags);

            activity?.Dispose();
        }
    }

    private async Task ExecuteHandlers(Context ctx, HandlerFunc[] handlers)
    {
        foreach (var handler in handlers)
        {
            if (ctx.IsAborted)
                break;

            await handler(ctx);
        }
    }

    private (Route? route, Dictionary<string, string> routeParams) FindRoute(string method, string path)
    {
        foreach (var route in _routes)
        {
            if (!string.Equals(route.Method, method, StringComparison.OrdinalIgnoreCase))
                continue;

            if (route.Pattern.TryMatch(path, out var routeParams))
                return (route, routeParams);
        }

        return (null, new Dictionary<string, string>());
    }

    #endregion

    #region Swagger

    private async Task<bool> TryHandleSwaggerAsync(HttpConnection connection, string path)
    {
        if (path.Equals("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/swagger/", StringComparison.OrdinalIgnoreCase))
        {
            var html = GenerateSwaggerHtml();
            await WriteResponse(connection, 200, "text/html; charset=utf-8", html);
            return true;
        }

        if (path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
        {
            var doc = GenerateOpenApiDoc();
            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            await WriteResponse(connection, 200, "application/json; charset=utf-8", json);
            return true;
        }

        return false;
    }

    private object GenerateOpenApiDoc()
    {
        var paths = new Dictionary<string, object>();

        // 收集所有使用的 tags
        var usedTags = _routes
            .Where(r => !string.IsNullOrEmpty(r.Tag))
            .Select(r => r.Tag!)
            .Distinct()
            .OrderBy(t => t)
            .Select(t => new { name = t, description = $"{t} 相关接口" })
            .ToList();

        foreach (var routeGroup in _routes.GroupBy(r => r.OpenApiPath))
        {
            var operations = new Dictionary<string, object>();
            foreach (var route in routeGroup)
            {
                var operation = new Dictionary<string, object>
                {
                    ["operationId"] = $"{route.Method}_{route.Path.Replace("/", "_").Replace(":", "")}",
                    ["parameters"] = route.PathParameters.Select(p => new
                    {
                        name = p,
                        @in = "path",
                        required = true,
                        schema = new { type = "string" }
                    }).ToArray(),
                    ["responses"] = new Dictionary<string, object>
                    {
                        ["200"] = new { description = "OK" }
                    }
                };

                // 添加 tags
                if (!string.IsNullOrEmpty(route.Tag))
                {
                    operation["tags"] = new[] { route.Tag };
                }
                else
                {
                    // 默认根据路径第一段生成 tag
                    var defaultTag = GetDefaultTag(route.Path);
                    if (!string.IsNullOrEmpty(defaultTag))
                    {
                        operation["tags"] = new[] { defaultTag };
                        // 添加到 usedTags 中（如果还没有）
                        if (!usedTags.Any(t => t.name == defaultTag))
                        {
                            usedTags.Add(new { name = defaultTag, description = $"{defaultTag} 相关接口" });
                        }
                    }
                }

                operations[route.Method.ToLowerInvariant()] = operation;
            }
            paths[routeGroup.Key] = operations;
        }

        // 对 tags 排序
        usedTags = usedTags.OrderBy(t => t.name).ToList();

        return new
        {
            openapi = "3.0.1",
            info = new { title = _swaggerTitle, version = _swaggerVersion },
            tags = usedTags,
            paths
        };
    }

    /// <summary>
    /// 根据路径获取默认 Tag
    /// </summary>
    private static string? GetDefaultTag(string path)
    {
        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // 跳过 api、v1、v2 等前缀
        foreach (var segment in segments)
        {
            if (segment.Equals("api", StringComparison.OrdinalIgnoreCase))
                continue;
            if (segment.StartsWith("v", StringComparison.OrdinalIgnoreCase) && 
                segment.Length > 1 && char.IsDigit(segment[1]))
                continue;
            if (segment.StartsWith(":")) // 跳过参数
                continue;
            
            // 首字母大写
            return char.ToUpper(segment[0]) + segment[1..].ToLower();
        }
        
        return null;
    }

    private static string GenerateSwaggerHtml() => @"<!doctype html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <title>Swagger UI</title>
    <link rel=""stylesheet"" href=""https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"" />
</head>
<body>
    <div id=""swagger-ui""></div>
    <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js""></script>
    <script>
        window.onload = () => {
            SwaggerUIBundle({
                url: '/swagger/v1/swagger.json',
                dom_id: '#swagger-ui',
            });
        };
    </script>
</body>
</html>";

    #endregion

    #region 响应辅助

    private static async Task WriteResponse(HttpConnection connection, int statusCode, string contentType, string body)
    {
        var response = new HttpResponse
        {
            StatusCode = statusCode,
            Body = Encoding.UTF8.GetBytes(body)
        };
        response.SetHeader("Content-Type", contentType);
        await connection.WriteResponseAsync(response);
    }

    private static Task WriteNotFound(HttpConnection connection)
        => WriteResponse(connection, 404, "application/json", "{\"error\":\"Not Found\"}");

    private static Task WriteError(HttpConnection connection, Exception ex)
        => WriteResponse(connection, 500, "application/json", $"{{\"error\":\"{ex.Message.Replace("\"", "\\\"")}\"}}");

    #endregion
}
