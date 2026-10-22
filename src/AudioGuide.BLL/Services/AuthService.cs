using AudioGuide.BLL.Security;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IAuthService
{
    Task<AdminProfile?> LoginAsync(string username, string password);
}

public class AuthService : IAuthService
{
    private readonly IAdminUserRepository _users;
    public AuthService(IAdminUserRepository users) => _users = users;

    public async Task<AdminProfile?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;
        var user = await _users.GetByUsernameAsync(username.Trim());
        if (user == null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash)) return null;
        return new AdminProfile(user.Id, user.Username, user.DisplayName);
    }
}
