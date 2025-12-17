using NetWeb;

namespace NetWeb.Demo.Routes;

/// <summary>
/// 管理员相关路由
/// </summary>
public static class AdminRoutes
{
    public static void MapAdminRoutes(this RouterGroup api)
    {
        // 嵌套分组
        var admin = api.Group("/admin");
        admin.Use(Middleware.BasicAuth((user, pass) => user == "admin" && pass == "123456"));

        admin.GET("/dashboard", async ctx =>
        {
            var user = ctx.Get<string>("user");
            await ctx.JSON(new { message = $"Welcome {user}!", role = "admin" });
        });
    }
}
