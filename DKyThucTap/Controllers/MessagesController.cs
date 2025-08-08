using DKyThucTap.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DKyThucTap.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly DKyThucTapContext _context;

        public MessagesController(DKyThucTapContext context)
        {
            _context = context;
        }

        // ✅ Trang chính tin nhắn
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var conversations = await _context.Conversations
                .Include(c => c.Participant1User).ThenInclude(u => u.UserProfile)
                .Include(c => c.Participant1User).ThenInclude(u => u.Role)
                .Include(c => c.Participant2User).ThenInclude(u => u.UserProfile)
                .Include(c => c.Participant2User).ThenInclude(u => u.Role)
                .Where(c => c.Participant1UserId == userId || c.Participant2UserId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return View(conversations);
        }

        // ✅ API trả về số tin nhắn chưa đọc (cho navbar badge)
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var count = await _context.Messages
                .Where(m => (m.Conversation.Participant1UserId == userId
                          || m.Conversation.Participant2UserId == userId)
                          && m.SenderUserId != userId
                          && m.IsRead == false)
                .CountAsync();

            return Json(new { unread = count });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadPerConversation()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var unreadCounts = await _context.Messages
                .Where(m => (m.Conversation.Participant1UserId == userId
                          || m.Conversation.Participant2UserId == userId)
                          && m.SenderUserId != userId
                          && m.IsRead == false)
                .GroupBy(m => m.ConversationId)
                .Select(g => new
                {
                    ConversationId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Json(unreadCounts);
        }

        // ✅ Load lịch sử tin nhắn trong 1 cuộc hội thoại
        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                    .ThenInclude(m => m.SenderUser)
                    .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId &&
                                          (c.Participant1UserId == userId || c.Participant2UserId == userId));

            if (conversation == null)
                return Unauthorized();

            var messages = conversation.Messages
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.MessageText,
                    m.SenderUserId,
                    Sender = m.SenderUser.UserProfile.FirstName + " " + m.SenderUser.UserProfile.LastName,
                    SentAt = m.SentAt.HasValue ? m.SentAt.Value.UtcDateTime.ToString("o") : "",
                    Avatar = m.SenderUser.UserProfile.ProfilePictureUrl ?? "/images/default-avatar.png"
                })
                .ToList();

            // ✅ Đánh dấu tin nhắn là đã đọc
            var unreadMessages = conversation.Messages
                .Where(m => m.SenderUserId != userId && m.IsRead == false)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                    msg.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(messages);
        }

        // ✅ Trang chat với nhà tuyển dụng (từ Position/Details)
        [HttpGet]
        public async Task<IActionResult> Chat(int positionId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Lấy thông tin công việc
            var position = await _context.Positions
                .Include(p => p.CreatedByNavigation)
                .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(p => p.PositionId == positionId);

            if (position == null)
            {
                TempData["Error"] = "Không tìm thấy công việc.";
                return RedirectToAction("Index", "Positions");
            }

            var employerId = position.CreatedBy;
            if (employerId == null)
            {
                TempData["Error"] = "Không tìm thấy nhà tuyển dụng.";
                return RedirectToAction("Index", "Positions");
            }

            // Kiểm tra xem đã có conversation giữa ứng viên và nhà tuyển dụng chưa
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.Participant1UserId == userId && c.Participant2UserId == employerId) ||
                    (c.Participant1UserId == employerId && c.Participant2UserId == userId));

            if (conversation == null)
            {
                conversation = new Models.Conversation
                {
                    Participant1UserId = userId,
                    Participant2UserId = employerId.Value,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastMessageAt = DateTimeOffset.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            ViewBag.PositionTitle = position.Title;
            ViewBag.ConversationId = conversation.ConversationId;
            ViewBag.EmployerName = position.CreatedByNavigation.UserProfile?.FirstName + " " +
                                   position.CreatedByNavigation.UserProfile?.LastName;

            return View("Chat");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId &&
                                          (c.Participant1UserId == userId || c.Participant2UserId == userId));

            if (conversation == null)
                return Unauthorized();

            var unreadMessages = conversation.Messages
                .Where(m => m.SenderUserId != userId && m.IsRead == false)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, count = unreadMessages.Count });
        }

    }
}