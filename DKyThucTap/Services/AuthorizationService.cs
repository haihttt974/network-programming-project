using DKyThucTap.Data;
using DKyThucTap.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DKyThucTap.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(DKyThucTapContext context, ILogger<AuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return false;

                return HasPermissionAsync(user, permission).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(User user, string permission)
        {
            try
            {
                if (user?.Role?.Permissions == null) return false;

                var permissions = ParsePermissions(user.Role.Permissions);
                return permissions.ContainsKey(permission) && permissions[permission];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, user?.UserId);
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> GetUserPermissionsAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return new Dictionary<string, bool>();

                return await GetUserPermissionsAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return new Dictionary<string, bool>();
            }
        }

        public async Task<Dictionary<string, bool>> GetUserPermissionsAsync(User user)
        {
            try
            {
                if (user?.Role?.Permissions == null) return new Dictionary<string, bool>();

                return ParsePermissions(user.Role.Permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", user?.UserId);
                return new Dictionary<string, bool>();
            }
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                return user?.Role?.RoleName?.Equals(roleName, StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role {RoleName} for user {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName)
        {
            try
            {
                return user?.Role?.RoleName?.Equals(roleName, StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role {RoleName} for user {UserId}", roleName, user?.UserId);
                return false;
            }
        }

        public Dictionary<string, bool> ParsePermissions(string? permissionsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(permissionsJson))
                    return new Dictionary<string, bool>();

                var permissions = JsonSerializer.Deserialize<Dictionary<string, bool>>(permissionsJson);
                return permissions ?? new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing permissions JSON: {PermissionsJson}", permissionsJson);
                return new Dictionary<string, bool>();
            }
        }

        public async Task<bool> InitializeDefaultRolesAsync()
        {
            try
            {
                _logger.LogInformation("Checking if roles need to be initialized");

                // Check if roles already exist
                var existingRoleCount = await _context.Roles.CountAsync();
                if (existingRoleCount > 0)
                {
                    _logger.LogInformation("Roles already exist ({RoleCount} roles found), skipping initialization", existingRoleCount);
                    return true;
                }

                _logger.LogInformation("No roles found, initializing default roles");

                var roles = new List<Role>
                {
                    new Role
                    {
                        RoleName = "Candidate",
                        Permissions = JsonSerializer.Serialize(new Dictionary<string, bool>
                        {
                            {"view_positions", true},
                            {"apply_position", true},
                            {"view_applications", true},
                            {"send_messages", true}
                        })
                    },
                    new Role
                    {
                        RoleName = "Recruiter",
                        Permissions = JsonSerializer.Serialize(new Dictionary<string, bool>
                        {
                            {"create_position", true},
                            {"manage_applications", true},
                            {"view_candidates", true},
                            {"send_messages", true},
                            {"create_company", true}
                        })
                    },
                    new Role
                    {
                        RoleName = "Admin",
                        Permissions = JsonSerializer.Serialize(new Dictionary<string, bool>
                        {
                            {"manage_users", true},
                            {"manage_companies", true},
                            {"manage_positions", true},
                            {"view_all_data", true},
                            {"moderate_reviews", true}
                        })
                    }
                };

                _context.Roles.AddRange(roles);
                var savedCount = await _context.SaveChangesAsync();

                _logger.LogInformation("Default roles initialized successfully. Created {RoleCount} roles", savedCount);

                // Verify roles were created
                var finalRoleCount = await _context.Roles.CountAsync();
                _logger.LogInformation("Total roles in database after initialization: {RoleCount}", finalRoleCount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default roles");
                return false;
            }
        }
    }
}
