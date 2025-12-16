using MiniGin.Mvc;
using LuckyDraw.Services;
using LuckyDraw.Domain.Entities;

namespace MiniGin.Demo.Controllers;

/// <summary>
/// 🎲 抽奖活动控制器
/// </summary>
[Route("/api/v2/lucky-draw")]
public class LuckyDrawController : ControllerBase
{
    private ILuckyDrawService Service => GetService<ILuckyDrawService>()!;

    /// <summary>
    /// 获取所有抽奖活动
    /// </summary>
    [HttpGet("activities")]
    public async Task GetActivities()
    {
        var activities = await Service.GetAllActivitiesAsync();
        await Ok(new { success = true, data = activities });
    }

    /// <summary>
    /// 获取单个活动详情
    /// </summary>
    [HttpGet("activities/:id")]
    public async Task GetActivity()
    {
        var id = int.Parse(Param("id") ?? "0");
        var activity = await Service.GetActivityAsync(id);
        
        if (activity == null)
        {
            await NotFound(new { success = false, message = "活动不存在" });
            return;
        }
        
        await Ok(new { success = true, data = activity });
    }

    /// <summary>
    /// 创建抽奖活动
    /// </summary>
    [HttpPost("activities")]
    public async Task CreateActivity()
    {
        var request = await BindAsync<CreateActivityDto>();
        if (request == null)
        {
            await BadRequest(new { success = false, message = "无效的请求" });
            return;
        }

        var activity = await Service.CreateActivityAsync(
            request.Name,
            request.Description,
            request.Prize,
            request.MaxParticipants,
            request.WinnerCount);

        await Created(new { success = true, message = "🎲 抽奖活动创建成功！", data = activity });
    }

    /// <summary>
    /// 开始活动
    /// </summary>
    [HttpPost("activities/:id/start")]
    public async Task StartActivity()
    {
        var id = int.Parse(Param("id") ?? "0");

        try
        {
            await Service.StartActivityAsync(id);
            await Ok(new { success = true, message = "🎉 活动已开始，快来参与吧！" });
        }
        catch (Exception ex)
        {
            await BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 参与抽奖
    /// </summary>
    [HttpPost("activities/:id/join")]
    public async Task JoinActivity()
    {
        var id = int.Parse(Param("id") ?? "0");
        var request = await BindAsync<JoinActivityDto>();
        
        if (request == null)
        {
            await BadRequest(new { success = false, message = "无效的请求" });
            return;
        }

        try
        {
            var participant = await Service.JoinActivityAsync(id, request.Name, request.Contact);
            await Created(new
            {
                success = true,
                message = $"🎫 参与成功！您的幸运号码是：{participant.LuckyNumber}",
                data = new { participant.Id, participant.Name, participant.LuckyNumber, participant.JoinedAt }
            });
        }
        catch (Exception ex)
        {
            await BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取活动参与者列表
    /// </summary>
    [HttpGet("activities/:id/participants")]
    public async Task GetParticipants()
    {
        var id = int.Parse(Param("id") ?? "0");
        var participants = await Service.GetParticipantsAsync(id);
        await Ok(new { success = true, data = participants });
    }

    /// <summary>
    /// 🎲 执行抽奖！
    /// </summary>
    [HttpPost("activities/:id/draw")]
    public async Task DrawWinners()
    {
        var id = int.Parse(Param("id") ?? "0");

        try
        {
            var result = await Service.DrawWinnersAsync(id);
            await Ok(new
            {
                success = true,
                message = result.Congratulations,
                data = result
            });
        }
        catch (Exception ex)
        {
            await BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取开奖结果
    /// </summary>
    [HttpGet("activities/:id/result")]
    public async Task GetDrawResult()
    {
        var id = int.Parse(Param("id") ?? "0");
        var result = await Service.GetDrawResultAsync(id);

        if (result == null)
        {
            await NotFound(new { success = false, message = "活动未开奖或不存在" });
            return;
        }

        await Ok(new { success = true, data = result });
    }
}

// DTO 类
public record CreateActivityDto(
    string Name,
    string Description,
    string Prize,
    int MaxParticipants,
    int WinnerCount = 1);

public record JoinActivityDto(string Name, string Contact);
