using System.Buffers;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace NetWeb.Http;

/// <summary>
/// 高性能 HTTP 服务器（基于 Socket）
/// </summary>
public class HttpServer : IDisposable
{
    private readonly Socket _listener;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public HttpServer()
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.NoDelay = true;
    }

    /// <summary>
    /// 启动服务器
    /// </summary>
    public void Start(string host, int port, int backlog = 512)
    {
        var endpoint = new IPEndPoint(
            host == "localhost" || host == "+" || host == "*" ? IPAddress.Any : IPAddress.Parse(host),
            port);
        
        _listener.Bind(endpoint);
        _listener.Listen(backlog);
    }

    /// <summary>
    /// 接受连接
    /// </summary>
    public async Task<HttpConnection?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _listener.AcceptAsync(cancellationToken);
            return new HttpConnection(client);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        try { _listener.Shutdown(SocketShutdown.Both); } catch { }
        _listener.Close();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts.Dispose();
        _listener.Dispose();
    }
}

/// <summary>
/// HTTP 连接
/// </summary>
public class HttpConnection : IDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private bool _disposed;

    internal HttpConnection(Socket socket)
    {
        _socket = socket;
        _socket.NoDelay = true;
        _stream = new NetworkStream(_socket, ownsSocket: false);
    }

    public Stream Stream => _stream;
    public EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;

    /// <summary>
    /// 解析 HTTP 请求
    /// </summary>
    public async Task<HttpRequest?> ReadRequestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                var totalRead = 0;
                var headerEndIndex = -1;

                // 读取请求头
                while (headerEndIndex < 0 && totalRead < buffer.Length)
                {
                    var read = await _stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
                    if (read == 0) return null; // 连接关闭

                    totalRead += read;
                    headerEndIndex = FindHeaderEnd(buffer, totalRead);
                }

                if (headerEndIndex < 0)
                    throw new InvalidOperationException("Request headers too large");

                // 解析请求行和头部
                var headerText = Encoding.UTF8.GetString(buffer, 0, headerEndIndex);
                var request = ParseRequest(headerText);

                // 读取请求体
                if (request.ContentLength > 0)
                {
                    var bodyStartIndex = headerEndIndex + 4; // 跳过 \r\n\r\n
                    var bodyAlreadyRead = totalRead - bodyStartIndex;
                    var bodyBuffer = new byte[request.ContentLength];

                    if (bodyAlreadyRead > 0)
                    {
                        Buffer.BlockCopy(buffer, bodyStartIndex, bodyBuffer, 0, Math.Min(bodyAlreadyRead, (int)request.ContentLength));
                    }

                    var remaining = (int)request.ContentLength - bodyAlreadyRead;
                    if (remaining > 0)
                    {
                        var offset = bodyAlreadyRead;
                        while (remaining > 0)
                        {
                            var read = await _stream.ReadAsync(bodyBuffer.AsMemory(offset, remaining), cancellationToken);
                            if (read == 0) break;
                            offset += read;
                            remaining -= read;
                        }
                    }

                    request.Body = bodyBuffer;
                }

                return request;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// 发送响应
    /// </summary>
    public async Task WriteResponseAsync(HttpResponse response, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {response.StatusCode} {GetStatusMessage(response.StatusCode)}\r\n");

        foreach (var (key, value) in response.Headers)
        {
            sb.Append($"{key}: {value}\r\n");
        }

        if (response.Body != null && response.Body.Length > 0)
        {
            if (!response.Headers.ContainsKey("Content-Length"))
                sb.Append($"Content-Length: {response.Body.Length}\r\n");
        }
        else
        {
            sb.Append("Content-Length: 0\r\n");
        }

        sb.Append("\r\n");

        var headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
        await _stream.WriteAsync(headerBytes, cancellationToken);

        if (response.Body != null && response.Body.Length > 0)
        {
            await _stream.WriteAsync(response.Body, cancellationToken);
        }

        await _stream.FlushAsync(cancellationToken);
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        // 查找 \r\n\r\n
        for (int i = 0; i < length - 3; i++)
        {
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                return i;
        }
        return -1;
    }

    private static HttpRequest ParseRequest(string headerText)
    {
        var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new InvalidOperationException("Invalid HTTP request");

        // 解析请求行: GET /path HTTP/1.1
        var requestLine = lines[0].Split(' ');
        if (requestLine.Length < 3)
            throw new InvalidOperationException("Invalid request line");

        var method = requestLine[0];
        var rawUrl = requestLine[1];
        var version = requestLine[2];

        // 分离路径和查询字符串
        var queryIndex = rawUrl.IndexOf('?');
        var path = queryIndex >= 0 ? rawUrl[..queryIndex] : rawUrl;
        var queryString = queryIndex >= 0 ? rawUrl[(queryIndex + 1)..] : "";

        // 解析请求头
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            var colonIndex = lines[i].IndexOf(':');
            if (colonIndex > 0)
            {
                var key = lines[i][..colonIndex].Trim();
                var value = lines[i][(colonIndex + 1)..].Trim();
                headers[key] = value;
            }
        }

        // 解析查询参数
        var query = new NameValueCollection();
        if (!string.IsNullOrEmpty(queryString))
        {
            var pairs = queryString.Split('&');
            foreach (var pair in pairs)
            {
                var eqIndex = pair.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = HttpUtility.UrlDecode(pair[..eqIndex]);
                    var value = HttpUtility.UrlDecode(pair[(eqIndex + 1)..]);
                    query[key] = value;
                }
                else
                {
                    query[pair] = "";
                }
            }
        }

        return new HttpRequest
        {
            Method = method,
            Path = HttpUtility.UrlDecode(path),
            RawUrl = rawUrl,
            QueryString = query,
            Headers = headers,
            HttpVersion = version,
            ContentLength = headers.TryGetValue("Content-Length", out var cl) ? long.Parse(cl) : 0,
            ContentType = headers.GetValueOrDefault("Content-Type")
        };
    }

    private static string GetStatusMessage(int statusCode) => statusCode switch
    {
        200 => "OK",
        201 => "Created",
        204 => "No Content",
        301 => "Moved Permanently",
        302 => "Found",
        304 => "Not Modified",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _ => "Unknown"
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _stream.Dispose(); } catch { }
        try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        try { _socket.Close(); } catch { }
        try { _socket.Dispose(); } catch { }
    }
}

/// <summary>
/// HTTP 请求
/// </summary>
public class HttpRequest
{
    public string Method { get; init; } = "GET";
    public string Path { get; init; } = "/";
    public string RawUrl { get; init; } = "/";
    public string HttpVersion { get; init; } = "HTTP/1.1";
    public Dictionary<string, string> Headers { get; init; } = new();
    public NameValueCollection QueryString { get; init; } = new();
    public long ContentLength { get; init; }
    public string? ContentType { get; init; }
    public byte[]? Body { get; set; }
}

/// <summary>
/// HTTP 响应
/// </summary>
public class HttpResponse
{
    public int StatusCode { get; set; } = 200;
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public byte[]? Body { get; set; }

    public void SetHeader(string key, string value) => Headers[key] = value;
}
