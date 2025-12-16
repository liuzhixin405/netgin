namespace LuckyDraw.Domain.Entities;

/// <summary>
/// 🎫 参与者实体
/// </summary>
public class Participant
{
    public int Id { get; set; }
    
    /// <summary>
    /// 所属活动 ID
    /// </summary>
    public int ActivityId { get; set; }
    
    /// <summary>
    /// 参与者名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 参与者联系方式
    /// </summary>
    public string Contact { get; set; } = string.Empty;
    
    /// <summary>
    /// 幸运号码
    /// </summary>
    public string LuckyNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否中奖
    /// </summary>
    public bool IsWinner { get; set; }
    
    /// <summary>
    /// 参与时间
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 生成幸运号码
    /// </summary>
    public void GenerateLuckyNumber()
    {
        // 生成格式: XXXXXXXX (8位随机数字)
        var random = new Random();
        LuckyNumber = random.Next(10000000, 99999999).ToString();
    }

    /// <summary>
    /// 标记为中奖
    /// </summary>
    public void MarkAsWinner()
    {
        IsWinner = true;
    }
}
