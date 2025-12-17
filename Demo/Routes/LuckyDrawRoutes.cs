using NetWeb;
using NetWeb.Extensions.DependencyInjection;
using LuckyDraw.Services;
using NetWeb.Demo.Models;

namespace NetWeb.Demo.Routes;

/// <summary>
/// 🎲 抽奖系统路由
/// </summary>
public static class LuckyDrawRoutes
{
    public static void MapLuckyDrawRoutes(this RouterGroup api)
    {
        var luckyDraw = api.Group("/lucky-draw");

        // 获取所有抽奖活动
        luckyDraw.GET("/activities", async ctx =>
        {
            var service = ctx.GetService<ILuckyDrawService>();
            var activities = await service!.GetAllActivitiesAsync();
            await ctx.JSON(new { success = true, data = activities });
        });

        // 获取单个活动详情
        luckyDraw.GET("/activities/:id", async ctx =>
        {
            var id = int.Parse(ctx.Param("id") ?? "0");
            var service = ctx.GetService<ILuckyDrawService>();
            var activity = await service!.GetActivityAsync(id);
            if (activity == null)
            {
                await ctx.NotFound(new { success = false, message = "活动不存在" });
                return;
            }
            await ctx.JSON(new { success = true, data = activity });
        });

        // 创建抽奖活动
        luckyDraw.POST("/activities", async ctx =>
        {
            var request = await ctx.BindAsync<CreateActivityRequest>();
            if (request == null)
            {
                await ctx.BadRequest(new { success = false, message = "无效的请求" });
                return;
            }

            var service = ctx.GetService<ILuckyDrawService>();
            var activity = await service!.CreateActivityAsync(
                request.Name,
                request.Description,
                request.Prize,
                request.MaxParticipants,
                request.WinnerCount);

            await ctx.Created(new { success = true, message = "🎲 抽奖活动创建成功！", data = activity });
        });

        // 开始活动
        luckyDraw.POST("/activities/:id/start", async ctx =>
        {
            var id = int.Parse(ctx.Param("id") ?? "0");
            var service = ctx.GetService<ILuckyDrawService>();

            try
            {
                await service!.StartActivityAsync(id);
                await ctx.OK(new { success = true, message = "🎉 活动已开始，快来参与吧！" });
            }
            catch (Exception ex)
            {
                await ctx.BadRequest(new { success = false, message = ex.Message });
            }
        });

        // 参与抽奖
        luckyDraw.POST("/activities/:id/join", async ctx =>
        {
            var id = int.Parse(ctx.Param("id") ?? "0");
            var request = await ctx.BindAsync<JoinActivityRequest>();
            if (request == null)
            {
                await ctx.BadRequest(new { success = false, message = "无效的请求" });
                return;
            }

            var service = ctx.GetService<ILuckyDrawService>();
            try
            {
                var participant = await service!.JoinActivityAsync(id, request.Name, request.Contact);
                await ctx.Created(new
                {
                    success = true,
                    message = $"🎫 参与成功！您的幸运号码是：{participant.LuckyNumber}",
                    data = new { participant.Id, participant.Name, participant.LuckyNumber, participant.JoinedAt }
                });
            }
            catch (Exception ex)
            {
                await ctx.BadRequest(new { success = false, message = ex.Message });
            }
        });

        // 获取活动参与者列表
        luckyDraw.GET("/activities/:id/participants", async ctx =>
        {
            var id = int.Parse(ctx.Param("id") ?? "0");
            var service = ctx.GetService<ILuckyDrawService>();
            var participants = await service!.GetParticipantsAsync(id);
            await ctx.JSON(new { success = true, data = participants });
        });

        // 🎲 执行抽奖！
        luckyDraw.POST("/activities/:id/draw", async ctx =>
        {
            var id = int.Parse(ctx.Param("id") ?? "0");
            var service = ctx.GetService<ILuckyDrawService>();

            try
            {
                var result = await service!.DrawWinnersAsync(id);
                await ctx.OK(new
                {
                    success = true,
                    message = result.Congratulations,
                    data = result
                });
            }
            catch (Exception ex)
            {
                await ctx.BadRequest(new { success = false, message = ex.Message });
            }
        });

        // 获取开奖结果
        luckyDraw.GET("/activities/:id/result", async ctx =>
        {
            var id = int.Parse(ctx.Param("id") ?? "0");
            var service = ctx.GetService<ILuckyDrawService>();
            var result = await service!.GetDrawResultAsync(id);

            if (result == null)
            {
                await ctx.NotFound(new { success = false, message = "活动未开奖或不存在" });
                return;
            }

            await ctx.JSON(new { success = true, data = result });
        });
    }
}
