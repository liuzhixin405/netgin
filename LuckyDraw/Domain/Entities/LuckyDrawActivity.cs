namespace LuckyDraw.Domain.Entities;

/// <summary>
/// 🎲 抽奖活动 - 聚合根
/// </summary>
public class LuckyDrawActivity
{
    public int Id { get; set; }
    
    /// <summary>
    /// 活动名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 活动描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 奖品
    /// </summary>
    public string Prize { get; set; } = string.Empty;
    
    /// <summary>
    /// 最大参与人数
    /// </summary>
    public int MaxParticipants { get; set; }
    
    /// <summary>
    /// 当前参与人数
    /// </summary>
    public int CurrentParticipants { get; set; }
    
    /// <summary>
    /// 获奖者数量
    /// </summary>
    public int WinnerCount { get; set; } = 1;
    
    /// <summary>
    /// 活动状态
    /// </summary>
    public ActivityStatus Status { get; set; } = ActivityStatus.NotStarted;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 开奖时间
    /// </summary>
    public DateTime? DrawTime { get; set; }

    /// <summary>
    /// 检查是否可以参与
    /// </summary>
    public bool CanJoin() => Status == ActivityStatus.InProgress && CurrentParticipants < MaxParticipants;

    /// <summary>
    /// 检查是否可以开奖
    /// </summary>
    public bool CanDraw() => Status == ActivityStatus.InProgress && CurrentParticipants > 0;

    /// <summary>
    /// 开始活动
    /// </summary>
    public void Start()
    {
        if (Status != ActivityStatus.NotStarted)
            throw new InvalidOperationException("活动已经开始或已结束");
        Status = ActivityStatus.InProgress;
    }

    /// <summary>
    /// 完成活动
    /// </summary>
    public void Complete()
    {
        Status = ActivityStatus.Completed;
        DrawTime = DateTime.Now;
    }
}

/// <summary>
/// 活动状态
/// </summary>
public enum ActivityStatus
{
    /// <summary>未开始</summary>
    NotStarted = 0,
    /// <summary>进行中</summary>
    InProgress = 1,
    /// <summary>已完成</summary>
    Completed = 2,
    /// <summary>已取消</summary>
    Cancelled = 3
}
