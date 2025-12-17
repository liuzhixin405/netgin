namespace NetWeb.Demo.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    IEnumerable<UserDto> GetUsers();
}

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    public IEnumerable<UserDto> GetUsers() =>
    [
        new UserDto(1, "Alice", "alice@example.com"),
        new UserDto(2, "Bob", "bob@example.com"),
        new UserDto(3, "Charlie", "charlie@example.com")
    ];
}

/// <summary>
/// 用户 DTO
/// </summary>
public record UserDto(int Id, string Name, string Email);
