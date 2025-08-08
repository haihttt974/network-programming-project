using BCrypt.Net;
using DKyThucTap.Data;
using DKyThucTap.Models;
using DKyThucTap.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Services
{
    public class AuthService : IAuthService
    {
        private readonly DKyThucTapContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly INotificationIntegrationService _notificationIntegration;

        public AuthService(
            DKyThucTapContext context,
            ILogger<AuthService> logger,
            INotificationIntegrationService notificationIntegration)
        {
            _context = context;
            _logger = logger;
            _notificationIntegration = notificationIntegration;
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                    return (false, "Email hoặc mật khẩu không đúng", null);
                }

                _logger.LogInformation("Found user: {UserId}, Role: {RoleName}, IsActive: {IsActive}",
                    user.UserId, user.Role?.RoleName, user.IsActive);

                if (user.IsActive != true)
                {
                    _logger.LogWarning("Login attempt with inactive account: {Email}", loginDto.Email);
                    return (false, "Tài khoản đã bị vô hiệu hóa", null);
                }

                _logger.LogInformation("Verifying password for user: {UserId}", user.UserId);
                var passwordValid = VerifyPassword(loginDto.Password, user.PasswordHash);
                _logger.LogInformation("Password verification result: {IsValid}", passwordValid);

                if (!passwordValid)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email} - Invalid password", loginDto.Email);
                    return (false, "Email hoặc mật khẩu không đúng", null);
                }

                await UpdateLastLoginAsync(user.UserId);
                _logger.LogInformation("Successful login for user: {Email}, UserId: {UserId}", loginDto.Email, user.UserId);

                // Send login notification
                await SendLoginNotificationAsync(user.UserId, user.Email);

                return (true, "Đăng nhập thành công", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                return (false, "Có lỗi xảy ra trong quá trình đăng nhập", null);
            }
        }

        public async Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation("Starting registration for email: {Email}", registerDto.Email);

                // Check if email already exists
                if (await IsEmailExistsAsync(registerDto.Email))
                {
                    _logger.LogWarning("Registration failed - email already exists: {Email}", registerDto.Email);
                    return (false, "Email đã được sử dụng", null);
                }

                // Validate role exists
                var role = await _context.Roles.FindAsync(registerDto.RoleId);
                if (role == null)
                {
                    _logger.LogWarning("Registration failed - invalid role: {RoleId}", registerDto.RoleId);
                    return (false, "Vai trò không hợp lệ", null);
                }

                _logger.LogInformation("Creating user and profile for email: {Email}", registerDto.Email);

                // Use transaction now that retry strategy is disabled
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create user
                    var user = new User
                    {
                        Email = registerDto.Email,
                        PasswordHash = HashPassword(registerDto.Password),
                        RoleId = registerDto.RoleId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User created with ID: {UserId}", user.UserId);

                    // Create user profile
                    var userProfile = new UserProfile
                    {
                        UserId = user.UserId,
                        FirstName = registerDto.FirstName,
                        LastName = registerDto.LastName,
                        Phone = registerDto.Phone,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    _context.UserProfiles.Add(userProfile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User profile created for user: {UserId}", user.UserId);

                    await transaction.CommitAsync();

                    // Load user with related data
                    var completeUser = await _context.Users
                        .Include(u => u.Role)
                        .Include(u => u.UserProfile)
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                    _logger.LogInformation("New user registered successfully: {Email}, UserId: {UserId}",
                        registerDto.Email, user.UserId);

                    // Send welcome notification
                    await SendWelcomeNotificationAsync(user.UserId, registerDto.FirstName ?? "");

                    return (true, "Đăng ký thành công", completeUser);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in transaction during registration for email: {Email}", registerDto.Email);
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", registerDto.Email);
                return (false, "Có lỗi xảy ra trong quá trình đăng ký", null);
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                _logger.LogInformation("User logged out: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastLogin = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return null;

                return new UserProfileDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    RoleName = user.Role.RoleName,
                    FirstName = user.UserProfile?.FirstName,
                    LastName = user.UserProfile?.LastName,
                    Phone = user.UserProfile?.Phone,
                    Address = user.UserProfile?.Address,
                    Bio = user.UserProfile?.Bio,
                    ProfilePictureUrl = user.UserProfile?.ProfilePictureUrl,
                    CvUrl = user.UserProfile?.CvUrl,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    IsActive = user.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            try
            {
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userProfile == null)
                {
                    // Create new profile if doesn't exist
                    userProfile = new UserProfile
                    {
                        UserId = userId,
                        FirstName = updateDto.FirstName,
                        LastName = updateDto.LastName,
                        Phone = updateDto.Phone,
                        Address = updateDto.Address,
                        Bio = updateDto.Bio,
                        ProfilePictureUrl = updateDto.ProfilePictureUrl,
                        CvUrl = updateDto.CvUrl,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    _context.UserProfiles.Add(userProfile);
                }
                else
                {
                    // Update existing profile
                    userProfile.FirstName = updateDto.FirstName;
                    userProfile.LastName = updateDto.LastName;
                    userProfile.Phone = updateDto.Phone;
                    userProfile.Address = updateDto.Address;
                    userProfile.Bio = updateDto.Bio;
                    userProfile.ProfilePictureUrl = updateDto.ProfilePictureUrl;
                    userProfile.CvUrl = updateDto.CvUrl;
                    userProfile.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Send profile update notification
                await SendProfileUpdateNotificationAsync(userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                return true; // Return true to be safe
            }
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            try
            {
                _logger.LogInformation("Getting roles from database");
                var roles = await _context.Roles.ToListAsync();
                _logger.LogInformation("Found {RoleCount} roles: {RoleNames}",
                    roles.Count, string.Join(", ", roles.Select(r => r.RoleName)));
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles from database");
                return new List<Role>();
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        public string HashPassword(string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password");
                throw;
            }
        }

        // Notification Methods
        private async Task SendLoginNotificationAsync(int userId, string email)
        {
            try
            {
                _logger.LogInformation("Sending login notification for user: {UserId}", userId);

                await _notificationIntegration.NotifySecurityAlertAsync(
                    userId,
                    "Đăng nhập thành công",
                    $"Tài khoản {email} vừa đăng nhập vào hệ thống lúc {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending login notification for user: {UserId}", userId);
            }
        }

        private async Task SendWelcomeNotificationAsync(int userId, string firstName)
        {
            try
            {
                _logger.LogInformation("Sending welcome notification for user: {UserId}", userId);

                await _notificationIntegration.NotifyAccountVerificationAsync(
                    userId,
                    $"Chào mừng {firstName} đến với hệ thống tuyển dụng! Hãy hoàn thiện hồ sơ của bạn để có cơ hội việc làm tốt nhất."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome notification for user: {UserId}", userId);
            }
        }

        private async Task SendProfileUpdateNotificationAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Sending profile update notification for user: {UserId}", userId);

                await _notificationIntegration.NotifyAccountVerificationAsync(
                    userId,
                    "Hồ sơ của bạn đã được cập nhật thành công. Hồ sơ đầy đủ sẽ giúp bạn có nhiều cơ hội việc làm hơn."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending profile update notification for user: {UserId}", userId);
            }
        }
    }
}
