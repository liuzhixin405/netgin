using MiniGin;
using MiniGin.Extensions.DependencyInjection;
using MiniGin.Demo.Models;
using MiniGin.Demo.Services;

namespace MiniGin.Demo.Routes;

/// <summary>
/// 用户相关路由
/// </summary>
public static class UserRoutes
{
    public static void MapUserRoutes(this RouterGroup api)
    {
        // RESTful 风格路由
        api.GET("/users", async ctx =>
        {
            var userService = ctx.GetService<IUserService>();
            var users = userService?.GetUsers() ?? [];
            var page = ctx.Query<int>("page") ?? 1;
            var size = ctx.Query<int>("size") ?? 10;
            await ctx.JSON(new { users, page, size });
        });

        api.GET("/users/:id", async ctx =>
        {
            var id = ctx.Param("id");
            await ctx.JSON(new { id, name = $"User_{id}" });
        });

        api.POST("/users", async ctx =>
        {
            var user = await ctx.BindAsync<CreateUserRequest>();
            if (user == null)
            {
                await ctx.BadRequest(new { error = "Invalid request body" });
                return;
            }
            await ctx.Created(new { id = 1, name = user.Name, email = user.Email });
        });

        api.PUT("/users/:id", async ctx =>
        {
            var id = ctx.Param("id");
            var user = await ctx.BindAsync<UpdateUserRequest>();
            await ctx.OK(new { id, updated = true, name = user?.Name });
        });

        api.DELETE("/users/:id", async ctx =>
        {
            var id = ctx.Param("id");
            await ctx.OK(new { id, deleted = true });
        });
    }
}
