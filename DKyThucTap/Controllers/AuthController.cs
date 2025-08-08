using DKyThucTap.Models.DTOs;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly Services.IAuthorizationService _authorizationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService, 
            Services.IAuthorizationService authorizationService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Nếu là Admin, chuyển đến trang Admin Dashboard
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("AdminDashboard", "AdminHome", new { area = "Admin" });
                }

                // Nếu là vai trò khác, chuyển đến trang Dashboard của người dùng
                return RedirectToAction("Dashboard", "Account");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt with invalid model state for email: {Email}", model.Email);
                return View(model);
            }

            try
            {
                _logger.LogInformation("Processing login attempt for email: {Email}", model.Email);

                var result = await _authService.LoginAsync(model);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for email: {Email}, Message: {Message}", model.Email, result.Message);
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                if (result.User == null)
                {
                    _logger.LogError("Login service returned success but user is null for email: {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình đăng nhập");
                    return View(model);
                }

                _logger.LogInformation("Creating claims for user: {UserId}, Role: {RoleName}",
                    result.User.UserId, result.User.Role?.RoleName);

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                    new Claim(ClaimTypes.Email, result.User.Email),
                    new Claim(ClaimTypes.Role, result.User.Role?.RoleName ?? "Unknown"),
                    new Claim("RoleId", result.User.RoleId.ToString()),
                    new Claim("FullName", $"{result.User.UserProfile?.FirstName} {result.User.UserProfile?.LastName}".Trim())
                };

                // Add permissions as claims
                try
                {
                    var permissions = await _authorizationService.GetUserPermissionsAsync(result.User);
                    _logger.LogInformation("Found {PermissionCount} permissions for user: {UserId}",
                        permissions.Count, result.User.UserId);

                    foreach (var permission in permissions.Where(p => p.Value))
                    {
                        claims.Add(new Claim("Permission", permission.Key));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting permissions for user: {UserId}", result.User.UserId);
                    // Continue without permissions rather than failing login
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                _logger.LogInformation("User {Email} (ID: {UserId}) logged in successfully with {ClaimCount} claims",
                    model.Email, result.User.UserId, claims.Count);

                // ...
                // Kiểm tra vai trò và chuyển hướng tương ứng
                if (result.User.Role?.RoleName == "Admin")
                {
                    _logger.LogInformation("Admin user logged in. Redirecting to Admin dashboard.");
                    // Chuyển hướng đến trang Index của AdminHomeController
                    return RedirectToAction("AdminDashboard", "AdminHome", new { area = "Admin" });
                }

                // Xử lý chuyển hướng cho các vai trò khác hoặc khi có returnUrl
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("Redirecting to return URL: {ReturnUrl}", returnUrl);
                    return Redirect(returnUrl);
                }

                _logger.LogInformation("Redirecting to user dashboard.");
                return RedirectToAction("Dashboard", "Account");
                //...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Account");
            }

            try
            {
                var roles = await _authService.GetRolesAsync();
                ViewBag.Roles = roles.Where(r => r.RoleName != "Admin").ToList(); // Don't allow admin registration

                //_logger.LogInformation("Register GET: Found {RoleCount} roles for registration", ViewBag.Roles.Count);
                _logger.LogInformation("Register GET: Found {RoleCount} roles for registration", ((List<DKyThucTap.Models.Role>)ViewBag.Roles).Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles for registration");
                ViewBag.Roles = new List<DKyThucTap.Models.Role>();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _authService.GetRolesAsync();
                ViewBag.Roles = roles.Where(r => r.RoleName != "Admin").ToList();
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);
            
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var roles = await _authService.GetRolesAsync();
                ViewBag.Roles = roles.Where(r => r.RoleName != "Admin").ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(userId, out int userIdInt))
            {
                await _authService.LogoutAsync(userIdInt);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            _logger.LogInformation("User {UserId} logged out", userId);
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
