using LuckyDraw.Domain.Entities;

namespace LuckyDraw.Repository;

/// <summary>
/// 抽奖活动仓储接口
/// </summary>
public interface ILuckyDrawRepository
{
    /// <summary>
    /// 获取活动
    /// </summary>
    Task<LuckyDrawActivity?> GetActivityByIdAsync(int id);
    
    /// <summary>
    /// 获取所有活动
    /// </summary>
    Task<IEnumerable<LuckyDrawActivity>> GetAllActivitiesAsync();
    
    /// <summary>
    /// 创建活动
    /// </summary>
    Task<int> CreateActivityAsync(LuckyDrawActivity activity);
    
    /// <summary>
    /// 更新活动
    /// </summary>
    Task<bool> UpdateActivityAsync(LuckyDrawActivity activity);
    
    /// <summary>
    /// 删除活动
    /// </summary>
    Task<bool> DeleteActivityAsync(int id);
}

/// <summary>
/// 参与者仓储接口
/// </summary>
public interface IParticipantRepository
{
    /// <summary>
    /// 获取参与者
    /// </summary>
    Task<Participant?> GetByIdAsync(int id);
    
    /// <summary>
    /// 获取活动的所有参与者
    /// </summary>
    Task<IEnumerable<Participant>> GetByActivityIdAsync(int activityId);
    
    /// <summary>
    /// 添加参与者
    /// </summary>
    Task<int> AddAsync(Participant participant);
    
    /// <summary>
    /// 更新参与者
    /// </summary>
    Task<bool> UpdateAsync(Participant participant);
    
    /// <summary>
    /// 批量更新参与者
    /// </summary>
    Task<bool> UpdateManyAsync(IEnumerable<Participant> participants);
    
    /// <summary>
    /// 检查是否已参与
    /// </summary>
    Task<bool> HasJoinedAsync(int activityId, string name, string contact);
    
    /// <summary>
    /// 获取活动的获奖者
    /// </summary>
    Task<IEnumerable<Participant>> GetWinnersByActivityIdAsync(int activityId);
}
