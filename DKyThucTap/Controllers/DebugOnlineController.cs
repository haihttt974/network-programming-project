using Microsoft.AspNetCore.Mvc;
using DKyThucTap.Services;
using DKyThucTap.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Route("Debug/Online")]
    public class DebugOnlineController : Controller
    {
        private readonly IOnlineUserService _onlineUserService;
        private readonly DKyThucTapContext _context;
        private readonly ILogger<DebugOnlineController> _logger;

        public DebugOnlineController(
            IOnlineUserService onlineUserService, 
            DKyThucTapContext context,
            ILogger<DebugOnlineController> logger)
        {
            _onlineUserService = onlineUserService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("TestDatabase")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var connections = await _context.WebsocketConnections
                    .Include(wc => wc.User)
                        .ThenInclude(u => u.UserProfile)
                    .OrderByDescending(wc => wc.LastActivity)
                    .ToListAsync();

                var result = new
                {
                    TotalConnections = connections.Count,
                    Connections = connections.Select(c => new
                    {
                        c.ConnectionId,
                        c.UserId,
                        UserEmail = c.User?.Email,
                        UserName = c.User?.UserProfile != null 
                            ? $"{c.User.UserProfile.FirstName} {c.User.UserProfile.LastName}".Trim()
                            : c.User?.Email,
                        c.ConnectedAt,
                        c.LastActivity,
                        c.ClientInfo,
                        MinutesAgo = c.LastActivity.HasValue 
                            ? (double?)Math.Floor((DateTimeOffset.UtcNow - c.LastActivity.Value).TotalMinutes)
                            : null
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("TestService")]
        public async Task<IActionResult> TestService()
        {
            try
            {
                var count = await _onlineUserService.GetOnlineUserCountAsync();
                var users = await _onlineUserService.GetOnlineUsersAsync();

                var result = new
                {
                    OnlineCount = count,
                    OnlineUsers = users,
                    ServiceType = _onlineUserService.GetType().Name
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing service");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("TestConnect")]
        public async Task<IActionResult> TestConnect()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { error = "User not authenticated" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Json(new { error = "Invalid user ID" });
                }

                var connectionId = $"debug_{userId}_{Guid.NewGuid():N}";
                var clientInfo = $"Debug Test - {Request.Headers.UserAgent}";

                await _onlineUserService.AddUserConnectionAsync(connectionId, userId, clientInfo);

                return Json(new { 
                    success = true, 
                    connectionId = connectionId,
                    userId = userId,
                    message = "Connection added successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connect");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("TestDisconnect")]
        public async Task<IActionResult> TestDisconnect([FromBody] string connectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId))
                {
                    return Json(new { error = "Connection ID is required" });
                }

                await _onlineUserService.RemoveUserConnectionAsync(connectionId);

                return Json(new { 
                    success = true, 
                    message = "Connection removed successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing disconnect");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("TestCleanup")]
        public async Task<IActionResult> TestCleanup()
        {
            try
            {
                await _onlineUserService.CleanupInactiveConnectionsAsync();

                return Json(new { 
                    success = true, 
                    message = "Cleanup completed successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing cleanup");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("TestAPI")]
        public async Task<IActionResult> TestAPI()
        {
            try
            {
                using var httpClient = new HttpClient();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var result = new
                {
                    CountEndpoint = new { StatusCode = 0, Content = "", IsSuccess = false },
                    ListEndpoint = new { StatusCode = 0, Content = "", IsSuccess = false }
                };

                try
                {
                    // Test count endpoint
                    var countResponse = await httpClient.GetAsync($"{baseUrl}/api/OnlineUsers/count");
                    var countContent = await countResponse.Content.ReadAsStringAsync();

                    result = new
                    {
                        CountEndpoint = new
                        {
                            StatusCode = (int)countResponse.StatusCode,
                            Content = countContent,
                            IsSuccess = countResponse.IsSuccessStatusCode
                        },
                        ListEndpoint = result.ListEndpoint
                    };
                }
                catch (Exception ex)
                {
                    result = new
                    {
                        CountEndpoint = new
                        {
                            StatusCode = 500,
                            Content = $"Error: {ex.Message}",
                            IsSuccess = false
                        },
                        ListEndpoint = result.ListEndpoint
                    };
                }

                try
                {
                    // Test list endpoint
                    var listResponse = await httpClient.GetAsync($"{baseUrl}/api/OnlineUsers/list");
                    var listContent = await listResponse.Content.ReadAsStringAsync();

                    result = new
                    {
                        CountEndpoint = result.CountEndpoint,
                        ListEndpoint = new
                        {
                            StatusCode = (int)listResponse.StatusCode,
                            Content = listContent,
                            IsSuccess = listResponse.IsSuccessStatusCode
                        }
                    };
                }
                catch (Exception ex)
                {
                    result = new
                    {
                        CountEndpoint = result.CountEndpoint,
                        ListEndpoint = new
                        {
                            StatusCode = 500,
                            Content = $"Error: {ex.Message}",
                            IsSuccess = false
                        }
                    };
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
