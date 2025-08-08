using Microsoft.AspNetCore.SignalR;
using DKyThucTap.Data;
using DKyThucTap.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DKyThucTap.Hubs
{
    public class ChatHub : Hub
    {
        private readonly DKyThucTapContext _context;

        public ChatHub(DKyThucTapContext context)
        {
            _context = context;
        }

        // Gửi tin nhắn theo conversationId
        public async Task SendMessage(int conversationId, string message)
        {
            var senderUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderUserId)) return;

            int senderId = int.Parse(senderUserId);

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId &&
                    (c.Participant1UserId == senderId || c.Participant2UserId == senderId));

            if (conversation == null)
                return;

            var receiverId = conversation.Participant1UserId == senderId
                ? conversation.Participant2UserId
                : conversation.Participant1UserId;

            var newMessage = new Message
            {
                ConversationId = conversationId,
                SenderUserId = senderId,
                MessageText = message,
                SentAt = DateTimeOffset.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(newMessage);
            conversation.LastMessageAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var sentTime = newMessage.SentAt?.ToString("HH:mm") ?? DateTimeOffset.UtcNow.ToString("HH:mm");

            await Clients.User(receiverId.ToString())
                .SendAsync("ReceiveMessage", senderId, message, sentTime);

            await Clients.User(senderId.ToString())
                .SendAsync("ReceiveMessage", senderId, message, sentTime);
        }
    }
}