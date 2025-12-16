using LuckyDraw.Domain.Entities;
using LuckyDraw.Domain.ValueObjects;

namespace LuckyDraw.Services;

/// <summary>
/// 抽奖服务接口
/// </summary>
public interface ILuckyDrawService
{
    /// <summary>
    /// 创建抽奖活动
    /// </summary>
    Task<LuckyDrawActivity> CreateActivityAsync(string name, string description, string prize, int maxParticipants, int winnerCount = 1);
    
    /// <summary>
    /// 获取活动详情
    /// </summary>
    Task<LuckyDrawActivity?> GetActivityAsync(int activityId);
    
    /// <summary>
    /// 获取所有活动
    /// </summary>
    Task<IEnumerable<LuckyDrawActivity>> GetAllActivitiesAsync();
    
    /// <summary>
    /// 开始活动
    /// </summary>
    Task<bool> StartActivityAsync(int activityId);
    
    /// <summary>
    /// 参与抽奖
    /// </summary>
    Task<Participant> JoinActivityAsync(int activityId, string name, string contact);
    
    /// <summary>
    /// 获取活动参与者
    /// </summary>
    Task<IEnumerable<Participant>> GetParticipantsAsync(int activityId);
    
    /// <summary>
    /// 🎲 执行抽奖！
    /// </summary>
    Task<DrawResult> DrawWinnersAsync(int activityId);
    
    /// <summary>
    /// 获取开奖结果
    /// </summary>
    Task<DrawResult?> GetDrawResultAsync(int activityId);
}
