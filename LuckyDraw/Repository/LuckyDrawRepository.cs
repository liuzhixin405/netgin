using System.Data;
using LuckyDraw.Domain.Entities;
using MiniGin.Extensions.Data;
using Dapper;

namespace LuckyDraw.Repository;

/// <summary>
/// 抽奖活动仓储实现
/// </summary>
public class LuckyDrawRepository : ILuckyDrawRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LuckyDrawRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LuckyDrawActivity?> GetActivityByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            SELECT Id, Name, Description, Prize, MaxParticipants, CurrentParticipants, 
                   WinnerCount, Status, CreatedAt, DrawTime 
            FROM LuckyDrawActivities 
            WHERE Id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<LuckyDrawActivity>(sql, new { Id = id });
    }

    public async Task<IEnumerable<LuckyDrawActivity>> GetAllActivitiesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            SELECT Id, Name, Description, Prize, MaxParticipants, CurrentParticipants, 
                   WinnerCount, Status, CreatedAt, DrawTime 
            FROM LuckyDrawActivities 
            ORDER BY CreatedAt DESC";
        
        return await connection.QueryAsync<LuckyDrawActivity>(sql);
    }

    public async Task<int> CreateActivityAsync(LuckyDrawActivity activity)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            INSERT INTO LuckyDrawActivities (Name, Description, Prize, MaxParticipants, CurrentParticipants, WinnerCount, Status, CreatedAt) 
            VALUES (@Name, @Description, @Prize, @MaxParticipants, @CurrentParticipants, @WinnerCount, @Status, @CreatedAt);
            SELECT LAST_INSERT_ID();";
        
        return await connection.ExecuteScalarAsync<int>(sql, activity);
    }

    public async Task<bool> UpdateActivityAsync(LuckyDrawActivity activity)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            UPDATE LuckyDrawActivities 
            SET Name = @Name, 
                Description = @Description, 
                Prize = @Prize, 
                MaxParticipants = @MaxParticipants, 
                CurrentParticipants = @CurrentParticipants, 
                WinnerCount = @WinnerCount, 
                Status = @Status, 
                DrawTime = @DrawTime
            WHERE Id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, activity);
        return affected > 0;
    }

    public async Task<bool> DeleteActivityAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = "DELETE FROM LuckyDrawActivities WHERE Id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}

/// <summary>
/// 参与者仓储实现
/// </summary>
public class ParticipantRepository : IParticipantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ParticipantRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Participant?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            SELECT Id, ActivityId, Name, Contact, LuckyNumber, IsWinner, JoinedAt 
            FROM Participants 
            WHERE Id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<Participant>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Participant>> GetByActivityIdAsync(int activityId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            SELECT Id, ActivityId, Name, Contact, LuckyNumber, IsWinner, JoinedAt 
            FROM Participants 
            WHERE ActivityId = @ActivityId
            ORDER BY JoinedAt";
        
        return await connection.QueryAsync<Participant>(sql, new { ActivityId = activityId });
    }

    public async Task<int> AddAsync(Participant participant)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            INSERT INTO Participants (ActivityId, Name, Contact, LuckyNumber, IsWinner, JoinedAt) 
            VALUES (@ActivityId, @Name, @Contact, @LuckyNumber, @IsWinner, @JoinedAt);
            SELECT LAST_INSERT_ID();";
        
        return await connection.ExecuteScalarAsync<int>(sql, participant);
    }

    public async Task<bool> UpdateAsync(Participant participant)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            UPDATE Participants 
            SET IsWinner = @IsWinner 
            WHERE Id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, participant);
        return affected > 0;
    }

    public async Task<bool> UpdateManyAsync(IEnumerable<Participant> participants)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        using var transaction = connection.BeginTransaction();
        try
        {
            const string sql = "UPDATE Participants SET IsWinner = @IsWinner WHERE Id = @Id";
            foreach (var participant in participants)
            {
                await connection.ExecuteAsync(sql, participant, transaction);
            }
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    public async Task<bool> HasJoinedAsync(int activityId, string name, string contact)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            SELECT COUNT(1) FROM Participants 
            WHERE ActivityId = @ActivityId AND (Name = @Name OR Contact = @Contact)";
        
        var count = await connection.ExecuteScalarAsync<int>(sql, new { ActivityId = activityId, Name = name, Contact = contact });
        return count > 0;
    }

    public async Task<IEnumerable<Participant>> GetWinnersByActivityIdAsync(int activityId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        const string sql = @"
            SELECT Id, ActivityId, Name, Contact, LuckyNumber, IsWinner, JoinedAt 
            FROM Participants 
            WHERE ActivityId = @ActivityId AND IsWinner = 1";
        
        return await connection.QueryAsync<Participant>(sql, new { ActivityId = activityId });
    }
}
