# 📋 **TÀI LIỆU HƯỚNG DẪN HỆ THỐNG THÔNG BÁO**

## **DKyThucTap - Real-time Notification System**

---

## 📖 **MỤC LỤC**

1. [Tổng quan hệ thống](#1-tổng-quan-hệ-thống)
2. [Kiến trúc hệ thống](#2-kiến-trúc-hệ-thống)
3. [Các loại thông báo](#3-các-loại-thông-báo)
4. [Hướng dẫn sử dụng cho Developer](#4-hướng-dẫn-sử-dụng-cho-developer)
5. [API Reference](#5-api-reference)
6. [Frontend Integration](#6-frontend-integration)
7. [Testing & Debugging](#7-testing--debugging)
8. [Best Practices](#8-best-practices)

---

## **1. TỔNG QUAN HỆ THỐNG**

### 🎯 **Mục đích**

Hệ thống thông báo real-time cho phép gửi thông báo tức thì đến người dùng, tương tự như Facebook notifications, với các tính năng:

- ✅ **Real-time delivery** qua SignalR (WebSocket)
- ✅ **Toast notifications** popup tự động
- ✅ **Badge counter** hiển thị số thông báo chưa đọc
- ✅ **Multi-tab synchronization** đồng bộ giữa các tab
- ✅ **Fallback mechanism** tự động chuyển sang polling nếu WebSocket lỗi

### 🚀 **Tính năng chính**

- **Instant notifications**: Thông báo xuất hiện ngay lập tức
- **Multiple delivery methods**: Real-time + Database storage
- **Rich notification types**: 8 loại thông báo khác nhau với icon và màu sắc riêng
- **Flexible targeting**: Gửi cho 1 người, nhóm người, theo role, hoặc tất cả
- **Comprehensive management**: CRUD operations đầy đủ

---

## **2. KIẾN TRÚC HỆ THỐNG**

### 🏗️ **Sơ đồ kiến trúc**

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │    │    Backend       │    │   Database      │
│                 │    │                  │    │                 │
│ • Notification  │◄──►│ • NotificationHub│◄──►│ • Notifications │
│   Manager       │    │ • Services       │    │ • Users         │
│ • SignalR       │    │ • Controllers    │    │ • Roles         │
│ • Toast UI      │    │ • Integration    │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### 🔧 **Các thành phần chính**

#### **Backend Services:**

- **`NotificationService`**: Core service xử lý CRUD operations
- **`NotificationIntegrationService`**: Helper methods cho các business scenarios
- **`NotificationHub`**: SignalR hub xử lý real-time connections
- **`NotificationController`**: API endpoints
- **`NotificationViewController`**: MVC views

#### **Frontend Components:**

- **`NotificationManager`**: JavaScript class quản lý notifications
- **`SignalR Client`**: Real-time connection handling
- **`Toast System`**: Bootstrap toast notifications
- **`Badge System`**: Notification counter trong header

---

## **3. CÁC LOẠI THÔNG BÁO**

### 📊 **Notification Types**

| Type                  | Icon                        | Màu sắc   | Mô tả               | Use Case                      |
| --------------------- | --------------------------- | --------- | ------------------- | ----------------------------- |
| `job_application`     | 💼 `fas fa-briefcase`       | Blue      | Ứng tuyển công việc | User nộp đơn ứng tuyển        |
| `job_status_update`   | ✅ `fas fa-clipboard-check` | Green     | Cập nhật trạng thái | Thay đổi trạng thái ứng tuyển |
| `new_job_posting`     | ➕ `fas fa-plus-circle`     | Info      | Việc làm mới        | Có việc làm phù hợp           |
| `company_invitation`  | 🏢 `fas fa-building`        | Warning   | Lời mời công ty     | Công ty mời ứng viên          |
| `system_announcement` | 📢 `fas fa-bullhorn`        | Danger    | Thông báo hệ thống  | Bảo trì, cập nhật             |
| `profile_update`      | 👤 `fas fa-user-edit`       | Secondary | Cập nhật hồ sơ      | Thay đổi profile              |
| `message_received`    | ✉️ `fas fa-envelope`        | Primary   | Tin nhắn mới        | Nhận tin nhắn                 |
| `account_security`    | 🛡️ `fas fa-shield-alt`      | Danger    | Bảo mật tài khoản   | Cảnh báo bảo mật              |

### 🎨 **Related Entity Types**

```csharp
public static class RelatedEntityTypes
{
    public const string User = "user";
    public const string Position = "position";
    public const string Application = "application";
    public const string Company = "company";
    public const string Message = "message";
    public const string System = "system";
}
```

---

## **4. HƯỚNG DẪN SỬ DỤNG CHO DEVELOPER**

### 🔨 **Setup & Dependency Injection**

#### **1. Đăng ký services trong `Program.cs`:**

```csharp
// Đã được cấu hình sẵn
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationIntegrationService, NotificationIntegrationService>();
builder.Services.AddSignalR();

// Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");
```

#### **2. Inject services trong Controller:**

```csharp
public class YourController : Controller
{
    private readonly INotificationIntegrationService _notificationIntegration;
    private readonly INotificationService _notificationService;

    public YourController(
        INotificationIntegrationService notificationIntegration,
        INotificationService notificationService)
    {
        _notificationIntegration = notificationIntegration;
        _notificationService = notificationService;
    }
}
```

### 📝 **Cách tạo thông báo**

#### **A. Sử dụng NotificationIntegrationService (Khuyến nghị)**

**Thông báo cho 1 người cụ thể:**

```csharp
// Khi user ứng tuyển công việc
await _notificationIntegration.NotifyJobApplicationSubmittedAsync(
    userId: 123,
    jobTitle: "Senior Developer",
    applicationId: 456
);

// Khi thay đổi trạng thái ứng tuyển
await _notificationIntegration.NotifyJobApplicationStatusChangedAsync(
    userId: 123,
    jobTitle: "Frontend Developer",
    newStatus: "Đã được chấp nhận",
    applicationId: 456
);

// Khi có việc làm phù hợp
await _notificationIntegration.NotifyNewJobMatchingCriteriaAsync(
    userId: 123,
    jobTitle: "React Developer",
    positionId: 789,
    matchReason: "Phù hợp với kỹ năng React, JavaScript"
);

// Thông báo bảo trì hệ thống
await _notificationIntegration.NotifySystemMaintenanceAsync(
    userId: 123,
    maintenanceStart: DateTime.Now.AddHours(2),
    maintenanceEnd: DateTime.Now.AddHours(4)
);
```

**Thông báo cho tất cả người dùng:**

```csharp
// Thông báo cập nhật hệ thống
await _notificationIntegration.NotifyAllUsersSystemUpdateAsync(
    updateTitle: "Tính năng mới",
    updateDetails: "Hệ thống đã có tính năng tìm kiếm nâng cao!"
);
```

#### **B. Sử dụng NotificationService trực tiếp**

**Tạo thông báo tùy chỉnh:**

```csharp
await _notificationService.CreateNotificationAsync(new CreateNotificationDto
{
    UserId = 123,
    Title = "Chúc mừng!",
    Message = "Hồ sơ của bạn đã được duyệt thành công",
    NotificationType = NotificationTypes.ProfileUpdate,
    RelatedEntityType = RelatedEntityTypes.User,
    RelatedEntityId = 123
});
```

**Broadcast theo nhóm:**

```csharp
// Gửi cho tất cả users
await _notificationService.BroadcastToAllUsersAsync(
    title: "Thông báo quan trọng",
    message: "Hệ thống sẽ bảo trì vào 2h sáng mai",
    notificationType: NotificationTypes.SystemAnnouncement
);

// Gửi theo role
await _notificationService.BroadcastToUsersByRoleAsync(
    role: "Recruiter",
    title: "Tính năng mới cho Recruiter",
    message: "Bạn có thể sử dụng tính năng tìm kiếm ứng viên nâng cao",
    notificationType: NotificationTypes.SystemAnnouncement
);

// Gửi theo công ty
await _notificationService.BroadcastToCompanyUsersAsync(
    companyId: 456,
    title: "Cập nhật công ty",
    message: "Thông tin công ty đã được cập nhật",
    notificationType: NotificationTypes.CompanyInvitation
);
```

### 🎯 **Ví dụ thực tế**

#### **1. Khi tạo vị trí công việc mới:**

```csharp
[HttpPost]
public async Task<IActionResult> CreatePosition(Position position)
{
    // Lưu position vào database
    var createdPosition = await _positionService.CreateAsync(position);

    // Thông báo cho recruiter
    await _notificationIntegration.NotifyJobPostingApprovedAsync(
        recruiterId: position.RecruiterId,
        jobTitle: position.Title,
        positionId: createdPosition.PositionId
    );

    // Tìm candidates phù hợp và gửi thông báo
    var matchingCandidates = await _candidateService.FindMatchingAsync(position);
    foreach(var candidate in matchingCandidates)
    {
        await _notificationIntegration.NotifyNewJobMatchingCriteriaAsync(
            userId: candidate.UserId,
            jobTitle: position.Title,
            positionId: createdPosition.PositionId,
            matchReason: "Phù hợp với kỹ năng và kinh nghiệm"
        );
    }

    return Json(new { success = true });
}
```

#### **2. Khi user ứng tuyển:**

```csharp
[HttpPost]
public async Task<IActionResult> ApplyForJob(int positionId)
{
    var userId = GetCurrentUserId();
    var position = await _positionService.GetByIdAsync(positionId);

    // Tạo application
    var application = await _applicationService.CreateAsync(userId, positionId);

    // Thông báo cho candidate
    await _notificationIntegration.NotifyJobApplicationSubmittedAsync(
        userId: userId,
        jobTitle: position.Title,
        applicationId: application.ApplicationId
    );

    // Thông báo cho recruiter
    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
    {
        UserId = position.RecruiterId,
        Title = "Ứng viên mới ứng tuyển",
        Message = $"Có ứng viên mới ứng tuyển vào vị trí '{position.Title}'",
        NotificationType = NotificationTypes.JobApplication,
        RelatedEntityType = RelatedEntityTypes.Application,
        RelatedEntityId = application.ApplicationId
    });

    return Json(new { success = true });
}
```

#### **3. Khi admin duyệt công ty:**

```csharp
[HttpPost]
public async Task<IActionResult> ApproveCompany(int companyId)
{
    var company = await _companyService.GetByIdAsync(companyId);
    company.IsApproved = true;
    await _companyService.UpdateAsync(company);

    // Thông báo cho tất cả recruiters của công ty
    await _notificationService.BroadcastToCompanyUsersAsync(
        companyId: companyId,
        title: "Công ty được duyệt",
        message: $"Công ty '{company.Name}' đã được duyệt và có thể đăng tin tuyển dụng",
        notificationType: NotificationTypes.SystemAnnouncement
    );

    return Json(new { success = true });
}
```

---

## **5. API REFERENCE**

### 🔌 **REST API Endpoints**

#### **Notification API (`/api/Notification`)**

| Method   | Endpoint                           | Mô tả                     | Parameters                       |
| -------- | ---------------------------------- | ------------------------- | -------------------------------- |
| `GET`    | `/api/Notification`                | Lấy danh sách thông báo   | `page`, `pageSize`, `unreadOnly` |
| `GET`    | `/api/Notification/summary`        | Lấy tổng quan thông báo   | -                                |
| `GET`    | `/api/Notification/unread-count`   | Lấy số thông báo chưa đọc | -                                |
| `GET`    | `/api/Notification/{id}`           | Lấy thông báo theo ID     | `id`                             |
| `POST`   | `/api/Notification/mark-read/{id}` | Đánh dấu đã đọc           | `id`                             |
| `POST`   | `/api/Notification/mark-all-read`  | Đánh dấu tất cả đã đọc    | -                                |
| `DELETE` | `/api/Notification/{id}`           | Xóa thông báo             | `id`                             |
| `POST`   | `/api/Notification/bulk-action`    | Thao tác hàng loạt        | `BulkNotificationActionDto`      |

#### **MVC Endpoints (`/NotificationView`)**

| Method | Endpoint                       | Mô tả                  |
| ------ | ------------------------------ | ---------------------- |
| `GET`  | `/NotificationView`            | Trang thông báo đầy đủ |
| `GET`  | `/NotificationView/Unread`     | Chỉ thông báo chưa đọc |
| `POST` | `/NotificationView/MarkAsRead` | Đánh dấu đã đọc (form) |
| `POST` | `/NotificationView/Delete`     | Xóa thông báo (form)   |

### 📡 **SignalR Events**

#### **Client Events (Nhận từ server):**

```javascript
// Thông báo mới
connection.on("NewNotification", (notification) => {
  // notification: NotificationDto object
  console.log("New notification:", notification);
});

// Cập nhật số lượng
connection.on("NotificationCountUpdate", (data) => {
  // data: { count: number, timestamp: string }
  console.log("Count updated:", data.count);
});

// Thông báo hệ thống
connection.on("SystemNotification", (notification) => {
  // notification: NotificationDto object
  console.log("System notification:", notification);
});
```

#### **Server Events (Gửi từ client):**

```javascript
// Cập nhật hoạt động
await connection.invoke("UpdateActivity");

// Tham gia nhóm thông báo
await connection.invoke("JoinNotificationGroup", "groupName");

// Rời khỏi nhóm
await connection.invoke("LeaveNotificationGroup", "groupName");
```

---

## **6. FRONTEND INTEGRATION**

### 🎨 **JavaScript NotificationManager**

#### **Khởi tạo tự động:**

```javascript
// Đã được khởi tạo tự động trong _Layout.cshtml
window.notificationManager = new NotificationManager();
```

#### **Sử dụng NotificationManager:**

```javascript
// Lấy trạng thái
const status = window.notificationManager.getStatus();
console.log("SignalR connected:", status.signalRConnected);

// Cập nhật số lượng thông báo
await window.notificationManager.updateNotificationCount();

// Tải danh sách thông báo
await window.notificationManager.loadNotifications();

// Đánh dấu đã đọc
await window.notificationManager.markAsRead(notificationId);

// Xóa thông báo
await window.notificationManager.delete(notificationId);

// Đánh dấu tất cả đã đọc
await window.notificationManager.markAllAsRead();
```

#### **Lắng nghe events:**

```javascript
// Thông báo mới
window.addEventListener("newNotification", (event) => {
  const notification = event.detail;
  console.log("Received:", notification.title);
  // Xử lý thông báo mới
});

// Thông báo hệ thống
window.addEventListener("systemNotification", (event) => {
  const notification = event.detail;
  console.log("System:", notification.title);
  // Xử lý thông báo hệ thống
});
```

### 🎯 **Toast Notifications**

Toast notifications tự động xuất hiện khi có thông báo mới:

```javascript
// Toast sẽ tự động hiển thị với:
// - Icon tương ứng với loại thông báo
// - Màu sắc phù hợp
// - Nút "Đánh dấu đã đọc"
// - Tự động biến mất sau 5 giây
```

### 🔔 **Notification Bell**

Badge counter trong header tự động cập nhật:

```html
<!-- Đã được tích hợp trong _Layout.cshtml -->
<div class="dropdown">
  <button class="btn btn-link position-relative" id="notificationDropdown">
    <i class="fas fa-bell"></i>
    <span id="notification-badge" class="badge bg-danger">3</span>
  </button>
</div>
```

---

## **7. TESTING & DEBUGGING**

### 🧪 **Test Pages**

#### **1. Basic Testing (`/Test/Notification`):**

- Tạo các loại thông báo mẫu
- Kiểm tra real-time delivery
- Monitor SignalR connection status
- Test bulk operations

#### **2. Real-time Testing (`/Test/RealTimeNotifications`):**

- Test multi-tab synchronization
- Monitor real-time events
- Connection resilience testing
- Auto-create mode for stress testing

### 🔍 **Debugging Tools**

#### **Browser Console:**

```javascript
// Kiểm tra trạng thái NotificationManager
console.log(window.notificationManager.getStatus());

// Kiểm tra SignalR connection
console.log(window.notificationManager.signalRConnection.state);

// Test manual reconnection
await window.notificationManager.reconnectSignalR();
```

#### **Server Logs:**

```csharp
// Logs được ghi tự động cho:
// - Notification creation
// - SignalR connections/disconnections
// - Real-time delivery attempts
// - Errors and exceptions
```

### 📊 **Performance Monitoring**

```javascript
// Monitor notification count updates
let updateCount = 0;
window.addEventListener("newNotification", () => {
  updateCount++;
  console.log(`Received ${updateCount} real-time notifications`);
});
```

---

## **8. BEST PRACTICES**

### ✅ **Dos**

1. **Sử dụng NotificationIntegrationService** cho business scenarios phổ biến
2. **Luôn handle exceptions** khi gửi thông báo
3. **Sử dụng appropriate notification types** cho từng trường hợp
4. **Test real-time functionality** trên multiple tabs/browsers
5. **Provide fallback mechanisms** khi SignalR không khả dụng

### ❌ **Don'ts**

1. **Không spam notifications** - tránh gửi quá nhiều thông báo cùng lúc
2. **Không ignore errors** - luôn log và handle exceptions
3. **Không hardcode user IDs** - luôn lấy từ context hoặc parameters
4. **Không skip validation** - validate input parameters
5. **Không block UI** - sử dụng async/await properly

### 🎯 **Performance Tips**

```csharp
// ✅ Good: Batch notifications
var notifications = new List<CreateNotificationDto>();
foreach(var user in users)
{
    notifications.Add(new CreateNotificationDto { ... });
}
// Process batch

// ❌ Bad: Individual calls in loop
foreach(var user in users)
{
    await _notificationService.CreateNotificationAsync(...);
}
```

### 🔒 **Security Considerations**

1. **Authorization**: Chỉ gửi thông báo cho users có quyền nhận
2. **Input Validation**: Validate tất cả input parameters
3. **Rate Limiting**: Implement rate limiting cho API endpoints
4. **CSRF Protection**: Sử dụng anti-forgery tokens

---

## **9. TROUBLESHOOTING**

### 🚨 **Common Issues**

#### **SignalR không kết nối:**

```javascript
// Check connection state
console.log(window.notificationManager.signalRConnection.state);

// Manual reconnection
await window.notificationManager.reconnectSignalR();
```

#### **Thông báo không hiển thị real-time:**

1. Kiểm tra SignalR connection
2. Verify user authentication
3. Check browser console for errors

#### **Badge count không cập nhật:**

```javascript
// Manual refresh
await window.notificationManager.updateNotificationCount();
```

### 📞 **Support**

Nếu gặp vấn đề, hãy:

1. Check browser console logs
2. Check server application logs
3. Test với `/Test/Notification` page
4. Verify database connections
5. Check SignalR hub registration in Program.cs

---

## **10. CHANGELOG & UPDATES**

### 🆕 **Version 1.0 Features**

- ✅ Real-time notifications via SignalR
- ✅ Toast notification system
- ✅ Comprehensive API endpoints
- ✅ Multi-tab synchronization
- ✅ 8 notification types with icons
- ✅ Broadcast capabilities
- ✅ Integration service for common scenarios
- ✅ Fallback to polling mechanism
- ✅ Test pages for debugging

### 🔮 **Future Enhancements**

- 📧 Email notification integration
- 📱 Push notifications for mobile
- 🎨 Customizable notification templates
- 📈 Analytics and reporting
- 🔔 User notification preferences
- 🌐 Multi-language support

---

**🎉 Hệ thống thông báo đã sẵn sàng sử dụng!**

**Test ngay tại:** `/Test/Notification`
