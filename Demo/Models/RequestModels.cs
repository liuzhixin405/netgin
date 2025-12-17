namespace NetWeb.Demo.Models;

// ==================== 请求模型 ====================

/// <summary>
/// 创建用户请求
/// </summary>
public record CreateUserRequest(string Name, string Email);

/// <summary>
/// 更新用户请求
/// </summary>
public record UpdateUserRequest(string? Name, string? Email);

// 🎲 抽奖系统请求模型

/// <summary>
/// 创建抽奖活动请求
/// </summary>
public record CreateActivityRequest(
    string Name, 
    string Description, 
    string Prize, 
    int MaxParticipants, 
    int WinnerCount = 1);

/// <summary>
/// 参与抽奖请求
/// </summary>
public record JoinActivityRequest(string Name, string Contact);
