using LuckyDraw.Domain.Entities;

namespace LuckyDraw.Domain.ValueObjects;

/// <summary>
/// 🏆 开奖结果值对象
/// </summary>
public class DrawResult
{
    /// <summary>
    /// 活动 ID
    /// </summary>
    public int ActivityId { get; init; }
    
    /// <summary>
    /// 活动名称
    /// </summary>
    public string ActivityName { get; init; } = string.Empty;
    
    /// <summary>
    /// 奖品
    /// </summary>
    public string Prize { get; init; } = string.Empty;
    
    /// <summary>
    /// 获奖者列表
    /// </summary>
    public List<WinnerInfo> Winners { get; init; } = new();
    
    /// <summary>
    /// 开奖时间
    /// </summary>
    public DateTime DrawTime { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 总参与人数
    /// </summary>
    public int TotalParticipants { get; init; }

    /// <summary>
    /// 祝贺语
    /// </summary>
    public string Congratulations => Winners.Count switch
    {
        0 => "🎲 很遗憾，没有中奖者！",
        1 => $"🎉 恭喜 {Winners[0].Name} 成为本次活动的幸运儿！",
        _ => $"🎉 恭喜 {Winners.Count} 位幸运儿中奖！"
    };
}

/// <summary>
/// 获奖者信息
/// </summary>
public class WinnerInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LuckyNumber { get; init; } = string.Empty;
    public string Contact { get; init; } = string.Empty;
}
