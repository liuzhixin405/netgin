using LuckyDraw.Domain.Entities;
using LuckyDraw.Domain.ValueObjects;
using LuckyDraw.Repository;

namespace LuckyDraw.Services;

/// <summary>
/// 🎲 抽奖服务实现
/// </summary>
public class LuckyDrawService : ILuckyDrawService
{
    private readonly ILuckyDrawRepository _activityRepository;
    private readonly IParticipantRepository _participantRepository;

    public LuckyDrawService(
        ILuckyDrawRepository activityRepository,
        IParticipantRepository participantRepository)
    {
        _activityRepository = activityRepository;
        _participantRepository = participantRepository;
    }

    public async Task<LuckyDrawActivity> CreateActivityAsync(
        string name, 
        string description, 
        string prize, 
        int maxParticipants, 
        int winnerCount = 1)
    {
        var activity = new LuckyDrawActivity
        {
            Name = name,
            Description = description,
            Prize = prize,
            MaxParticipants = maxParticipants,
            WinnerCount = winnerCount,
            Status = ActivityStatus.NotStarted,
            CreatedAt = DateTime.Now
        };

        var id = await _activityRepository.CreateActivityAsync(activity);
        activity.Id = id;
        
        return activity;
    }

    public async Task<LuckyDrawActivity?> GetActivityAsync(int activityId)
    {
        return await _activityRepository.GetActivityByIdAsync(activityId);
    }

    public async Task<IEnumerable<LuckyDrawActivity>> GetAllActivitiesAsync()
    {
        return await _activityRepository.GetAllActivitiesAsync();
    }

    public async Task<bool> StartActivityAsync(int activityId)
    {
        var activity = await _activityRepository.GetActivityByIdAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException($"活动 {activityId} 不存在");

        activity.Start();
        return await _activityRepository.UpdateActivityAsync(activity);
    }

    public async Task<Participant> JoinActivityAsync(int activityId, string name, string contact)
    {
        var activity = await _activityRepository.GetActivityByIdAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException($"活动 {activityId} 不存在");

        if (!activity.CanJoin())
            throw new InvalidOperationException("活动不可参与：可能已结束或已满员");

        // 检查是否已参与
        if (await _participantRepository.HasJoinedAsync(activityId, name, contact))
            throw new InvalidOperationException("您已经参与过此活动了");

        var participant = new Participant
        {
            ActivityId = activityId,
            Name = name,
            Contact = contact,
            JoinedAt = DateTime.Now
        };
        
        // 生成幸运号码
        participant.GenerateLuckyNumber();

        var id = await _participantRepository.AddAsync(participant);
        participant.Id = id;

        // 更新参与人数
        activity.CurrentParticipants++;
        await _activityRepository.UpdateActivityAsync(activity);

        return participant;
    }

    public async Task<IEnumerable<Participant>> GetParticipantsAsync(int activityId)
    {
        return await _participantRepository.GetByActivityIdAsync(activityId);
    }

    public async Task<DrawResult> DrawWinnersAsync(int activityId)
    {
        var activity = await _activityRepository.GetActivityByIdAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException($"活动 {activityId} 不存在");

        if (!activity.CanDraw())
            throw new InvalidOperationException("活动不可开奖：可能未开始或没有参与者");

        var participants = (await _participantRepository.GetByActivityIdAsync(activityId)).ToList();
        
        // 🎲 随机抽取获奖者
        var random = new Random();
        var winnerCount = Math.Min(activity.WinnerCount, participants.Count);
        var winners = participants
            .OrderBy(_ => random.Next())
            .Take(winnerCount)
            .ToList();

        // 标记获奖者
        foreach (var winner in winners)
        {
            winner.MarkAsWinner();
        }
        await _participantRepository.UpdateManyAsync(winners);

        // 完成活动
        activity.Complete();
        await _activityRepository.UpdateActivityAsync(activity);

        return new DrawResult
        {
            ActivityId = activity.Id,
            ActivityName = activity.Name,
            Prize = activity.Prize,
            TotalParticipants = participants.Count,
            DrawTime = DateTime.Now,
            Winners = winners.Select(w => new WinnerInfo
            {
                Id = w.Id,
                Name = w.Name,
                LuckyNumber = w.LuckyNumber,
                Contact = MaskContact(w.Contact)
            }).ToList()
        };
    }

    public async Task<DrawResult?> GetDrawResultAsync(int activityId)
    {
        var activity = await _activityRepository.GetActivityByIdAsync(activityId);
        if (activity == null || activity.Status != ActivityStatus.Completed)
            return null;

        var winners = await _participantRepository.GetWinnersByActivityIdAsync(activityId);
        var allParticipants = await _participantRepository.GetByActivityIdAsync(activityId);

        return new DrawResult
        {
            ActivityId = activity.Id,
            ActivityName = activity.Name,
            Prize = activity.Prize,
            TotalParticipants = allParticipants.Count(),
            DrawTime = activity.DrawTime ?? DateTime.Now,
            Winners = winners.Select(w => new WinnerInfo
            {
                Id = w.Id,
                Name = w.Name,
                LuckyNumber = w.LuckyNumber,
                Contact = MaskContact(w.Contact)
            }).ToList()
        };
    }

    /// <summary>
    /// 脱敏联系方式
    /// </summary>
    private static string MaskContact(string contact)
    {
        if (string.IsNullOrEmpty(contact) || contact.Length < 4)
            return "****";
        
        return contact[..3] + "****" + contact[^3..];
    }
}
