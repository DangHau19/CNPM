using AudioGuide.BLL.Security;
using AudioGuide.DAL.Repositories;

namespace AudioGuide.BLL.Services;

public interface IAuthService
{
    Task<AdminProfile?> LoginAsync(string username, string password);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
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

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _users.FindAsync(userId) ?? throw new NotFoundException("Không tìm thấy tài khoản.");
        if (string.IsNullOrEmpty(currentPassword) || !PasswordHasher.Verify(currentPassword, user.PasswordHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.");
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8 || newPassword.Length > 100)
            throw new BusinessException("Mật khẩu mới phải từ 8 đến 100 ký tự.");
        if (newPassword == currentPassword)
            throw new BusinessException("Mật khẩu mới phải khác mật khẩu hiện tại.");

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        await _users.SaveAsync();
    }
}
