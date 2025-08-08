using DKyThucTap.Data;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Controllers
{
    public class DebugController : Controller
    {
        private readonly DKyThucTapContext _context;
        private readonly IAuthService _authService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<DebugController> _logger;

        public DebugController(
            DKyThucTapContext context,
            IAuthService authService,
            IAuthorizationService authorizationService,
            ILogger<DebugController> logger)
        {
            _context = context;
            _authService = authService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> TestDatabase()
        {
            var result = new
            {
                DatabaseConnected = false,
                RoleCount = 0,
                UserCount = 0,
                Roles = new List<object>(),
                Users = new List<object>(),
                Error = ""
            };

            try
            {
                _logger.LogInformation("Testing database connection...");

                // Test basic connection
                var canConnect = await _context.Database.CanConnectAsync();
                result = result with { DatabaseConnected = canConnect };

                if (!canConnect)
                {
                    result = result with { Error = "Cannot connect to database" };
                    return Json(result);
                }

                // Test roles
                var roles = await _context.Roles.ToListAsync();
                result = result with 
                { 
                    RoleCount = roles.Count,
                    Roles = roles.Select(r => (object)new { r.RoleId, r.RoleName, r.Permissions }).ToList()
                };

                // Test users
                var users = await _context.Users.Include(u => u.Role).Take(5).ToListAsync();
                result = result with 
                { 
                    UserCount = await _context.Users.CountAsync(),
                    Users = users.Select(u => (object)new { u.UserId, u.Email, u.IsActive, RoleName = u.Role?.RoleName }).ToList()
                };

                _logger.LogInformation("Database test completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                result = result with { Error = ex.Message };
            }

            return Json(result);
        }

        public async Task<IActionResult> TestAuth()
        {
            var result = new
            {
                RolesFromService = new List<object>(),
                ServiceError = "",
                InitializationResult = false
            };

            try
            {
                _logger.LogInformation("Testing auth services...");

                // Test role initialization
                var initResult = await _authorizationService.InitializeDefaultRolesAsync();
                result = result with { InitializationResult = initResult };

                // Test getting roles
                var roles = await _authService.GetRolesAsync();
                result = result with 
                { 
                    RolesFromService = roles.Select(r => (object)new { r.RoleId, r.RoleName, r.Permissions }).ToList()
                };

                _logger.LogInformation("Auth service test completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auth service test failed");
                result = result with { ServiceError = ex.Message };
            }

            return Json(result);
        }

        public async Task<IActionResult> TestPasswordHashing()
        {
            var testPassword = "Test123";
            var result = new
            {
                OriginalPassword = testPassword,
                HashedPassword = "",
                VerificationResult = false,
                Error = ""
            };

            try
            {
                _logger.LogInformation("Testing password hashing...");

                var hashedPassword = _authService.HashPassword(testPassword);
                var verificationResult = _authService.VerifyPassword(testPassword, hashedPassword);

                result = result with 
                { 
                    HashedPassword = hashedPassword,
                    VerificationResult = verificationResult
                };

                _logger.LogInformation("Password hashing test completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password hashing test failed");
                result = result with { Error = ex.Message };
            }

            return Json(result);
        }

        public async Task<IActionResult> CreateTestUser()
        {
            var result = new
            {
                Success = false,
                Message = "",
                CreatedUser = new object()
            };

            try
            {
                _logger.LogInformation("Creating test user...");

                // Check if test user already exists
                var existingUser = await _authService.GetUserByEmailAsync("test@example.com");
                if (existingUser != null)
                {
                    result = result with
                    {
                        Success = true,
                        Message = "Test user already exists",
                        CreatedUser = new { existingUser.UserId, existingUser.Email, RoleName = existingUser.Role?.RoleName }
                    };
                    return Json(result);
                }

                // Get Candidate role
                var roles = await _authService.GetRolesAsync();
                var candidateRole = roles.FirstOrDefault(r => r.RoleName == "Candidate");

                if (candidateRole == null)
                {
                    result = result with { Message = "Candidate role not found" };
                    return Json(result);
                }

                // Create test user
                var registerDto = new DKyThucTap.Models.DTOs.RegisterDto
                {
                    Email = "test@example.com",
                    Password = "Test123",
                    ConfirmPassword = "Test123",
                    FirstName = "Test",
                    LastName = "User",
                    Phone = "0123456789",
                    RoleId = candidateRole.RoleId
                };

                var registerResult = await _authService.RegisterAsync(registerDto);

                if (registerResult.Success && registerResult.User != null)
                {
                    result = result with
                    {
                        Success = true,
                        Message = "Test user created successfully",
                        CreatedUser = new
                        {
                            registerResult.User.UserId,
                            registerResult.User.Email,
                            RoleName = registerResult.User.Role?.RoleName,
                            FirstName = registerResult.User.UserProfile?.FirstName,
                            LastName = registerResult.User.UserProfile?.LastName
                        }
                    };
                }
                else
                {
                    result = result with { Message = registerResult.Message };
                }

                _logger.LogInformation("Test user creation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test user creation failed");
                result = result with { Message = ex.Message };
            }

            return Json(result);
        }

        public async Task<IActionResult> TestRegistration()
        {
            var result = new
            {
                Success = false,
                Message = "",
                Details = new object()
            };

            try
            {
                _logger.LogInformation("Testing registration process...");

                // Test with a unique email each time
                var testEmail = $"test{DateTime.Now.Ticks}@example.com";

                // Get Candidate role
                var roles = await _authService.GetRolesAsync();
                var candidateRole = roles.FirstOrDefault(r => r.RoleName == "Candidate");

                if (candidateRole == null)
                {
                    result = result with { Message = "Candidate role not found" };
                    return Json(result);
                }

                // Create test registration
                var registerDto = new DKyThucTap.Models.DTOs.RegisterDto
                {
                    Email = testEmail,
                    Password = "Test123",
                    ConfirmPassword = "Test123",
                    FirstName = "Test",
                    LastName = "User",
                    Phone = "0123456789",
                    RoleId = candidateRole.RoleId
                };

                _logger.LogInformation("Attempting registration with email: {Email}", testEmail);

                var registerResult = await _authService.RegisterAsync(registerDto);

                result = result with
                {
                    Success = registerResult.Success,
                    Message = registerResult.Message,
                    Details = new
                    {
                        Email = testEmail,
                        RoleId = candidateRole.RoleId,
                        RoleName = candidateRole.RoleName,
                        User = registerResult.User != null ? new
                        {
                            registerResult.User.UserId,
                            registerResult.User.Email,
                            registerResult.User.IsActive,
                            RoleName = registerResult.User.Role?.RoleName,
                            FirstName = registerResult.User.UserProfile?.FirstName,
                            LastName = registerResult.User.UserProfile?.LastName
                        } : null
                    }
                };

                _logger.LogInformation("Registration test completed with result: {Success}", registerResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration test failed");
                result = result with { Message = ex.Message };
            }

            return Json(result);
        }
    }
}
