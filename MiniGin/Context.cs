using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MiniGin.Http;

namespace MiniGin;

/// <summary>
/// 请求上下文 - 封装 HTTP 请求/响应的所有操作
/// </summary>
public sealed class Context
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, string> _params;
    private readonly Dictionary<string, object> _items = new();
    private bool _responseSent;
    private string? _cachedBody;

    // 内部使用的 HTTP 对象
    private readonly HttpRequest _request;
    private readonly HttpResponse _response;
    private readonly HttpConnection _connection;

    internal Context(HttpConnection connection, HttpRequest request, Dictionary<string, string> routeParams, JsonSerializerOptions jsonOptions)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _response = new HttpResponse();
        _params = routeParams ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _jsonOptions = jsonOptions;
    }

    #region 基础属性

    /// <summary>HTTP 请求对象</summary>
    public HttpRequest Request => _request;

    /// <summary>HTTP 响应对象</summary>
    public HttpResponse Response => _response;

    /// <summary>请求路径</summary>
    public string Path => _request.Path;

    /// <summary>请求方法</summary>
    public string Method => _request.Method;

    /// <summary>完整 URL</summary>
    public string FullUrl => _request.RawUrl;

    /// <summary>客户端 IP</summary>
    public string ClientIP => _connection.RemoteEndPoint?.ToString()?.Split(':')[0] ?? "";

    /// <summary>Content-Type</summary>
    public string? ContentType => _request.ContentType;

    /// <summary>是否已中止</summary>
    public bool IsAborted { get; private set; }

    #endregion

    #region 路由参数

    /// <summary>获取路由参数</summary>
    public string? Param(string key)
        => _params.TryGetValue(key, out var value) ? value : null;

    /// <summary>获取路由参数（带默认值）</summary>
    public string Param(string key, string defaultValue)
        => _params.TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>获取所有路由参数</summary>
    public IReadOnlyDictionary<string, string> Params => _params;

    #endregion

    #region 查询参数

    /// <summary>获取查询参数</summary>
    public string? Query(string key)
        => _request.QueryString[key];

    /// <summary>获取查询参数（带默认值）</summary>
    public string Query(string key, string defaultValue)
        => _request.QueryString[key] ?? defaultValue;

    /// <summary>获取查询参数并转换类型</summary>
    public T? Query<T>(string key) where T : struct
    {
        var value = _request.QueryString[key];
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>获取所有查询参数的 key</summary>
    public string[] QueryKeys => _request.QueryString.AllKeys!;

    #endregion

    #region 请求头

    /// <summary>获取请求头</summary>
    public string? GetHeader(string key)
        => _request.Headers.GetValueOrDefault(key);

    /// <summary>获取请求头（带默认值）</summary>
    public string GetHeader(string key, string defaultValue)
        => _request.Headers.GetValueOrDefault(key) ?? defaultValue;

    #endregion

    #region 请求体

    /// <summary>读取原始请求体</summary>
    public Task<string> GetRawBodyAsync()
    {
        if (_cachedBody != null)
            return Task.FromResult(_cachedBody);

        if (_request.Body == null || _request.Body.Length == 0)
            return Task.FromResult(_cachedBody = string.Empty);

        _cachedBody = Encoding.UTF8.GetString(_request.Body);
        return Task.FromResult(_cachedBody);
    }

    /// <summary>绑定 JSON 请求体到对象</summary>
    public async Task<T?> BindAsync<T>() where T : class
    {
        var body = await GetRawBodyAsync();
        if (string.IsNullOrWhiteSpace(body))
            return null;

        return JsonSerializer.Deserialize<T>(body, _jsonOptions);
    }

    /// <summary>绑定 JSON 请求体到对象（带默认值）</summary>
    public async Task<T> BindAsync<T>(T defaultValue) where T : class
    {
        var result = await BindAsync<T>();
        return result ?? defaultValue;
    }

    /// <summary>必须绑定成功，否则抛异常</summary>
    public async Task<T> MustBindAsync<T>() where T : class
    {
        var result = await BindAsync<T>();
        return result ?? throw new InvalidOperationException($"Failed to bind request body to {typeof(T).Name}");
    }

    #endregion

    #region 上下文数据

    /// <summary>设置上下文数据</summary>
    public void Set(string key, object value) => _items[key] = value;

    /// <summary>获取上下文数据</summary>
    public T? Get<T>(string key) where T : class
        => _items.TryGetValue(key, out var value) ? value as T : null;

    /// <summary>获取上下文数据（带默认值）</summary>
    public T Get<T>(string key, T defaultValue) where T : class
        => _items.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;

    /// <summary>是否存在上下文数据</summary>
    public bool Has(string key) => _items.ContainsKey(key);

    #endregion

    #region 响应方法

    /// <summary>中止请求处理</summary>
    public void Abort() => IsAborted = true;

    /// <summary>设置响应头</summary>
    public Context Header(string key, string value)
    {
        _response.SetHeader(key, value);
        return this;
    }

    /// <summary>设置状态码并结束响应</summary>
    public async Task Status(int statusCode)
    {
        if (!TryStartResponse()) return;

        _response.StatusCode = statusCode;
        _response.Body = null;
        await _connection.WriteResponseAsync(_response);
    }

    /// <summary>返回纯文本</summary>
    public async Task String(int statusCode, string content)
    {
        if (!TryStartResponse()) return;

        var bytes = Encoding.UTF8.GetBytes(content);
        _response.StatusCode = statusCode;
        _response.SetHeader("Content-Type", "text/plain; charset=utf-8");
        _response.Body = bytes;
        await _connection.WriteResponseAsync(_response);
    }

    /// <summary>返回 HTML</summary>
    public async Task HTML(int statusCode, string html)
    {
        if (!TryStartResponse()) return;

        var bytes = Encoding.UTF8.GetBytes(html);
        _response.StatusCode = statusCode;
        _response.SetHeader("Content-Type", "text/html; charset=utf-8");
        _response.Body = bytes;
        await _connection.WriteResponseAsync(_response);
    }

    /// <summary>返回 JSON</summary>
    public async Task JSON(int statusCode, object? data)
    {
        if (!TryStartResponse()) return;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);
        _response.StatusCode = statusCode;
        _response.SetHeader("Content-Type", "application/json; charset=utf-8");
        _response.Body = bytes;
        await _connection.WriteResponseAsync(_response);
    }

    /// <summary>返回 JSON（200 状态码）</summary>
    public Task JSON(object? data) => JSON(200, data);

    /// <summary>返回原始字节</summary>
    public async Task Data(int statusCode, string contentType, byte[] data)
    {
        if (!TryStartResponse()) return;

        _response.StatusCode = statusCode;
        _response.SetHeader("Content-Type", contentType);
        _response.Body = data;
        await _connection.WriteResponseAsync(_response);
    }

    /// <summary>重定向</summary>
    public async Task Redirect(int statusCode, string location)
    {
        if (!TryStartResponse()) return;

        _response.StatusCode = statusCode;
        _response.SetHeader("Location", location);
        _response.Body = null;
        await _connection.WriteResponseAsync(_response);
    }

    /// <summary>重定向（302）</summary>
    public Task Redirect(string location) => Redirect(302, location);

    #endregion

    #region 快捷响应方法

    /// <summary>200 OK</summary>
    public Task OK(object? data = null) => data == null ? Status(200) : JSON(200, data);

    /// <summary>201 Created</summary>
    public Task Created(object? data = null) => data == null ? Status(201) : JSON(201, data);

    /// <summary>204 No Content</summary>
    public Task NoContent() => Status(204);

    /// <summary>400 Bad Request</summary>
    public Task BadRequest(object? error = null)
        => JSON(400, error ?? new { error = "Bad Request" });

    /// <summary>401 Unauthorized</summary>
    public Task Unauthorized(object? error = null)
        => JSON(401, error ?? new { error = "Unauthorized" });

    /// <summary>403 Forbidden</summary>
    public Task Forbidden(object? error = null)
        => JSON(403, error ?? new { error = "Forbidden" });

    /// <summary>404 Not Found</summary>
    public Task NotFound(object? error = null)
        => JSON(404, error ?? new { error = "Not Found" });

    /// <summary>500 Internal Server Error</summary>
    public Task InternalServerError(object? error = null)
        => JSON(500, error ?? new { error = "Internal Server Error" });

    /// <summary>中止并返回状态码</summary>
    public Task AbortWithStatus(int statusCode)
    {
        Abort();
        return Status(statusCode);
    }

    /// <summary>中止并返回 JSON 错误</summary>
    public Task AbortWithJSON(int statusCode, object error)
    {
        Abort();
        return JSON(statusCode, error);
    }

    #endregion

    #region 私有方法

    private bool TryStartResponse()
    {
        if (_responseSent) return false;
        _responseSent = true;
        return true;
    }

    #endregion
}
