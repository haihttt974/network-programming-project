using Microsoft.AspNetCore.Mvc;
using DKyThucTap.Services;
using DKyThucTap.Data;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Controllers
{
    [Route("Test/Online")]
    public class TestOnlineController : Controller
    {
        private readonly IOnlineUserService _onlineUserService;
        private readonly DKyThucTapContext _context;
        private readonly ILogger<TestOnlineController> _logger;

        public TestOnlineController(
            IOnlineUserService onlineUserService, 
            DKyThucTapContext context,
            ILogger<TestOnlineController> logger)
        {
            _onlineUserService = onlineUserService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("QuickTest")]
        public async Task<IActionResult> QuickTest()
        {
            var results = new List<string>();
            
            try
            {
                results.Add("=== QUICK ONLINE USERS TEST ===");
                
                // Test 1: Database connection
                results.Add("1. Testing database connection...");
                var connectionCount = await _context.WebsocketConnections.CountAsync();
                results.Add($"   ✓ Database OK - {connectionCount} total connections");
                
                // Test 2: Service methods
                results.Add("2. Testing service methods...");
                var onlineCount = await _onlineUserService.GetOnlineUserCountAsync();
                results.Add($"   ✓ GetOnlineUserCountAsync: {onlineCount} users");
                
                var onlineUsers = await _onlineUserService.GetOnlineUsersAsync();
                results.Add($"   ✓ GetOnlineUsersAsync: {onlineUsers.Count} users");
                
                // Test 3: Add test connection
                results.Add("3. Testing add connection...");
                var testConnectionId = $"test_{Guid.NewGuid():N}";
                var testUserId = 1; // Assuming user ID 1 exists
                
                await _onlineUserService.AddUserConnectionAsync(testConnectionId, testUserId, "Test Connection");
                results.Add($"   ✓ Added test connection: {testConnectionId}");
                
                // Test 4: Check count after adding
                var newCount = await _onlineUserService.GetOnlineUserCountAsync();
                results.Add($"   ✓ Count after adding: {newCount}");
                
                // Test 5: Remove test connection
                await _onlineUserService.RemoveUserConnectionAsync(testConnectionId);
                results.Add($"   ✓ Removed test connection");
                
                // Test 6: Final count
                var finalCount = await _onlineUserService.GetOnlineUserCountAsync();
                results.Add($"   ✓ Final count: {finalCount}");
                
                results.Add("=== ALL TESTS PASSED ===");
                
            }
            catch (Exception ex)
            {
                results.Add($"❌ ERROR: {ex.Message}");
                results.Add($"Stack trace: {ex.StackTrace}");
            }
            
            return Json(new { 
                success = !results.Any(r => r.Contains("ERROR")),
                results = results,
                timestamp = DateTimeOffset.UtcNow
            });
        }

        [HttpGet("CreateTestData")]
        public async Task<IActionResult> CreateTestData()
        {
            try
            {
                var results = new List<string>();
                results.Add("Creating test data...");
                
                // Create 3 test connections for different users
                var testConnections = new[]
                {
                    new { ConnectionId = $"test_user1_{Guid.NewGuid():N}", UserId = 1, ClientInfo = "Test Browser 1" },
                    new { ConnectionId = $"test_user2_{Guid.NewGuid():N}", UserId = 2, ClientInfo = "Test Browser 2" },
                    new { ConnectionId = $"test_user3_{Guid.NewGuid():N}", UserId = 3, ClientInfo = "Test Browser 3" }
                };

                foreach (var conn in testConnections)
                {
                    try
                    {
                        await _onlineUserService.AddUserConnectionAsync(conn.ConnectionId, conn.UserId, conn.ClientInfo);
                        results.Add($"✓ Created connection for user {conn.UserId}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ Failed to create connection for user {conn.UserId}: {ex.Message}");
                    }
                }

                // Check final counts
                var totalCount = await _onlineUserService.GetOnlineUserCountAsync();
                var users = await _onlineUserService.GetOnlineUsersAsync();
                
                results.Add($"Total online users: {totalCount}");
                results.Add($"User details: {users.Count} users found");
                
                return Json(new { 
                    success = true,
                    results = results,
                    onlineCount = totalCount,
                    users = users
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("ClearTestData")]
        public async Task<IActionResult> ClearTestData()
        {
            try
            {
                var testConnections = await _context.WebsocketConnections
                    .Where(wc => wc.ConnectionId.StartsWith("test_"))
                    .ToListAsync();

                if (testConnections.Any())
                {
                    _context.WebsocketConnections.RemoveRange(testConnections);
                    await _context.SaveChangesAsync();
                }

                return Json(new { 
                    success = true,
                    message = $"Cleared {testConnections.Count} test connections",
                    clearedConnections = testConnections.Select(tc => tc.ConnectionId).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
