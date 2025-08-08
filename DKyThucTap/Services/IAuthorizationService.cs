using DKyThucTap.Models;

namespace DKyThucTap.Services
{
    public interface IAuthorizationService
    {
        Task<bool> HasPermissionAsync(int userId, string permission);
        Task<bool> HasPermissionAsync(User user, string permission);
        Task<Dictionary<string, bool>> GetUserPermissionsAsync(int userId);
        Task<Dictionary<string, bool>> GetUserPermissionsAsync(User user);
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<bool> IsInRoleAsync(User user, string roleName);
        Dictionary<string, bool> ParsePermissions(string? permissionsJson);
        Task<bool> InitializeDefaultRolesAsync();
    }
}
