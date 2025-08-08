using Microsoft.AspNetCore.Mvc;
using DKyThucTap.Services;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnlineUsersController : ControllerBase
    {
        private readonly IOnlineUserService _onlineUserService;
        private readonly ILogger<OnlineUsersController> _logger;

        public OnlineUsersController(IOnlineUserService onlineUserService, ILogger<OnlineUsersController> logger)
        {
            _onlineUserService = onlineUserService;
            _logger = logger;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetOnlineUserCount()
        {
            try
            {
                var count = await _onlineUserService.GetOnlineUserCountAsync();
                return Ok(new { count = count, timestamp = DateTimeOffset.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online user count");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            try
            {
                var users = await _onlineUserService.GetOnlineUsersAsync();
                return Ok(new { users = users, count = users.Count, timestamp = DateTimeOffset.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectRequest request)
        {
            try
            {
                _logger.LogInformation("Connect request received");

                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Connect request from unauthenticated user");
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User ID claim: {UserIdClaim}", userIdClaim);

                if (!int.TryParse(userIdClaim, out var userId) || userId == 0)
                {
                    _logger.LogWarning("Invalid user ID: {UserIdClaim}", userIdClaim);
                    return Unauthorized(new { error = "Invalid user ID" });
                }

                // Generate a unique connection ID
                var connectionId = $"web_{userId}_{Guid.NewGuid():N}";
                _logger.LogInformation("Generated connection ID: {ConnectionId} for user {UserId}", connectionId, userId);

                await _onlineUserService.AddUserConnectionAsync(connectionId, userId, request?.ClientInfo);

                _logger.LogInformation("Successfully connected user {UserId} with connection {ConnectionId}", userId, connectionId);
                return Ok(new { connectionId = connectionId, message = "Connected successfully", userId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting user");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect([FromBody] DisconnectRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ConnectionId))
                {
                    return BadRequest(new { error = "Connection ID is required" });
                }

                await _onlineUserService.RemoveUserConnectionAsync(request.ConnectionId);

                return Ok(new { message = "Disconnected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ConnectionId))
                {
                    return BadRequest(new { error = "Connection ID is required" });
                }

                await _onlineUserService.UpdateUserActivityAsync(request.ConnectionId);

                return Ok(new { message = "Heartbeat received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("is-online/{userId}")]
        public async Task<IActionResult> IsOnline(int userId)
        {
            var isOnline = await _onlineUserService.IsUserOnlineAsync(userId);
            return Ok(new { userId, isOnline });
        }
    }

    public class ConnectRequest
    {
        public string? ClientInfo { get; set; }
    }

    public class DisconnectRequest
    {
        public string ConnectionId { get; set; } = null!;
    }

    public class HeartbeatRequest
    {
        public string ConnectionId { get; set; } = null!;
    }
}
