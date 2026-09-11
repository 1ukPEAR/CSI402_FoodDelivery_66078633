using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.FoodViewModels;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers
{
    public class ChatController : Controller
    {
        private readonly FooddeliverydbContext _db;

        public ChatController(FooddeliverydbContext db)
        {
            _db = db;
        }

        // GET: /Chat/OrderChat?orderId=...
        [HttpGet]
        public IActionResult OrderChat(string orderId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToAction("Login", "Login");

            if (string.IsNullOrWhiteSpace(orderId))
            {
                TempData["Error"] = "ไม่พบรหัสออเดอร์";
                return RedirectToAction("OrderStatus", "Order");
            }

            var order = _db.FoodOrders
                .Include(o => o.User)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบข้อมูลออเดอร์";
                return RedirectToAction("OrderStatus", "Order");
            }

            bool isCustomerOwner = order.UserId == userId;

            var room = _db.FoodOrderChatRooms
                .Include(r => r.FoodOrderChatMessages)
                    .ThenInclude(m => m.SenderUser)
                .FirstOrDefault(r => r.OrderId == orderId);

            if (room == null)
            {
                room = new FoodOrderChatRoom
                {
                    ChatRoomId = GenerateChatRoomId(),
                    OrderId = orderId,
                    CreatedAt = DateTime.Now
                };

                _db.FoodOrderChatRooms.Add(room);
                _db.SaveChanges();

                room = _db.FoodOrderChatRooms
                    .Include(r => r.FoodOrderChatMessages)
                        .ThenInclude(m => m.SenderUser)
                    .FirstOrDefault(r => r.ChatRoomId == room.ChatRoomId);
            }

            if (room == null)
            {
                TempData["Error"] = "ไม่สามารถสร้างห้องแชทได้";
                return RedirectToAction("OrderStatus", "Order");
            }

            bool canSendMessage = IsOrderChatOpen(order.OrderStatus);

            // mark ข้อความของอีกฝ่ายว่าอ่านแล้ว
            MarkMessagesAsRead(room.ChatRoomId, userId);

            var messages = _db.FoodOrderChatMessages
                .Include(m => m.SenderUser)
                .Where(m => m.ChatRoomId == room.ChatRoomId)
                .OrderBy(m => m.SentAt)
                .ToList();

            var vm = new ChatViewModel
            {
                OrderId = order.OrderId,
                ChatRoomId = room.ChatRoomId,
                OrderStatus = order.OrderStatus,
                CanSendMessage = canSendMessage,
                IsCustomerOwner = isCustomerOwner,
                CustomerUserId = order.UserId,
                CustomerName = order.User?.UserName,
                Messages = messages.Select(m => new ChatMessageItemViewModel
                {
                    MessageId = m.MessageId,
                    SenderUserId = m.SenderUserId,
                    SenderName = m.SenderUser?.UserName ?? m.SenderUserId,
                    MessageText = m.MessageText ?? string.Empty,
                    SentAt = m.SentAt,
                    IsMine = m.SenderUserId == userId
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Chat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage(string orderId, string chatRoomId, string? messageText)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToAction("Login", "Login");

            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(chatRoomId))
            {
                TempData["Error"] = "ข้อมูลห้องแชทไม่ถูกต้อง";
                return RedirectToAction("OrderStatus", "Order");
            }

            var order = _db.FoodOrders
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderStatus", "Order");
            }

            var room = _db.FoodOrderChatRooms
                .FirstOrDefault(r => r.ChatRoomId == chatRoomId && r.OrderId == orderId);

            if (room == null)
            {
                TempData["Error"] = "ไม่พบห้องแชท";
                return RedirectToAction("OrderChat", new { orderId });
            }

            if (!IsOrderChatOpen(order.OrderStatus))
            {
                TempData["Error"] = "ออเดอร์นี้ปิดการส่งข้อความแล้ว";
                return RedirectToAction("OrderChat", new { orderId });
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                TempData["Error"] = "กรุณาพิมพ์ข้อความก่อนส่ง";
                return RedirectToAction("OrderChat", new { orderId });
            }

            var cleanMessage = messageText.Trim();
            if (cleanMessage.Length > 1000)
            {
                TempData["Error"] = "ข้อความยาวเกินไป (สูงสุด 1000 ตัวอักษร)";
                return RedirectToAction("OrderChat", new { orderId });
            }

            var newMessage = new FoodOrderChatMessage
            {
                MessageId = GenerateMessageId(),
                ChatRoomId = room.ChatRoomId,
                SenderUserId = userId,
                MessageText = cleanMessage,
                SentAt = DateTime.Now,
                IsRead = 0
            };

            _db.FoodOrderChatMessages.Add(newMessage);
            _db.SaveChanges();

            return RedirectToAction("OrderChat", new { orderId });
        }


        // GET: /Chat/GetOrderMessages?orderId=...
        [HttpGet]
        public IActionResult GetOrderMessages(string orderId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    message = "กรุณาเข้าสู่ระบบใหม่"
                });
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบรหัสออเดอร์"
                });
            }

            var order = _db.FoodOrders
                .Include(o => o.User)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบข้อมูลออเดอร์"
                });
            }

            bool isCustomerOwner = order.UserId == userId;

            var room = EnsureChatRoom(orderId);

            if (room == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่สามารถเปิดห้องแชทได้"
                });
            }

            // mark ข้อความที่อีกฝ่ายส่งมาเป็นอ่านแล้ว
            MarkMessagesAsRead(room.ChatRoomId, userId);

            var messages = _db.FoodOrderChatMessages
                .Include(m => m.SenderUser)
                .Where(m => m.ChatRoomId == room.ChatRoomId)
                .OrderBy(m => m.SentAt)
                .ToList();

            var result = messages.Select(m => new
            {
                messageId = m.MessageId,
                senderUserId = m.SenderUserId,
                senderName = m.SenderUser?.UserName ?? m.SenderUserId,
                messageText = m.MessageText ?? "",
                sentAt = m.SentAt.ToString("dd/MM/yyyy HH:mm"),
                sentAtRaw = m.SentAt,
                isMine = m.SenderUserId == userId,
                isRead = (m.IsRead ?? 0) == 1
            }).ToList();

            var lastMessage = messages.LastOrDefault();

            return Json(new
            {
                success = true,
                orderId = order.OrderId,
                chatRoomId = room.ChatRoomId,
                orderStatus = order.OrderStatus,
                canSendMessage = IsOrderChatOpen(order.OrderStatus),
                isCustomerOwner = isCustomerOwner,
                customerUserId = order.UserId,
                customerName = order.User?.UserName,
                lastMessageTicks = lastMessage != null ? lastMessage.SentAt.Ticks : 0L,
                messages = result
            });
        }

        // =========================
        // POST: /Chat/SendMessageAjax
        // ส่งข้อความแบบ AJAX สำหรับ inline chat
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessageAjax(string orderId, string? messageText)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    message = "กรุณาเข้าสู่ระบบใหม่"
                });
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบรหัสออเดอร์"
                });
            }

            var order = _db.FoodOrders
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบออเดอร์"
                });
            }

            // ลูกค้าเปิดได้เฉพาะออเดอร์ตัวเอง
            // พนักงาน/ร้าน เปิดได้ทุกออเดอร์
            if (order.UserId != userId)
            {
                var me = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);
                var roleName = me?.RoleId?.Trim().ToUpper();

                bool isStaffSide =
                    roleName == "R001" ||   // owner
                    roleName == "R002" ||   // employee
                    roleName == "OWNER" ||
                    roleName == "EMPLOYEE";

                if (!isStaffSide)
                {
                    return Json(new
                    {
                        success = false,
                        message = "คุณไม่มีสิทธิ์ใช้งานแชทของออเดอร์นี้"
                    });
                }
            }

            if (!IsOrderChatOpen(order.OrderStatus))
            {
                return Json(new
                {
                    success = false,
                    message = "ออเดอร์นี้ปิดการส่งข้อความแล้ว"
                });
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return Json(new
                {
                    success = false,
                    message = "กรุณาพิมพ์ข้อความก่อนส่ง"
                });
            }

            var cleanMessage = messageText.Trim();
            if (cleanMessage.Length > 1000)
            {
                return Json(new
                {
                    success = false,
                    message = "ข้อความยาวเกินไป (สูงสุด 1000 ตัวอักษร)"
                });
            }

            var room = EnsureChatRoom(orderId);

            if (room == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่สามารถสร้างห้องแชทได้"
                });
            }

            var newMessage = new FoodOrderChatMessage
            {
                MessageId = GenerateMessageId(),
                ChatRoomId = room.ChatRoomId,
                SenderUserId = userId,
                MessageText = cleanMessage,
                SentAt = DateTime.Now,
                IsRead = 0
            };

            _db.FoodOrderChatMessages.Add(newMessage);
            _db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "ส่งข้อความสำเร็จ",
                lastMessageTicks = newMessage.SentAt.Ticks,
                data = new
                {
                    messageId = newMessage.MessageId,
                    senderUserId = newMessage.SenderUserId,
                    messageText = newMessage.MessageText ?? "",
                    sentAt = newMessage.SentAt.ToString("dd/MM/yyyy HH:mm"),
                    sentAtRaw = newMessage.SentAt,
                    isMine = true
                }
            });
        }

        // =========================
        // POST: /Chat/MarkOrderChatAsRead
        // ใช้สำหรับหน้า OrderList / OrderStatus
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkOrderChatAsRead(string orderId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    message = "กรุณาเข้าสู่ระบบใหม่"
                });
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบรหัสออเดอร์"
                });
            }

            var order = _db.FoodOrders
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบออเดอร์"
                });
            }

            // ลูกค้าเปิดได้เฉพาะออเดอร์ตัวเอง
            // พนักงาน/ร้าน เปิดได้ทุกออเดอร์
            if (order.UserId != userId)
            {
                var me = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);
                var roleName = me?.RoleId?.Trim().ToUpper();

                bool isStaffSide =
                    roleName == "R001" ||
                    roleName == "R002" ||
                    roleName == "OWNER" ||
                    roleName == "EMPLOYEE";

                if (!isStaffSide)
                {
                    return Json(new
                    {
                        success = false,
                        message = "คุณไม่มีสิทธิ์ใช้งานแชทของออเดอร์นี้"
                    });
                }
            }

            var room = EnsureChatRoom(orderId);
            if (room == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่สามารถเปิดห้องแชทได้"
                });
            }

            var markedCount = MarkMessagesAsRead(room.ChatRoomId, userId);

            var lastMessage = _db.FoodOrderChatMessages
                .Where(m => m.ChatRoomId == room.ChatRoomId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault();

            var unreadCount = _db.FoodOrderChatMessages.Count(m =>
                m.ChatRoomId == room.ChatRoomId &&
                m.SenderUserId != userId &&
                (m.IsRead == null || m.IsRead == 0)
            );

            return Json(new
            {
                success = true,
                message = "อัปเดตสถานะการอ่านแล้ว",
                markedCount = markedCount,
                unreadCount = unreadCount,
                lastMessageTicks = lastMessage != null ? lastMessage.SentAt.Ticks : 0L,
                canSendMessage = IsOrderChatOpen(order.OrderStatus)
            });
        }

        // =========================
        // GET: /Chat/GetOrderChatMeta?orderId=...&lastSeenTicks=...
        // ใช้เช็ก badge "ข้อความใหม่"
        // =========================
        [HttpGet]
        public IActionResult GetOrderChatMeta(string orderId, long lastSeenTicks = 0)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new
                {
                    success = false,
                    message = "กรุณาเข้าสู่ระบบใหม่"
                });
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบรหัสออเดอร์"
                });
            }

            var order = _db.FoodOrders
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่พบออเดอร์"
                });
            }

            // ลูกค้าเปิดได้เฉพาะออเดอร์ตัวเอง
            // พนักงาน/ร้าน เปิดได้ทุกออเดอร์
            if (order.UserId != userId)
            {
                var me = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);
                var roleName = me?.RoleId?.Trim().ToUpper();

                bool isStaffSide =
                    roleName == "R001" ||
                    roleName == "R002" ||
                    roleName == "OWNER" ||
                    roleName == "EMPLOYEE";

                if (!isStaffSide)
                {
                    return Json(new
                    {
                        success = false,
                        message = "คุณไม่มีสิทธิ์ใช้งานแชทของออเดอร์นี้"
                    });
                }
            }

            var room = EnsureChatRoom(orderId);

            if (room == null)
            {
                return Json(new
                {
                    success = false,
                    message = "ไม่สามารถเปิดห้องแชทได้"
                });
            }

            var messages = _db.FoodOrderChatMessages
                .Where(m => m.ChatRoomId == room.ChatRoomId)
                .OrderBy(m => m.SentAt)
                .ToList();

            if (!messages.Any())
            {
                return Json(new
                {
                    success = true,
                    hasMessages = false,
                    hasUnread = false,
                    unreadCount = 0,
                    lastMessageTicks = 0L,
                    canSendMessage = IsOrderChatOpen(order.OrderStatus)
                });
            }

            var lastMessage = messages.Last();
            var latestTicks = lastMessage.SentAt.Ticks;
            var baselineTicks = lastSeenTicks;

            var unreadCount = messages.Count(m =>
                m.SenderUserId != userId &&
                (m.IsRead == null || m.IsRead == 0) &&
                m.SentAt.Ticks > baselineTicks
            );

            var hasUnread = unreadCount > 0;

            return Json(new
            {
                success = true,
                hasMessages = true,
                hasUnread = hasUnread,
                unreadCount = unreadCount,
                lastMessageTicks = latestTicks,
                canSendMessage = IsOrderChatOpen(order.OrderStatus)
            });
        }

        // =========================
        // Helpers
        // =========================
        private bool IsOrderChatOpen(ulong? orderStatus)
        {
            if (orderStatus == null) return false;
            return orderStatus >= 1 && orderStatus <= 6;
        }

        private FoodOrderChatRoom? EnsureChatRoom(string orderId)
        {
            var room = _db.FoodOrderChatRooms
                .FirstOrDefault(r => r.OrderId == orderId);

            if (room != null)
                return room;

            room = new FoodOrderChatRoom
            {
                ChatRoomId = GenerateChatRoomId(),
                OrderId = orderId,
                CreatedAt = DateTime.Now
            };

            _db.FoodOrderChatRooms.Add(room);
            _db.SaveChanges();

            return _db.FoodOrderChatRooms
                .FirstOrDefault(r => r.ChatRoomId == room.ChatRoomId);
        }

        private int MarkMessagesAsRead(string chatRoomId, string currentUserId)
        {
            var unreadMessages = _db.FoodOrderChatMessages
                .Where(m =>
                    m.ChatRoomId == chatRoomId &&
                    m.SenderUserId != currentUserId &&
                    (m.IsRead == null || m.IsRead == 0))
                .ToList();

            if (!unreadMessages.Any())
                return 0;

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = 1;
            }

            _db.SaveChanges();
            return unreadMessages.Count;
        }

        private string? GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
                return userId;

            return HttpContext.Session.GetString("UserId")
                ?? HttpContext.Session.GetString("Userid")
                ?? HttpContext.Session.GetString("userId")
                ?? HttpContext.Session.GetString("UserID");
        }

        private string GenerateChatRoomId()
        {
            string newId;
            do
            {
                var count = _db.FoodOrderChatRooms.Count() + 1;
                newId = "CR" + count.ToString("D8");
            }
            while (_db.FoodOrderChatRooms.Any(x => x.ChatRoomId == newId));

            return newId;
        }

        private string GenerateMessageId()
        {
            string newId;
            do
            {
                var count = _db.FoodOrderChatMessages.Count() + 1;
                newId = "CM" + count.ToString("D8");
            }
            while (_db.FoodOrderChatMessages.Any(x => x.MessageId == newId));

            return newId;
        }
    }
}