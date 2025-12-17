using System.Reflection;

namespace NetWeb.Mvc;

/// <summary>
/// 控制器扩展方法
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// 注册单个控制器
    /// </summary>
    public static Engine MapController<TController>(this Engine engine) where TController : ControllerBase, new()
    {
        MapControllerInternal(engine, typeof(TController));
        return engine;
    }

    /// <summary>
    /// 注册单个控制器到指定路由组
    /// </summary>
    public static RouterGroup MapController<TController>(this RouterGroup group) where TController : ControllerBase, new()
    {
        MapControllerInternal(group, typeof(TController));
        return group;
    }

    /// <summary>
    /// 扫描并注册程序集中所有控制器
    /// </summary>
    public static Engine MapControllers(this Engine engine, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        
        var controllerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        foreach (var controllerType in controllerTypes)
        {
            MapControllerInternal(engine, controllerType);
        }

        return engine;
    }

    private static void MapControllerInternal(RouterGroup group, Type controllerType)
    {
        // 获取路由前缀
        var routeAttr = controllerType.GetCustomAttribute<RouteAttribute>();
        var routePrefix = routeAttr?.Template ?? $"/{controllerType.Name.Replace("Controller", "").ToLower()}";

        // 创建路由组
        var controllerGroup = group.Group(routePrefix);

        // 获取所有 action 方法
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            var httpMethodAttr = method.GetCustomAttribute<HttpMethodAttribute>();
            if (httpMethodAttr == null) continue;

            var actionTemplate = httpMethodAttr.Template;
            var httpMethod = httpMethodAttr.Method;

            // 创建路由处理器
            HandlerFunc handler = async ctx =>
            {
                // 创建控制器实例
                var controller = (ControllerBase)Activator.CreateInstance(controllerType)!;
                controller.Context = ctx;

                try
                {
                    // 调用 action 方法
                    var result = method.Invoke(controller, null);
                    
                    if (result is Task task)
                    {
                        await task;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    // 解包内部异常
                    throw ex.InnerException ?? ex;
                }
            };

            // 注册路由
            switch (httpMethod)
            {
                case "GET":
                    controllerGroup.GET(actionTemplate, handler);
                    break;
                case "POST":
                    controllerGroup.POST(actionTemplate, handler);
                    break;
                case "PUT":
                    controllerGroup.PUT(actionTemplate, handler);
                    break;
                case "DELETE":
                    controllerGroup.DELETE(actionTemplate, handler);
                    break;
                case "PATCH":
                    controllerGroup.PATCH(actionTemplate, handler);
                    break;
            }
        }
    }
}
