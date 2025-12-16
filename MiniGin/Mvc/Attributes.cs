namespace MiniGin.Mvc;

/// <summary>
/// 标记控制器的路由前缀
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RouteAttribute : Attribute
{
    public string Template { get; }

    public RouteAttribute(string template)
    {
        Template = template;
    }
}

/// <summary>
/// HTTP 方法特性基类
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class HttpMethodAttribute : Attribute
{
    public string Template { get; }
    public abstract string Method { get; }

    protected HttpMethodAttribute(string template = "")
    {
        Template = template;
    }
}

/// <summary>
/// HTTP GET 方法
/// </summary>
public class HttpGetAttribute : HttpMethodAttribute
{
    public override string Method => "GET";
    public HttpGetAttribute(string template = "") : base(template) { }
}

/// <summary>
/// HTTP POST 方法
/// </summary>
public class HttpPostAttribute : HttpMethodAttribute
{
    public override string Method => "POST";
    public HttpPostAttribute(string template = "") : base(template) { }
}

/// <summary>
/// HTTP PUT 方法
/// </summary>
public class HttpPutAttribute : HttpMethodAttribute
{
    public override string Method => "PUT";
    public HttpPutAttribute(string template = "") : base(template) { }
}

/// <summary>
/// HTTP DELETE 方法
/// </summary>
public class HttpDeleteAttribute : HttpMethodAttribute
{
    public override string Method => "DELETE";
    public HttpDeleteAttribute(string template = "") : base(template) { }
}

/// <summary>
/// HTTP PATCH 方法
/// </summary>
public class HttpPatchAttribute : HttpMethodAttribute
{
    public override string Method => "PATCH";
    public HttpPatchAttribute(string template = "") : base(template) { }
}
