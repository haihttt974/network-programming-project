Create database DKyThucTap
go

use DKyThucTap
go

---

-- 1. Bảng `roles`: Lưu trữ các vai trò người dùng (ví dụ: 'ứng viên', 'nhà tuyển dụng', 'quản trị')
CREATE TABLE roles (
    role_id INT PRIMARY KEY IDENTITY(1,1),
    role_name VARCHAR(50) UNIQUE NOT NULL,
    permissions NVARCHAR(MAX) -- Dùng NVARCHAR(MAX) để lưu JSON cho quyền hạn
);

---

-- 2. Bảng `users`: Thông tin người dùng cơ bản
CREATE TABLE users (
    user_id INT PRIMARY KEY IDENTITY(1,1),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(role_id),
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    last_login DATETIMEOFFSET,
    is_active BIT DEFAULT 1
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role_id);

---

-- 3. Bảng `user_profiles`: Thông tin hồ sơ chi tiết của người dùng
CREATE TABLE user_profiles (
    profile_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT UNIQUE NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    address NVARCHAR(MAX),
    cv_url VARCHAR(255),
    profile_picture_url VARCHAR(255),
    bio NVARCHAR(MAX),
    updated_at DATETIMEOFFSET DEFAULT GETUTCDATE()
);

---

-- 4. Bảng `companies`: Thông tin các công ty
CREATE TABLE companies (
    company_id INT PRIMARY KEY IDENTITY(1,1),
    name VARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    logo_url VARCHAR(255),
    website VARCHAR(255),
    industry VARCHAR(100),
    location VARCHAR(255),
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    created_by INT REFERENCES users(user_id) -- Người dùng (thường là nhà tuyển dụng/admin) tạo công ty này
);

CREATE INDEX idx_companies_name ON companies(name);

---

-- 5. Bảng `skills`: Danh sách các kỹ năng
CREATE TABLE skills (
    skill_id INT PRIMARY KEY IDENTITY(1,1),
    name VARCHAR(100) UNIQUE NOT NULL,
    category VARCHAR(100) -- Ví dụ: 'Programming', 'Soft Skills', 'Design'
);

---

-- 6. Bảng `job_categories`: Danh mục cho các vị trí tuyển dụng
CREATE TABLE job_categories (
    category_id INT PRIMARY KEY IDENTITY(1,1),
    category_name VARCHAR(100) UNIQUE NOT NULL,
    description NVARCHAR(MAX)
);

---

-- 7. Bảng `positions`: Thông tin về các vị trí thực tập/tuyển dụng
CREATE TABLE positions (
    position_id INT PRIMARY KEY IDENTITY(1,1),
    company_id INT NOT NULL REFERENCES companies(company_id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NOT NULL,
    position_type VARCHAR(50) NOT NULL, -- Ví dụ: 'internship', 'full-time', 'part-time'
    location VARCHAR(255),
    is_remote BIT DEFAULT 0,
    salary_range VARCHAR(100),
    application_deadline DATE,
    is_active BIT DEFAULT 1,
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    created_by INT REFERENCES users(user_id), -- Người dùng (thường là nhà tuyển dụng) tạo vị trí này
    category_id INT REFERENCES job_categories(category_id) -- Liên kết với danh mục công việc
);

CREATE INDEX idx_positions_company ON positions(company_id);
CREATE INDEX idx_positions_type ON positions(position_type);
CREATE INDEX idx_positions_active ON positions(is_active);
CREATE INDEX idx_positions_category ON positions(category_id);

---

-- 8. Bảng `position_skills`: Kỹ năng yêu cầu cho từng vị trí
CREATE TABLE position_skills (
    position_id INT REFERENCES positions(position_id) ON DELETE CASCADE,
    skill_id INT REFERENCES skills(skill_id) ON DELETE CASCADE,
    is_required BIT DEFAULT 1, -- Kỹ năng này có bắt buộc không
    PRIMARY KEY (position_id, skill_id)
);

---

-- 9. Bảng `user_skills`: Kỹ năng của từng người dùng
CREATE TABLE user_skills (
    user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
    skill_id INT REFERENCES skills(skill_id) ON DELETE CASCADE,
    proficiency_level INT CHECK (proficiency_level BETWEEN 1 AND 5), -- Mức độ thành thạo (1-5)
    PRIMARY KEY (user_id, skill_id)
);

---

-- 10. Bảng `applications`: Đơn ứng tuyển của người dùng vào vị trí
CREATE TABLE applications (
    application_id INT PRIMARY KEY IDENTITY(1,1),
    position_id INT NOT NULL REFERENCES positions(position_id) ON DELETE CASCADE,
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    current_status VARCHAR(50) NOT NULL DEFAULT 'applied', -- Ví dụ: 'applied', 'under_review', 'interview', 'offered', 'rejected', 'hired'
    applied_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    cover_letter NVARCHAR(MAX),
    additional_info NVARCHAR(MAX), -- Dùng NVARCHAR(MAX) để lưu JSON cho thông tin bổ sung (ví dụ: câu hỏi tùy chỉnh)
    UNIQUE(position_id, user_id) -- Đảm bảo một người dùng chỉ nộp 1 đơn cho 1 vị trí
);

CREATE INDEX idx_applications_user ON applications(user_id);
CREATE INDEX idx_applications_position ON applications(position_id);
CREATE INDEX idx_applications_status ON applications(current_status);

---

-- 11. Bảng `application_status_history`: Lịch sử thay đổi trạng thái của đơn ứng tuyển
CREATE TABLE application_status_history (
    history_id INT PRIMARY KEY IDENTITY(1,1),
    application_id INT NOT NULL REFERENCES applications(application_id) ON DELETE CASCADE,
    status VARCHAR(50) NOT NULL, -- Trạng thái mới
    changed_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    changed_by INT REFERENCES users(user_id), -- Người dùng (nhà tuyển dụng/admin) thực hiện thay đổi
    notes NVARCHAR(MAX) -- Ghi chú về lý do thay đổi trạng thái
);

CREATE INDEX idx_status_history_app ON application_status_history(application_id);

---

-- 12. Bảng `position_history`: Lịch sử thay đổi của một vị trí tuyển dụng
CREATE TABLE position_history (
    history_id INT PRIMARY KEY IDENTITY(1,1),
    position_id INT NOT NULL REFERENCES positions(position_id) ON DELETE CASCADE,
    changed_by_user_id INT REFERENCES users(user_id), -- Người dùng thực hiện thay đổi
    changed_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    change_type VARCHAR(50) NOT NULL, -- Ví dụ: 'update_description', 'extend_deadline', 'deactivate'
    old_value NVARCHAR(MAX), -- Lưu giá trị cũ (có thể là JSON của một phần hoặc toàn bộ đối tượng)
    new_value NVARCHAR(MAX),  -- Lưu giá trị mới (có thể là JSON của một phần hoặc toàn bộ đối tượng)
    notes NVARCHAR(MAX)
);

CREATE INDEX idx_position_history_position ON position_history(position_id);
CREATE INDEX idx_position_history_user ON position_history(changed_by_user_id);

---

-- 13. Bảng `company_reviews`: Đánh giá của người dùng về công ty
CREATE TABLE company_reviews (
    review_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE, -- Người dùng (ứng viên) gửi đánh giá
    company_id INT NOT NULL REFERENCES companies(company_id) ON DELETE CASCADE,
    rating INT CHECK (rating BETWEEN 1 AND 5) NOT NULL, -- Điểm đánh giá từ 1 đến 5 sao
    comment NVARCHAR(MAX),
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    is_approved BIT DEFAULT 0 -- Cần duyệt bởi quản trị viên trước khi hiển thị
);

CREATE INDEX idx_company_reviews_user ON company_reviews(user_id);
CREATE INDEX idx_company_reviews_company ON company_reviews(company_id);

---

-- 14. Bảng `applicant_notes`: Ghi chú nội bộ của nhà tuyển dụng về ứng viên
CREATE TABLE applicant_notes (
    note_id INT PRIMARY KEY IDENTITY(1,1),
    application_id INT NOT NULL REFERENCES applications(application_id) ON DELETE CASCADE,
    interviewer_user_id INT NOT NULL REFERENCES users(user_id), -- Người tạo ghi chú (thường là nhà tuyển dụng)
    note_text NVARCHAR(MAX) NOT NULL,
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE()
);

CREATE INDEX idx_applicant_notes_application ON applicant_notes(application_id);
CREATE INDEX idx_applicant_notes_interviewer ON applicant_notes(interviewer_user_id);

---

-- 15. Bảng `conversations`: Lưu trữ các cuộc hội thoại giữa 2 người dùng
CREATE TABLE conversations (
    conversation_id INT PRIMARY KEY IDENTITY(1,1),
    participant1_user_id INT NOT NULL REFERENCES users(user_id),
    participant2_user_id INT NOT NULL REFERENCES users(user_id),
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    last_message_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    -- Đảm bảo chỉ có một cuộc trò chuyện duy nhất giữa một cặp người dùng,
    -- cần sắp xếp ID người dùng để tránh trùng lặp (ví dụ: (1,2) và (2,1) là như nhau)
    UNIQUE (participant1_user_id, participant2_user_id) -- Có thể cần CHECK constraint để đảm bảo participant1_user_id < participant2_user_id nếu muốn chuẩn hóa hoàn toàn UNIQUE
);

CREATE INDEX idx_conversations_p1 ON conversations(participant1_user_id);
CREATE INDEX idx_conversations_p2 ON conversations(participant2_user_id);

---

-- 16. Bảng `messages`: Lưu trữ nội dung tin nhắn trong các cuộc hội thoại
CREATE TABLE messages (
    message_id INT PRIMARY KEY IDENTITY(1,1),
    conversation_id INT NOT NULL REFERENCES conversations(conversation_id) ON DELETE CASCADE,
    sender_user_id INT NOT NULL REFERENCES users(user_id),
    message_text NVARCHAR(MAX) NOT NULL,
    sent_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    is_read BIT DEFAULT 0 -- Trạng thái đã đọc/chưa đọc
);

CREATE INDEX idx_messages_conversation ON messages(conversation_id);
CREATE INDEX idx_messages_sender ON messages(sender_user_id);

---

-- 17. Bảng `notifications`: Lưu trữ thông báo gửi đến người dùng
CREATE TABLE notifications (
    notification_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    message NVARCHAR(MAX) NOT NULL,
    is_read BIT DEFAULT 0,
    created_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    related_entity_type VARCHAR(50), -- Ví dụ: 'application', 'position', 'conversation', 'review'
    related_entity_id INT, -- ID của thực thể liên quan (ví dụ: application_id, position_id, conversation_id)
    notification_type VARCHAR(50) -- Ví dụ: 'status_change', 'new_position', 'new_message', 'new_review'
);

CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_read ON notifications(is_read);

---

-- 18. Bảng `websocket_connections`: Quản lý các kết nối WebSocket đang hoạt động
CREATE TABLE websocket_connections (
    connection_id VARCHAR(255) PRIMARY KEY, -- ID duy nhất cho mỗi kết nối WebSocket
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    connected_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
    last_activity DATETIMEOFFSET DEFAULT GETUTCDATE(),
    client_info NVARCHAR(MAX) -- Thông tin client (ví dụ: IP, User-Agent) dưới dạng JSON
);

CREATE INDEX idx_websocket_user ON websocket_connections(user_id);

USE DKyThucTap
GO

/*
--------------------------------------------------------------------------------
-- SCRIPT SỬA LỖI CHUYỂN ĐỔI VARCHAR SANG NVARCHAR
-- Lý do lỗi: Không thể thay đổi cột khi có các đối tượng phụ thuộc (Index, Constraint).
-- Giải pháp: Xóa các đối tượng phụ thuộc, thay đổi kiểu dữ liệu cột, sau đó tạo lại chúng.
--------------------------------------------------------------------------------
*/

-- 1. Bảng `roles` (Sửa lỗi UNIQUE constraint)
-- Lấy tên UNIQUE constraint của bảng roles
DECLARE @role_unique_constraint_name NVARCHAR(128);
SELECT @role_unique_constraint_name = name
FROM sys.key_constraints
WHERE type = 'UQ' AND parent_object_id = OBJECT_ID('dbo.roles');
-- Xóa constraint nếu tồn tại
IF @role_unique_constraint_name IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.roles DROP CONSTRAINT ' + @role_unique_constraint_name);
END
-- Thay đổi kiểu dữ liệu cột
ALTER TABLE roles ALTER COLUMN role_name NVARCHAR(50) NOT NULL;
-- Tạo lại UNIQUE constraint
ALTER TABLE roles ADD CONSTRAINT UQ_roles_role_name UNIQUE (role_name);
GO

-- 2. Bảng `user_profiles` (Không có lỗi, giữ nguyên)
ALTER TABLE user_profiles ALTER COLUMN first_name NVARCHAR(100) NOT NULL;
ALTER TABLE user_profiles ALTER COLUMN last_name NVARCHAR(100) NOT NULL;
GO

-- 3. Bảng `companies` (Sửa lỗi Index)
-- Xóa index
DROP INDEX IF EXISTS idx_companies_name ON companies;
-- Thay đổi kiểu dữ liệu cột
ALTER TABLE companies ALTER COLUMN name NVARCHAR(255) NOT NULL;
-- Tạo lại index
CREATE INDEX idx_companies_name ON companies(name);
-- Thay đổi các cột khác
ALTER TABLE companies ALTER COLUMN industry NVARCHAR(100);
ALTER TABLE companies ALTER COLUMN location NVARCHAR(255);
GO

-- 4. Bảng `skills` (Sửa lỗi UNIQUE constraint)
DECLARE @skill_unique_constraint_name NVARCHAR(128);
SELECT @skill_unique_constraint_name = name
FROM sys.key_constraints
WHERE type = 'UQ' AND parent_object_id = OBJECT_ID('dbo.skills');
IF @skill_unique_constraint_name IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.skills DROP CONSTRAINT ' + @skill_unique_constraint_name);
END
-- Thay đổi kiểu dữ liệu cột
ALTER TABLE skills ALTER COLUMN name NVARCHAR(100) NOT NULL;
-- Tạo lại UNIQUE constraint
ALTER TABLE skills ADD CONSTRAINT UQ_skills_name UNIQUE (name);
-- Thay đổi cột khác
ALTER TABLE skills ALTER COLUMN category NVARCHAR(100);
GO

-- 5. Bảng `job_categories` (Sửa lỗi UNIQUE constraint)
DECLARE @jobcat_unique_constraint_name NVARCHAR(128);
SELECT @jobcat_unique_constraint_name = name
FROM sys.key_constraints
WHERE type = 'UQ' AND parent_object_id = OBJECT_ID('dbo.job_categories');
IF @jobcat_unique_constraint_name IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.job_categories DROP CONSTRAINT ' + @jobcat_unique_constraint_name);
END
-- Thay đổi kiểu dữ liệu cột
ALTER TABLE job_categories ALTER COLUMN category_name NVARCHAR(100) NOT NULL;
-- Tạo lại UNIQUE constraint
ALTER TABLE job_categories ADD CONSTRAINT UQ_job_categories_category_name UNIQUE (category_name);
GO

-- 6. Bảng `positions` (Sửa lỗi Index)
-- Xóa index
DROP INDEX IF EXISTS idx_positions_type ON positions;
-- Thay đổi kiểu dữ liệu cột
ALTER TABLE positions ALTER COLUMN position_type NVARCHAR(50) NOT NULL;
-- Tạo lại index
CREATE INDEX idx_positions_type ON positions(position_type);
-- Thay đổi các cột khác
ALTER TABLE positions ALTER COLUMN title NVARCHAR(255) NOT NULL;
ALTER TABLE positions ALTER COLUMN location NVARCHAR(255);
ALTER TABLE positions ALTER COLUMN salary_range NVARCHAR(100);
GO

-- 7. Bảng `applications` (Sửa lỗi DEFAULT constraint và Index)
-- Lấy tên DEFAULT constraint
DECLARE @app_default_constraint_name NVARCHAR(128);
SELECT @app_default_constraint_name = name
FROM sys.default_constraints
WHERE parent_object_id = OBJECT_ID('dbo.applications') AND parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.applications') AND name = 'current_status');
-- Xóa DEFAULT constraint nếu tồn tại
IF @app_default_constraint_name IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.applications DROP CONSTRAINT ' + @app_default_constraint_name);
END
-- Xóa index
DROP INDEX IF EXISTS idx_applications_status ON applications;
-- Thay đổi kiểu dữ liệu cột
ALTER TABLE applications ALTER COLUMN current_status NVARCHAR(50) NOT NULL;
-- Tạo lại DEFAULT constraint
ALTER TABLE applications ADD CONSTRAINT DF_applications_current_status DEFAULT N'applied' FOR current_status;
-- Tạo lại index
CREATE INDEX idx_applications_status ON applications(current_status);
GO

-- 8. Bảng `application_status_history` (Không có lỗi, giữ nguyên)
ALTER TABLE application_status_history
ALTER COLUMN status NVARCHAR(50) NOT NULL;
GO

-- 9. Bảng `position_history` (Không có lỗi, giữ nguyên)
ALTER TABLE position_history
ALTER COLUMN change_type NVARCHAR(50) NOT NULL;
GO

-- 10. Bảng `notifications` (Không có lỗi, giữ nguyên)
ALTER TABLE notifications
ALTER COLUMN title NVARCHAR(255) NOT NULL;
ALTER TABLE notifications
ALTER COLUMN related_entity_type NVARCHAR(50);
ALTER TABLE notifications
ALTER COLUMN notification_type NVARCHAR(50);
GO

PRINT '✅ Cập nhật thành công tất cả các cột VARCHAR sang NVARCHAR và tạo lại các đối tượng phụ thuộc!';

-- =============================================
-- File: SeedData.sql
-- Mô tả: Dữ liệu mẫu cho hệ thống đăng ký thực tập
-- Tác giả: System
-- Ngày tạo: 2024
-- =============================================

USE DKyThucTap
GO

-- Xóa dữ liệu cũ (nếu có) theo thứ tự ngược lại để tránh lỗi foreign key
DELETE FROM websocket_connections;
DELETE FROM notifications;
DELETE FROM messages;
DELETE FROM conversations;
DELETE FROM applicant_notes;
DELETE FROM application_status_history;
DELETE FROM applications;
DELETE FROM position_skills;
DELETE FROM user_skills;
DELETE FROM position_history;
DELETE FROM positions;
DELETE FROM company_reviews;
DELETE FROM companies;
DELETE FROM user_profiles;
DELETE FROM users;
DELETE FROM skills;
DELETE FROM job_categories;
DELETE FROM roles;
GO

-- =============================================
-- 1. BẢNG ROLES - Vai trò người dùng
-- =============================================
-- Tạo các vai trò cơ bản trong hệ thống
INSERT INTO roles (role_name, permissions) VALUES
('Candidate', '{"view_positions": true, "apply_position": true, "view_applications": true, "send_messages": true}'),
('Recruiter', '{"create_position": true, "manage_applications": true, "view_candidates": true, "send_messages": true, "create_company": true}'),
('Admin', '{"manage_users": true, "manage_companies": true, "manage_positions": true, "view_all_data": true, "moderate_reviews": true}');
GO

-- =============================================
-- 2. BẢNG JOB_CATEGORIES - Danh mục công việc
-- =============================================
-- Các danh mục công việc phổ biến trong lĩnh vực IT và kinh doanh
INSERT INTO job_categories (category_name, description) VALUES
(N'Công nghệ thông tin', N'Các vị trí liên quan đến phát triển phần mềm, hệ thống, và công nghệ'),
(N'Marketing & Truyền thông', N'Các vị trí về marketing, quảng cáo, và truyền thông'),
(N'Kinh doanh & Bán hàng', N'Các vị trí về kinh doanh, bán hàng, và phát triển thị trường'),
(N'Thiết kế & Sáng tạo', N'Các vị trí về thiết kế đồ họa, UI/UX, và sáng tạo nội dung'),
(N'Tài chính & Kế toán', N'Các vị trí về tài chính, kế toán, và phân tích dữ liệu tài chính');
GO

-- =============================================
-- 3. BẢNG SKILLS - Kỹ năng
-- =============================================
-- Danh sách các kỹ năng phổ biến được phân loại theo nhóm
INSERT INTO skills (name, category) VALUES
-- Kỹ năng lập trình
('Java', 'Programming'),
('C#', 'Programming'),
('Python', 'Programming'),
('JavaScript', 'Programming'),
('React', 'Programming'),
('Angular', 'Programming'),
('Node.js', 'Programming'),
('ASP.NET Core', 'Programming'),
('SQL Server', 'Database'),
('MySQL', 'Database'),
('MongoDB', 'Database'),
-- Kỹ năng thiết kế
('Photoshop', 'Design'),
('Illustrator', 'Design'),
('Blender', 'Design'),
('Figma', 'Design'),
('UI/UX Design', 'Design'),
-- Kỹ năng marketing
('Digital Marketing', 'Marketing'),
('SEO/SEM', 'Marketing'),
('Content Marketing', 'Marketing'),
('Social Media Marketing', 'Marketing'),
-- Kỹ năng mềm
(N'Giao tiếp', 'Soft Skills'),
(N'Làm việc nhóm', 'Soft Skills'),
(N'Quản lý thời gian', 'Soft Skills'),
(N'Tư duy phản biện', 'Soft Skills'),
(N'Tiếng Anh', 'Language');
GO

-- =============================================
-- 4. BẢNG USERS - Người dùng
-- =============================================
-- Tạo các tài khoản người dùng với mật khẩu đã hash (password123)
INSERT INTO users (email, password_hash, role_id, created_at, is_active) VALUES
-- Admin
('admin@dkythuctap.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 3, DATEADD(day, -30, GETUTCDATE()), 1),
-- Recruiters
('recruiter1@techcorp.vn', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 2, DATEADD(day, -25, GETUTCDATE()), 1),
('recruiter2@fptsoft.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 2, DATEADD(day, -20, GETUTCDATE()), 1),
('recruiter3@viettel.vn', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 2, DATEADD(day, -18, GETUTCDATE()), 1),
-- Candidates
('nguyenvana@gmail.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 1, DATEADD(day, -15, GETUTCDATE()), 1),
('tranthib@gmail.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 1, DATEADD(day, -12, GETUTCDATE()), 1),
('lequangc@gmail.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 1, DATEADD(day, -10, GETUTCDATE()), 1),
('phamthid@gmail.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 1, DATEADD(day, -8, GETUTCDATE()), 1),
('hoangvane@gmail.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 1, DATEADD(day, -6, GETUTCDATE()), 1),
('vuthif@gmail.com', '$2a$12$ex4F.vBRSfYJw6cCu7w2Mu7PSHl3GncU74auflf5u5mZFnnZSMJ4.', 1, DATEADD(day, -4, GETUTCDATE()), 1);
GO

-- =============================================
-- 5. BẢNG USER_PROFILES - Hồ sơ người dùng
-- =============================================
-- Thông tin chi tiết của từng người dùng
INSERT INTO user_profiles (user_id, first_name, last_name, phone, address, cv_url, profile_picture_url, bio) VALUES
-- Admin profile
(1, N'Quản trị', N'Hệ thống', '0901234567', N'Hà Nội, Việt Nam', NULL, '/images/profiles/admin.jpg', N'Quản trị viên hệ thống đăng ký thực tập'),
-- Recruiter profiles
(2, N'Nguyễn Minh', N'Tuấn', '0912345678', N'123 Đường Láng, Đống Đa, Hà Nội', NULL, '/images/profiles/recruiter1.jpg', N'HR Manager tại TechCorp với 5 năm kinh nghiệm tuyển dụng IT'),
(3, N'Trần Thị', N'Hương', N'0923456789', N'456 Cầu Giấy, Hà Nội', NULL, '/images/profiles/recruiter2.jpg', N'Chuyên viên tuyển dụng tại FPT Software, chuyên về các vị trí lập trình'),
(4, N'Lê Văn', N'Đức', '0934567890', N'789 Nguyễn Trãi, Thanh Xuân, Hà Nội', NULL, '/images/profiles/recruiter3.jpg', N'Senior Recruiter tại Viettel, tập trung vào tuyển dụng nhân tài công nghệ'),
-- Candidate profiles
(5, N'Nguyễn Văn', N'An', '0945678901', N'12 Phố Huế, Hai Bà Trưng, Hà Nội', '/files/cv/nguyenvana_cv.pdf', '/images/profiles/candidate1.jpg', N'Sinh viên năm 4 ngành Công nghệ thông tin, đam mê lập trình web và mobile'),
(6, N'Trần Thị', N'Bình', '0956789012', N'34 Tây Sơn, Đống Đa, Hà Nội', '/files/cv/tranthib_cv.pdf', '/images/profiles/candidate2.jpg', N'Sinh viên Marketing, có kinh nghiệm thực tế về digital marketing và content creation'),
(7, N'Lê Quang', N'Cường', '0967890123', N'56 Giải Phóng, Hai Bà Trưng, Hà Nội', '/files/cv/lequangc_cv.pdf', '/images/profiles/candidate3.jpg', N'Sinh viên Thiết kế đồ họa, thành thạo các công cụ Adobe và có portfolio ấn tượng'),
(8, N'Phạm Thị', N'Dung', '0978901234', N'78 Xã Đàn, Đống Đa, Hà Nội', '/files/cv/phamthid_cv.pdf', '/images/profiles/candidate4.jpg', N'Sinh viên Kế toán - Tài chính, có chứng chỉ Excel và đang học Power BI'),
(9, N'Hoàng Văn', N'Em', '0989012345', N'90 Thái Hà, Đống Đa, Hà Nội', '/files/cv/hoangvane_cv.pdf', '/images/profiles/candidate5.jpg', N'Sinh viên CNTT chuyên về AI/ML, có nhiều project cá nhân trên GitHub'),
(10, N'Vũ Thị', N'Phương', '0990123456', N'12 Láng Hạ, Ba Đình, Hà Nội', '/files/cv/vuthif_cv.pdf', '/images/profiles/candidate6.jpg', N'Sinh viên Kinh doanh quốc tế, thành thạo tiếng Anh và có kinh nghiệm bán hàng online');
GO

-- =============================================
-- 6. BẢNG COMPANIES - Công ty
-- =============================================
-- Thông tin các công ty tuyển dụng
INSERT INTO companies (name, description, logo_url, website, industry, location, created_by) VALUES
('TechCorp Vietnam', N'Công ty công nghệ hàng đầu chuyên phát triển phần mềm doanh nghiệp và giải pháp số hóa', '/images/logos/techcorp.png', 'https://techcorp.vn', N'Công nghệ thông tin', N'Hà Nội', 2),
('FPT Software', N'Công ty phần mềm lớn nhất Việt Nam, cung cấp dịch vụ phát triển phần mềm và chuyển đổi số', '/images/logos/fptsoft.png', 'https://fptsoft.com', N'Công nghệ thông tin', N'Hà Nội, TP.HCM, Đà Nẵng', 3),
('Viettel Group', N'Tập đoàn viễn thông và công nghệ hàng đầu Việt Nam', '/images/logos/viettel.png', 'https://viettel.vn', N'Viễn thông & Công nghệ', N'Hà Nội', 4),
('VNG Corporation', N'Công ty internet và công nghệ hàng đầu Việt Nam, phát triển các sản phẩm số', '/images/logos/vng.png', 'https://vng.com.vn', N'Internet & Game', N'TP.HCM', 2),
('Sendo Technology', N'Nền tảng thương mại điện tử và công nghệ fintech', '/images/logos/sendo.png', 'https://sendo.vn', N'E-commerce & Fintech', N'TP.HCM', 3);
GO

-- =============================================
-- 7. BẢNG POSITIONS - Vị trí tuyển dụng
-- =============================================
-- Các vị trí thực tập và việc làm
INSERT INTO positions (company_id, title, description, position_type, location, is_remote, salary_range, application_deadline, category_id, created_by) VALUES
-- TechCorp positions
(1, N'Thực tập sinh Lập trình Web', N'Tham gia phát triển ứng dụng web sử dụng ASP.NET Core và React. Học hỏi từ đội ngũ senior developer có kinh nghiệm.', 'internship', N'Hà Nội', 0, N'3-5 triệu VND', DATEADD(day, 30, GETDATE()), 1, 2),
(1, N'Thực tập sinh UI/UX Designer', N'Thiết kế giao diện người dùng cho các ứng dụng web và mobile. Sử dụng Figma và Adobe Creative Suite.', 'internship', N'Hà Nội', 1, N'4-6 triệu VND', DATEADD(day, 25, GETDATE()), 4, 2),
(1, N'Junior Developer Full-time', N'Vị trí developer chính thức cho ứng viên có kinh nghiệm. Phát triển và maintain các hệ thống enterprise.', 'full-time', N'Hà Nội', 0, N'12-18 triệu VND', DATEADD(day, 45, GETDATE()), 1, 2),

-- FPT Software positions 
(2, N'Thực tập sinh Java Developer', N'Phát triển ứng dụng Java Spring Boot. Training intensive 3 tháng với mentor 1-1.', 'internship', N'Hà Nội', 0, N'4-7 triệu VND', DATEADD(day, 20, GETDATE()), 1, 3),
(2, N'Thực tập sinh Digital Marketing', N'Hỗ trợ team marketing trong việc quản lý social media, content creation và SEO.', 'internship', N'TP.HCM', 1, N'3-5 triệu VND', DATEADD(day, 35, GETDATE()), 2, 3),
(2, N'Business Analyst Intern', N'Phân tích yêu cầu nghiệp vụ, viết tài liệu đặc tả và hỗ trợ team development.', 'internship', N'Đà Nẵng', 0, N'5-7 triệu VND', DATEADD(day, 40, GETDATE()), 3, 3),

-- Viettel positions
(3, N'Thực tập sinh Data Analyst', N'Phân tích dữ liệu viễn thông, xây dựng dashboard và báo cáo insight.', 'internship', N'Hà Nội', 0, N'5-8 triệu VND', DATEADD(day, 28, GETDATE()), 1, 4),
(3, N'Thực tập sinh Mobile Developer', N'Phát triển ứng dụng mobile native (Android/iOS) cho các sản phẩm Viettel.', 'internship', N'Hà Nội', 0, N'6-9 triệu VND', DATEADD(day, 32, GETDATE()), 1, 4),

-- VNG positions
(4, N'Game Developer Intern', N'Tham gia phát triển game mobile sử dụng Unity. Cơ hội làm việc với các hit game của VNG.', 'internship', N'TP.HCM', 0, N'7-10 triệu VND', DATEADD(day, 22, GETDATE()), 1, 2),
(4, N'Product Marketing Intern', N'Hỗ trợ marketing cho các sản phẩm game và ứng dụng của VNG.', 'internship', N'TP.HCM', 1, N'4-6 triệu VND', DATEADD(day, 38, GETDATE()), 2, 2),

-- Sendo positions
(5, N'Frontend Developer Intern', N'Phát triển giao diện website và mobile app Sendo sử dụng React và React Native.', 'internship', N'TP.HCM', 1, N'5-8 triệu VND', DATEADD(day, 26, GETDATE()), 1, 3),
(5, N'Finance Analyst Intern', N'Phân tích tài chính, lập báo cáo và hỗ trợ team finance trong các dự án fintech.', 'internship', N'TP.HCM', 0, N'4-7 triệu VND', DATEADD(day, 42, GETDATE()), 5, 3);
GO

-- =============================================
-- 8. BẢNG POSITION_SKILLS - Kỹ năng yêu cầu cho vị trí
-- =============================================
-- Liên kết kỹ năng với từng vị trí tuyển dụng
INSERT INTO position_skills (position_id, skill_id, is_required) VALUES
-- Position 1: Thực tập sinh Lập trình Web (TechCorp)
(1, 2, 1), -- C# (required)
(1, 8, 1), -- ASP.NET Core (required)  
(1, 5, 1), -- React (required)
(1, 9, 0), -- SQL Server (preferred)
(1, 21, 0), -- Giao tiếp (preferred)

-- Position 2: Thực tập sinh UI/UX Designer (TechCorp)
(2, 15, 1), -- Figma (required)
(2, 16, 1), -- UI/UX Design (required)
(2, 12, 0), -- Photoshop (preferred)
(2, 13, 0), -- Illustrator (preferred)

-- Position 3: Junior Developer Full-time (TechCorp)
(3, 2, 1), -- C# (required)
(3, 8, 1), -- ASP.NET Core (required)
(3, 4, 0), -- JavaScript (preferred)
(3, 9, 1), -- SQL Server (required)
(3, 22, 1), -- Làm việc nhóm (required)

-- Position 4: Thực tập sinh Java Developer (FPT)
(4, 1, 1), -- Java (required)
(4, 9, 0), -- SQL Server (preferred)
(4, 21, 1), -- Giao tiếp (required)
(4, 25, 0), -- Tiếng Anh (preferred)

-- Position 5: Thực tập sinh Digital Marketing (FPT)
(5, 17, 1), -- Digital Marketing (required)
(5, 18, 1), -- SEO/SEM (required)
(5, 19, 0), -- Content Marketing (preferred)
(5, 20, 0), -- Social Media Marketing (preferred)

-- Position 6: Business Analyst Intern (FPT)
(6, 21, 1), -- Giao tiếp (required)
(6, 24, 1), -- Tư duy phản biện (required)
(6, 25, 1), -- Tiếng Anh (required)
(6, 23, 0), -- Quản lý thời gian (preferred)

-- Position 7: Thực tập sinh Data Analyst (Viettel)
(7, 3, 1), -- Python (required)
(7, 9, 1), -- SQL Server (required)
(7, 24, 1), -- Tư duy phản biện (required)
(7, 25, 0), -- Tiếng Anh (preferred)

-- Position 8: Thực tập sinh Mobile Developer (Viettel)
(8, 1, 0), -- Java (preferred)
(8, 4, 1), -- JavaScript (required)
(8, 22, 1), -- Làm việc nhóm (required)
(8, 21, 1), -- Giao tiếp (required)

-- Position 9: Game Developer Intern (VNG)
(9, 2, 1), -- C# (required)
(9, 22, 1), -- Làm việc nhóm (required)
(9, 24, 0), -- Tư duy phản biện (preferred)

-- Position 10: Product Marketing Intern (VNG)
(10, 17, 1), -- Digital Marketing (required)
(10, 19, 1), -- Content Marketing (required)
(10, 20, 1), -- Social Media Marketing (required)
(10, 25, 0), -- Tiếng Anh (preferred)

-- Position 11: Frontend Developer Intern (Sendo)
(11, 4, 1), -- JavaScript (required)
(11, 5, 1), -- React (required)
(11, 21, 1), -- Giao tiếp (required)
(11, 22, 0), -- Làm việc nhóm (preferred)

-- Position 12: Finance Analyst Intern (Sendo)
(12, 24, 1), -- Tư duy phản biện (required)
(12, 25, 1), -- Tiếng Anh (required)
(12, 23, 1), -- Quản lý thời gian (required)
(12, 21, 0); -- Giao tiếp (preferred)
GO

-- =============================================
-- 9. BẢNG USER_SKILLS - Kỹ năng của người dùng
-- =============================================
-- Kỹ năng và trình độ của từng ứng viên (chỉ candidates)
INSERT INTO user_skills (user_id, skill_id, proficiency_level) VALUES
-- Nguyễn Văn An (user_id = 5) - Web Developer
(5, 2, 4), -- C# - Giỏi
(5, 8, 3), -- ASP.NET Core - Khá
(5, 4, 4), -- JavaScript - Giỏi  
(5, 5, 3), -- React - Khá
(5, 9, 3), -- SQL Server - Khá
(5, 21, 4), -- Giao tiếp - Giỏi
(5, 22, 4), -- Làm việc nhóm - Giỏi
(5, 25, 3), -- Tiếng Anh - Khá

-- Trần Thị Bình (user_id = 6) - Marketing
(6, 17, 4), -- Digital Marketing - Giỏi
(6, 18, 3), -- SEO/SEM - Khá
(6, 19, 4), -- Content Marketing - Giỏi
(6, 20, 5), -- Social Media Marketing - Xuất sắc
(6, 21, 5), -- Giao tiếp - Xuất sắc
(6, 25, 4), -- Tiếng Anh - Giỏi

-- Lê Quang Cường (user_id = 7) - Designer
(7, 12, 4), -- Photoshop - Giỏi
(7, 13, 4), -- Illustrator - Giỏi
(7, 15, 5), -- Figma - Xuất sắc
(7, 16, 4), -- UI/UX Design - Giỏi
(7, 21, 3), -- Giao tiếp - Khá
(7, 24, 4), -- Tư duy phản biện - Giỏi

-- Phạm Thị Dung (user_id = 8) - Finance
(8, 23, 4), -- Quản lý thời gian - Giỏi
(8, 24, 4), -- Tư duy phản biện - Giỏi
(8, 25, 3), -- Tiếng Anh - Khá
(8, 21, 4), -- Giao tiếp - Giỏi

-- Hoàng Văn Em (user_id = 9) - AI/ML Developer
(9, 3, 5), -- Python - Xuất sắc
(9, 1, 3), -- Java - Khá
(9, 9, 3), -- SQL Server - Khá
(9, 11, 2), -- MongoDB - Trung bình
(9, 22, 3), -- Làm việc nhóm - Khá
(9, 24, 5), -- Tư duy phản biện - Xuất sắc
(9, 25, 4), -- Tiếng Anh - Giỏi

-- Vũ Thị Phương (user_id = 10) - Business
(10, 21, 5), -- Giao tiếp - Xuất sắc
(10, 22, 4), -- Làm việc nhóm - Giỏi
(10, 23, 4), -- Quản lý thời gian - Giỏi
(10, 25, 5), -- Tiếng Anh - Xuất sắc
(10, 17, 2), -- Digital Marketing - Trung bình
(10, 24, 4); -- Tư duy phản biện - Giỏi
GO

-- =============================================
-- 10. BẢNG APPLICATIONS - Đơn ứng tuyển
-- =============================================
-- Các đơn ứng tuyển của candidates vào các vị trí
INSERT INTO applications (position_id, user_id, current_status, applied_at, cover_letter, additional_info) VALUES
-- Nguyễn Văn An applications
(1, 5, 'hired', DATEADD(day, -10, GETUTCDATE()), N'Em rất quan tâm đến vị trí thực tập lập trình web tại TechCorp. Em có kinh nghiệm với C# và React qua các project học tập.', N'{"years_of_experience": 1, "portfolio_url": "https://github.com/nguyenvana", "expected_start_date": "2024-06-01"}'),
(3, 5, 'under_review', DATEADD(day, -5, GETUTCDATE()), N'Em muốn ứng tuyển vị trí Junior Developer để phát triển sự nghiệp lâu dài tại TechCorp.', N'{"years_of_experience": 1, "portfolio_url": "https://github.com/nguyenvana", "willing_to_relocate": true}'),

-- Trần Thị Bình applications 
(5, 6, 'interview', DATEADD(day, -8, GETUTCDATE()), N'Em có kinh nghiệm thực tế về digital marketing và rất mong muốn được học hỏi tại FPT Software.', N'{"social_media_experience": "2 years", "content_samples": "https://portfolio.tranthib.com", "certifications": ["Google Analytics", "Facebook Blueprint"]}'),
(10, 6, 'applied', DATEADD(day, -3, GETUTCDATE()), N'Em quan tâm đến vị trí marketing tại VNG và mong muốn đóng góp vào sự phát triển các sản phẩm game.', N'{"gaming_interest": true, "marketing_campaigns": 5}'),

-- Lê Quang Cường applications
(2, 7, 'offered', DATEADD(day, -12, GETUTCDATE()), N'Em là sinh viên thiết kế với portfolio đa dạng và kinh nghiệm sử dụng Figma, Photoshop.', N'{"portfolio_url": "https://behance.net/lequangcuong", "design_experience": "3 years", "specialization": "UI/UX"}'),

-- Phạm Thị Dung applications
(12, 8, 'under_review', DATEADD(day, -6, GETUTCDATE()), N'Em có nền tảng tài chính vững chắc và mong muốn áp dụng kiến thức vào lĩnh vực fintech.', N'{"gpa": 3.7, "excel_certification": true, "accounting_software": ["MISA", "SAP"]}'),
(6, 8, 'rejected', DATEADD(day, -15, GETUTCDATE()), N'Em muốn thử sức với vai trò Business Analyst để mở rộng kiến thức.', N'{"business_courses": 3, "internship_experience": "6 months"}'),

-- Hoàng Văn Em applications
(7, 9, 'interview', DATEADD(day, -7, GETUTCDATE()), N'Em có đam mê với data science và AI, với nhiều project cá nhân về machine learning.', N'{"github_url": "https://github.com/hoangvanem", "ml_projects": 8, "python_experience": "4 years"}'),
(9, 9, 'applied', DATEADD(day, -2, GETUTCDATE()), N'Em quan tâm đến game development và muốn học hỏi từ team VNG.', N'{"unity_experience": "2 years", "published_games": 2}'),

-- Vũ Thị Phương applications
(6, 10, 'under_review', DATEADD(day, -9, GETUTCDATE()), N'Em có kinh nghiệm kinh doanh và tiếng Anh tốt, mong muốn phát triển trong vai trò BA.', N'{"business_experience": "2 years", "ielts_score": 7.5, "project_management": true}'),
(11, 10, 'applied', DATEADD(day, -4, GETUTCDATE()), N'Mặc dù chuyên ngành kinh doanh nhưng em có hứng thú với công nghệ và muốn học frontend.', N'{"coding_bootcamp": "6 months", "personal_projects": 3}');
GO

-- =============================================
-- 11. BẢNG APPLICATION_STATUS_HISTORY - Lịch sử trạng thái đơn ứng tuyển
-- =============================================
-- Theo dõi quá trình thay đổi trạng thái của các đơn ứng tuyển
INSERT INTO application_status_history (application_id, status, changed_at, changed_by, notes) VALUES
-- Application 1 (Nguyễn Văn An - Position 1) - hired
(1, N'applied', DATEADD(day, -10, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(1, N'under_review', DATEADD(day, -9, GETUTCDATE()), 2, N'Bắt đầu review hồ sơ ứng viên'),
(1, N'interview', DATEADD(day, -7, GETUTCDATE()), 2, N'Mời ứng viên phỏng vấn - technical skills tốt'),
(1, N'offered', DATEADD(day, -3, GETUTCDATE()), 2, N'Đưa ra offer - ứng viên phù hợp với yêu cầu'),
(1, N'hired', DATEADD(day, -1, GETUTCDATE()), 2, N'Ứng viên chấp nhận offer và ký hợp đồng'),

-- Application 2 (Nguyễn Văn An - Position 3) - under_review
(2, N'applied', DATEADD(day, -5, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển vị trí full-time'),
(2, N'under_review', DATEADD(day, -4, GETUTCDATE()), 2, N'Đang xem xét hồ sơ cho vị trí junior developer'),

-- Application 3 (Trần Thị Bình - Position 5) - interview
(3, N'applied', DATEADD(day, -8, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(3, N'under_review', DATEADD(day, -6, GETUTCDATE()), 3, N'Review hồ sơ - kinh nghiệm marketing tốt'),
(3, N'interview', DATEADD(day, -2, GETUTCDATE()), 3, N'Lên lịch phỏng vấn - portfolio ấn tượng'),

-- Application 4 (Trần Thị Bình - Position 10) - applied
(4, N'applied', DATEADD(day, -3, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),

-- Application 5 (Lê Quang Cường - Position 2) - offered
(5, N'applied', DATEADD(day, -12, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(5, N'under_review', DATEADD(day, -11, GETUTCDATE()), 2, N'Review portfolio design'),
(5, N'interview', DATEADD(day, -8, GETUTCDATE()), 2, N'Phỏng vấn design thinking và technical skills'),
(5, N'offered', DATEADD(day, -5, GETUTCDATE()), 2, N'Offer thực tập - design skills xuất sắc'),

-- Application 6 (Phạm Thị Dung - Position 12) - under_review
(6, N'applied', DATEADD(day, -6, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(6, N'under_review', DATEADD(day, -5, GETUTCDATE()), 3, N'Đang đánh giá background tài chính'),

-- Application 7 (Phạm Thị Dung - Position 6) - rejected
(7, N'applied', DATEADD(day, -15, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(7, N'under_review', DATEADD(day, -13, GETUTCDATE()), 3, N'Review hồ sơ BA position'),
(7, N'rejected', DATEADD(day, -10, GETUTCDATE()), 3, N'Thiếu kinh nghiệm phân tích nghiệp vụ'),

-- Application 8 (Hoàng Văn Em - Position 7) - interview
(8, N'applied', DATEADD(day, -7, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(8, N'under_review', DATEADD(day, -6, GETUTCDATE()), 4, N'Review GitHub và ML projects'),
(8, N'interview', DATEADD(day, -3, GETUTCDATE()), 4, N'Technical interview về data analysis'),

-- Application 9 (Hoàng Văn Em - Position 9) - applied
(9, N'applied', DATEADD(day, -2, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),

-- Application 10 (Vũ Thị Phương - Position 6) - under_review
(10, N'applied', DATEADD(day, -9, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển'),
(10, N'under_review', DATEADD(day, -7, GETUTCDATE()), 3, N'Đánh giá English skills và business background'),

-- Application 11 (Vũ Thị Phương - Position 11) - applied
(11, N'applied', DATEADD(day, -4, GETUTCDATE()), NULL, N'Ứng viên nộp đơn ứng tuyển');
GO

-- =============================================
-- 12. BẢNG APPLICANT_NOTES - Ghi chú về ứng viên
-- =============================================
-- Ghi chú nội bộ của recruiters về các ứng viên
INSERT INTO applicant_notes (application_id, interviewer_user_id, note_text) VALUES
(1, 2, N'Ứng viên có technical skills tốt, đặc biệt là C# và React. Attitude tích cực và eager to learn.'),
(1, 2, N'Phỏng vấn technical round: giải quyết coding problem khá tốt, hiểu OOP principles.'),
(1, 2, N'Final decision: HIRE - Phù hợp với team culture và có potential phát triển.'),

(3, 3, N'Portfolio marketing rất ấn tượng, có case study cụ thể về social media campaigns.'),
(3, 3, N'Soft skills tốt, giao tiếp tự tin. Hiểu biết về digital marketing trends.'),

(5, 2, N'Design portfolio xuất sắc, có eye for detail và hiểu UX principles.'),
(5, 2, N'Figma skills rất tốt, có thể làm việc độc lập. Recommend để offer.'),

(7, 3, N'Thiếu kinh nghiệm thực tế về business analysis. Cần training nhiều hơn.'),

(8, 4, N'Python skills rất mạnh, có nhiều ML projects thực tế trên GitHub.'),
(8, 4, N'Technical interview: giải thích algorithms rõ ràng, có thể handle complex data problems.'),

(10, 3, N'English communication excellent, business mindset tốt nhưng cần đánh giá thêm về technical aptitude.');
GO

-- =============================================
-- 13. BẢNG CONVERSATIONS - Cuộc hội thoại
-- =============================================
-- Các cuộc trò chuyện giữa recruiters và candidates
INSERT INTO conversations (participant1_user_id, participant2_user_id, created_at, last_message_at) VALUES
-- Conversations between recruiters and candidates (participant1 < participant2 để tránh duplicate)
(2, 5, DATEADD(day, -8, GETUTCDATE()), DATEADD(hour, -2, GETUTCDATE())), -- TechCorp recruiter & Nguyễn Văn An
(2, 7, DATEADD(day, -6, GETUTCDATE()), DATEADD(hour, -5, GETUTCDATE())), -- TechCorp recruiter & Lê Quang Cường  
(3, 6, DATEADD(day, -5, GETUTCDATE()), DATEADD(hour, -1, GETUTCDATE())), -- FPT recruiter & Trần Thị Bình
(3, 8, DATEADD(day, -4, GETUTCDATE()), DATEADD(day, -2, GETUTCDATE())), -- FPT recruiter & Phạm Thị Dung
(4, 9, DATEADD(day, -3, GETUTCDATE()), DATEADD(hour, -3, GETUTCDATE())); -- Viettel recruiter & Hoàng Văn Em
GO

-- =============================================
-- 14. BẢNG MESSAGES - Tin nhắn
-- =============================================
-- Nội dung tin nhắn trong các cuộc hội thoại
INSERT INTO messages (conversation_id, sender_user_id, message_text, sent_at, is_read) VALUES
-- Conversation 1: TechCorp recruiter (2) & Nguyễn Văn An (5)
(1, 2, N'Chào An, cảm ơn bạn đã ứng tuyển vị trí thực tập tại TechCorp. Chúng tôi rất ấn tượng với hồ sơ của bạn.', DATEADD(day, -8, GETUTCDATE()), 1),
(1, 5, N'Chào anh, em rất vui khi nhận được tin nhắn. Em rất mong muốn được làm việc tại TechCorp ạ.', DATEADD(day, -8, GETUTCDATE()), 1),
(1, 2, N'Chúng tôi muốn mời bạn tham gia phỏng vấn vào thứ 5 tuần sau lúc 2PM. Bạn có rảnh không?', DATEADD(day, -7, GETUTCDATE()), 1),
(1, 5, N'Dạ em có thể tham gia ạ. Em sẽ chuẩn bị kỹ cho buổi phỏng vấn.', DATEADD(day, -7, GETUTCDATE()), 1),
(1, 2, N'Tuyệt vời! Chúc mừng An, chúng tôi quyết định offer vị trí thực tập cho bạn. HR sẽ liên hệ về contract sớm.', DATEADD(hour, -2, GETUTCDATE()), 1),
(1, 5, N'Cảm ơn anh rất nhiều! Em rất hạnh phúc và sẽ cố gắng hết mình ạ.', DATEADD(hour, -2, GETUTCDATE()), 0),

-- Conversation 2: TechCorp recruiter (2) & Lê Quang Cường (7) 
(2, 2, N'Chào Cường, portfolio design của bạn rất ấn tượng. Chúng tôi muốn tìm hiểu thêm về kinh nghiệm UI/UX của bạn.', DATEADD(day, -6, GETUTCDATE()), 1),
(2, 7, N'Chào anh, em rất vui khi được anh đánh giá cao. Em có 3 năm kinh nghiệm design và đặc biệt yêu thích UI/UX.', DATEADD(day, -6, GETUTCDATE()), 1),
(2, 2, N'Bạn có thể chia sẻ về process design thinking của mình không? Và tools nào bạn thường sử dụng?', DATEADD(day, -5, GETUTCDATE()), 1),
(2, 7, N'Em thường bắt đầu với user research, tạo persona, wireframe trên Figma, rồi prototype và user testing ạ.', DATEADD(day, -5, GETUTCDATE()), 1),
(2, 2, N'Rất tốt! Chúng tôi muốn offer vị trí thực tập UI/UX Designer. Bạn có thể bắt đầu từ đầu tháng 6 không?', DATEADD(hour, -5, GETUTCDATE()), 0),

-- Conversation 3: FPT recruiter (3) & Trần Thị Bình (6)
(3, 3, N'Chào Bình, chúng tôi đã review hồ sơ marketing của bạn. Rất ấn tượng với các campaign bạn đã thực hiện.', DATEADD(day, -5, GETUTCDATE()), 1),
(3, 6, N'Chào chị, em cảm ơn chị đã dành thời gian review. Em rất đam mê digital marketing và mong muốn học hỏi thêm.', DATEADD(day, -5, GETUTCDATE()), 1),
(3, 3, N'Bạn có kinh nghiệm với B2B marketing không? Vì team chúng tôi chủ yếu làm marketing cho enterprise clients.', DATEADD(day, -3, GETUTCDATE()), 1),
(3, 6, N'Em chưa có kinh nghiệm B2B nhiều, nhưng em rất eager to learn và có thể adapt nhanh ạ.', DATEADD(day, -3, GETUTCDATE()), 1),
(3, 3, N'Chúng tôi sẽ arrange phỏng vấn để tìm hiểu thêm. Thứ 3 tuần sau 10AM bạn có rảnh không?', DATEADD(hour, -1, GETUTCDATE()), 0),

-- Conversation 4: FPT recruiter (3) & Phạm Thị Dung (8)
(4, 3, N'Chào Dung, cảm ơn bạn đã apply vị trí Finance Analyst intern tại Sendo.', DATEADD(day, -4, GETUTCDATE()), 1),
(4, 8, N'Chào chị, em rất quan tâm đến fintech và mong muốn áp dụng kiến thức tài chính vào thực tế ạ.', DATEADD(day, -4, GETUTCDATE()), 1),
(4, 3, N'Background tài chính của bạn tốt, nhưng chúng tôi cần người có thể làm việc với data analysis tools. Bạn có kinh nghiệm gì không?', DATEADD(day, -2, GETUTCDATE()), 1),
(4, 8, N'Em có kinh nghiệm với Excel advanced và đang học Power BI. Em có thể học thêm các tools khác nếu cần ạ.', DATEADD(day, -2, GETUTCDATE()), 0),

-- Conversation 5: Viettel recruiter (4) & Hoàng Văn Em (9)
(5, 4, N'Chào Em, GitHub của bạn rất impressive với nhiều ML projects. Chúng tôi quan tâm đến profile của bạn.', DATEADD(day, -3, GETUTCDATE()), 1),
(5, 9, N'Chào anh, em rất vui khi được anh quan tâm. Em đam mê AI/ML và muốn áp dụng vào telecommunications.', DATEADD(day, -3, GETUTCDATE()), 1),
(5, 4, N'Tuyệt vời! Viettel đang có nhiều dự án về AI cho telecom. Bạn có thể tham gia technical interview không?', DATEADD(hour, -3, GETUTCDATE()), 1),
(5, 9, N'Dạ em sẵn sàng ạ. Em rất mong muốn được contribute vào các dự án AI của Viettel.', DATEADD(hour, -3, GETUTCDATE()), 0);
GO

-- =============================================
-- 15. BẢNG NOTIFICATIONS - Thông báo
-- =============================================
-- Các thông báo gửi đến người dùng
INSERT INTO notifications (user_id, title, message, is_read, created_at, related_entity_type, related_entity_id, notification_type) VALUES
-- Notifications for candidates about application status changes
(5, N'Đơn ứng tuyển được chấp nhận', N'Chúc mừng! Đơn ứng tuyển vị trí "Thực tập sinh Lập trình Web" tại TechCorp đã được chấp nhận.', 1, DATEADD(day, -1, GETUTCDATE()), 'application', 1, 'status_change'),
(5, N'Đơn ứng tuyển đang được xem xét', N'Đơn ứng tuyển vị trí "Junior Developer Full-time" tại TechCorp đang được xem xét.', 0, DATEADD(day, -4, GETUTCDATE()), 'application', 2, 'status_change'),

(6, N'Mời phỏng vấn', N'Bạn được mời phỏng vấn cho vị trí "Thực tập sinh Digital Marketing" tại FPT Software.', 1, DATEADD(day, -2, GETUTCDATE()), 'application', 3, 'status_change'),
(6, N'Tin nhắn mới', N'Bạn có tin nhắn mới từ nhà tuyển dụng FPT Software.', 0, DATEADD(hour, -1, GETUTCDATE()), 'conversation', 3, 'new_message'),

(7, N'Nhận được offer', N'Chúc mừng! Bạn nhận được offer cho vị trí "Thực tập sinh UI/UX Designer" tại TechCorp.', 0, DATEADD(hour, -5, GETUTCDATE()), 'application', 5, 'status_change'),

(8, N'Đơn ứng tuyển bị từ chối', N'Đơn ứng tuyển vị trí "Business Analyst Intern" tại FPT Software không được chấp nhận.', 1, DATEADD(day, -10, GETUTCDATE()), 'application', 7, 'status_change'),
(8, N'Đơn ứng tuyển đang được xem xét', N'Đơn ứng tuyển vị trí "Finance Analyst Intern" tại Sendo đang được xem xét.', 0, DATEADD(day, -5, GETUTCDATE()), 'application', 6, 'status_change'),

(9, N'Mời phỏng vấn', N'Bạn được mời phỏng vấn cho vị trí "Thực tập sinh Data Analyst" tại Viettel.', 1, DATEADD(day, -3, GETUTCDATE()), 'application', 8, 'status_change'),
(9, N'Tin nhắn mới', N'Bạn có tin nhắn mới từ nhà tuyển dụng Viettel.', 0, DATEADD(hour, -3, GETUTCDATE()), 'conversation', 5, 'new_message'),

(10, N'Đơn ứng tuyển đang được xem xét', N'Đơn ứng tuyển vị trí "Business Analyst Intern" tại FPT Software đang được xem xét.', 0, DATEADD(day, -7, GETUTCDATE()), 'application', 10, 'status_change'),

-- Notifications for recruiters about new applications
(2, N'Đơn ứng tuyển mới', N'Có đơn ứng tuyển mới cho vị trí "Junior Developer Full-time" từ Nguyễn Văn An.', 1, DATEADD(day, -5, GETUTCDATE()), 'application', 2, 'new_application'),
(2, N'Đơn ứng tuyển mới', N'Có đơn ứng tuyển mới cho vị trí "Product Marketing Intern" từ Trần Thị Bình.', 0, DATEADD(day, -3, GETUTCDATE()), 'application', 4, 'new_application'),

(3, N'Đơn ứng tuyển mới', N'Có đơn ứng tuyển mới cho vị trí "Finance Analyst Intern" từ Phạm Thị Dung.', 1, DATEADD(day, -6, GETUTCDATE()), 'application', 6, 'new_application'),
(3, N'Đơn ứng tuyển mới', N'Có đơn ứng tuyển mới cho vị trí "Frontend Developer Intern" từ Vũ Thị Phương.', 0, DATEADD(day, -4, GETUTCDATE()), 'application', 11, 'new_application'),

(4, N'Đơn ứng tuyển mới', N'Có đơn ứng tuyển mới cho vị trí "Thực tập sinh Data Analyst" từ Hoàng Văn Em.', 1, DATEADD(day, -7, GETUTCDATE()), 'application', 8, 'new_application'),

-- System notifications
(1, N'Báo cáo hệ thống', N'Hệ thống đã xử lý 50 đơn ứng tuyển trong tuần qua.', 1, DATEADD(day, -7, GETUTCDATE()), 'system', NULL, 'system_report'),
(1, N'Công ty mới', N'Có 2 công ty mới đăng ký tham gia hệ thống trong tuần qua.', 0, DATEADD(day, -3, GETUTCDATE()), 'system', NULL, 'system_report');
GO

-- =============================================
-- 16. BẢNG COMPANY_REVIEWS - Đánh giá công ty
-- =============================================
-- Đánh giá của candidates về các công ty (chỉ những người đã từng apply hoặc làm việc)
INSERT INTO company_reviews (user_id, company_id, rating, comment, created_at, is_approved) VALUES
(5, 1, 5, N'TechCorp có môi trường làm việc rất tốt, mentor nhiệt tình hướng dẫn. Được học hỏi rất nhiều kiến thức thực tế về web development.', DATEADD(day, -2, GETUTCDATE()), 1),
(6, 2, 4, N'FPT Software có quy trình training rất bài bản. Team marketing rất professional và supportive với intern.', DATEADD(day, -5, GETUTCDATE()), 1),
(7, 1, 5, N'Design team tại TechCorp rất creative và open-minded. Được tham gia nhiều project thú vị và challenging.', DATEADD(day, -3, GETUTCDATE()), 1),
(8, 2, 3, N'Công ty tốt nhưng workload hơi nặng đối với intern. Tuy nhiên học được nhiều kinh nghiệm quý báu.', DATEADD(day, -8, GETUTCDATE()), 1),
(9, 3, 4, N'Viettel có infrastructure công nghệ rất mạnh. Cơ hội tiếp cận với big data và AI projects thực tế.', DATEADD(day, -1, GETUTCDATE()), 0);
GO

-- =============================================
-- 17. BẢNG POSITION_HISTORY - Lịch sử thay đổi vị trí
-- =============================================
-- Theo dõi các thay đổi quan trọng của positions
INSERT INTO position_history (position_id, changed_by_user_id, changed_at, change_type, old_value, new_value, notes) VALUES
(1, 2, DATEADD(day, -5, GETUTCDATE()), N'extend_deadline', N'{"application_deadline": "2024-05-15"}', N'{"application_deadline": "2024-05-30"}', N'Gia hạn deadline do nhận được nhiều hồ sơ chất lượng'),
(5, 3, DATEADD(day, -3, GETUTCDATE()), N'update_description', N'{"description": "Hỗ trợ team marketing..."}', N'{"description": "Hỗ trợ team marketing trong việc quản lý social media, content creation, SEO và B2B marketing campaigns..."}', N'Cập nhật mô tả chi tiết hơn về B2B marketing'),
(7, 4, DATEADD(day, -2, GETUTCDATE()), N'update_salary', N'{"salary_range": "4-7 triệu VND"}', N'{"salary_range": "5-8 triệu VND"}', N'Tăng mức lương để thu hút ứng viên chất lượng cao'),
(12, 3, DATEADD(day, -1, GETUTCDATE()), N'extend_deadline', N'{"application_deadline": "2024-05-20"}', N'{"application_deadline": "2024-06-10"}', N'Gia hạn do cần thêm thời gian tìm ứng viên phù hợp');
GO

-- =============================================
-- 18. BẢNG WEBSOCKET_CONNECTIONS - Kết nối WebSocket (mẫu)
-- =============================================
-- Một số kết nối WebSocket đang hoạt động (thường sẽ được quản lý bởi application)
INSERT INTO websocket_connections (connection_id, user_id, connected_at, last_activity, client_info) VALUES
('conn_user5_' + CAST(NEWID() AS VARCHAR(36)), 5, DATEADD(minute, -30, GETUTCDATE()), DATEADD(minute, -5, GETUTCDATE()), N'{"ip": "192.168.1.100", "user_agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", "device": "desktop"}'),
('conn_user6_' + CAST(NEWID() AS VARCHAR(36)), 6, DATEADD(minute, -15, GETUTCDATE()), DATEADD(minute, -2, GETUTCDATE()), N'{"ip": "192.168.1.101", "user_agent": "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)", "device": "mobile"}'),
('conn_user2_' + CAST(NEWID() AS VARCHAR(36)), 2, DATEADD(minute, -45, GETUTCDATE()), DATEADD(minute, -1, GETUTCDATE()), N'{"ip": "10.0.0.50", "user_agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", "device": "desktop"}');
GO

-- =============================================
-- HOÀN THÀNH SEED DATA
-- =============================================
PRINT 'Seed data đã được tạo thành công!';
PRINT 'Tổng quan dữ liệu:';
PRINT '- 3 roles (candidate, recruiter, admin)';
PRINT '- 10 users (1 admin, 3 recruiters, 6 candidates)';
PRINT '- 5 companies với thông tin chi tiết';
PRINT '- 25 skills được phân loại';
PRINT '- 5 job categories';
PRINT '- 12 positions đa dạng';
PRINT '- 11 applications với các trạng thái khác nhau';
PRINT '- 5 conversations và 16 messages';
PRINT '- 15 notifications cho các scenarios khác nhau';
PRINT '- 5 company reviews';

USE DKyThucTap;
GO

-- Enhanced Company Recruiters Schema Update Script
-- This script adds missing columns and features for the advanced recruiter management system

PRINT '🚀 Starting enhanced company_recruiters schema update...';

-- Check if company_recruiters table exists, if not create it
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'company_recruiters')
BEGIN
    PRINT '1. Creating the company_recruiters table...';
    CREATE TABLE company_recruiters (
        user_id INT NOT NULL,
        company_id INT NOT NULL,
        role_in_company NVARCHAR(100) NULL,
        is_admin BIT DEFAULT 0,
        assigned_at DATETIMEOFFSET DEFAULT GETUTCDATE(),
        PRIMARY KEY (user_id, company_id),
        CONSTRAINT FK_company_recruiters_users FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_company_recruiters_companies FOREIGN KEY (company_id) REFERENCES companies(company_id) ON DELETE CASCADE
    );
    
    -- Populate with existing company creators
    INSERT INTO company_recruiters (user_id, company_id, role_in_company, is_admin)
    SELECT
        created_by,
        company_id,
        N'Chủ sở hữu',
        1
    FROM companies
    WHERE created_by IS NOT NULL;
END
ELSE
BEGIN
    PRINT '1. company_recruiters table already exists, proceeding with column updates...';
END
GO

-- Add missing columns for enhanced functionality
PRINT '2. Adding enhanced columns to company_recruiters table...';

-- Add is_approved column for invitation/request approval system
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'is_approved')
BEGIN
    ALTER TABLE company_recruiters ADD is_approved BIT DEFAULT 1;
    PRINT '   ✅ Added is_approved column';
END
ELSE
BEGIN
    PRINT '   ℹ️  is_approved column already exists';
END

-- Add joined_at column (separate from assigned_at for better tracking)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'joined_at')
BEGIN
    ALTER TABLE company_recruiters ADD joined_at DATETIMEOFFSET DEFAULT GETUTCDATE();
    PRINT '   ✅ Added joined_at column';
END
ELSE
BEGIN
    PRINT '   ℹ️  joined_at column already exists';
END

-- Add request_message column for join requests
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'request_message')
BEGIN
    ALTER TABLE company_recruiters ADD request_message NVARCHAR(500) NULL;
    PRINT '   ✅ Added request_message column';
END
ELSE
BEGIN
    PRINT '   ℹ️  request_message column already exists';
END

-- Add response_message column for approval/rejection responses
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'response_message')
BEGIN
    ALTER TABLE company_recruiters ADD response_message NVARCHAR(500) NULL;
    PRINT '   ✅ Added response_message column';
END
ELSE
BEGIN
    PRINT '   ℹ️  response_message column already exists';
END

-- Add invited_by column to track who invited the recruiter
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'invited_by')
BEGIN
    ALTER TABLE company_recruiters ADD invited_by INT NULL;
    PRINT '   ✅ Added invited_by column';
    
    -- Add foreign key constraint
    ALTER TABLE company_recruiters 
    ADD CONSTRAINT FK_company_recruiters_invited_by 
    FOREIGN KEY (invited_by) REFERENCES users(user_id);
    PRINT '   ✅ Added FK constraint for invited_by';
END
ELSE
BEGIN
    PRINT '   ℹ️  invited_by column already exists';
END

-- Add responded_by column to track who approved/rejected the request
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'responded_by')
BEGIN
    ALTER TABLE company_recruiters ADD responded_by INT NULL;
    PRINT '   ✅ Added responded_by column';
    
    -- Add foreign key constraint
    ALTER TABLE company_recruiters 
    ADD CONSTRAINT FK_company_recruiters_responded_by 
    FOREIGN KEY (responded_by) REFERENCES users(user_id);
    PRINT '   ✅ Added FK constraint for responded_by';
END
ELSE
BEGIN
    PRINT '   ℹ️  responded_by column already exists';
END

-- Add responded_at column to track when the response was made
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'responded_at')
BEGIN
    ALTER TABLE company_recruiters ADD responded_at DATETIMEOFFSET NULL;
    PRINT '   ✅ Added responded_at column';
END
ELSE
BEGIN
    PRINT '   ℹ️  responded_at column already exists';
END

-- Add status column for better tracking (pending, approved, rejected, left)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'status')
BEGIN
    ALTER TABLE company_recruiters ADD status NVARCHAR(20) DEFAULT 'approved';
    PRINT '   ✅ Added status column';
END
ELSE
BEGIN
    PRINT '   ℹ️  status column already exists';
END

-- Add last_activity column for performance tracking
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('company_recruiters') AND name = 'last_activity')
BEGIN
    ALTER TABLE company_recruiters ADD last_activity DATETIMEOFFSET DEFAULT GETUTCDATE();
    PRINT '   ✅ Added last_activity column';
END
ELSE
BEGIN
    PRINT '   ℹ️  last_activity column already exists';
END

GO

-- Update existing records to have proper default values
PRINT '3. Updating existing records with default values...';

-- Set is_approved = 1 for existing records (they were already approved)
UPDATE company_recruiters 
SET is_approved = 1 
WHERE is_approved IS NULL;

-- Set joined_at = assigned_at for existing records
UPDATE company_recruiters 
SET joined_at = assigned_at 
WHERE joined_at IS NULL AND assigned_at IS NOT NULL;

-- Set status = 'approved' for existing records
UPDATE company_recruiters 
SET status = 'approved' 
WHERE status IS NULL OR status = '';

-- Set last_activity = assigned_at for existing records
UPDATE company_recruiters 
SET last_activity = COALESCE(assigned_at, GETUTCDATE()) 
WHERE last_activity IS NULL;

GO

-- Create useful indexes for performance
PRINT '4. Creating indexes for better performance...';

-- Index for finding pending requests
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_company_recruiters_status_company')
BEGIN
    CREATE INDEX IX_company_recruiters_status_company 
    ON company_recruiters (status, company_id);
    PRINT '   ✅ Created index IX_company_recruiters_status_company';
END

-- Index for finding user's companies
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_company_recruiters_user_approved')
BEGIN
    CREATE INDEX IX_company_recruiters_user_approved 
    ON company_recruiters (user_id, is_approved);
    PRINT '   ✅ Created index IX_company_recruiters_user_approved';
END

-- Index for activity tracking
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_company_recruiters_last_activity')
BEGIN
    CREATE INDEX IX_company_recruiters_last_activity 
    ON company_recruiters (last_activity DESC);
    PRINT '   ✅ Created index IX_company_recruiters_last_activity';
END

GO

-- Create a view for easy querying of recruiter statistics
PRINT '5. Creating useful views...';

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_company_recruiter_stats')
BEGIN
    DROP VIEW vw_company_recruiter_stats;
END



PRINT '✅ Enhanced company_recruiters schema update completed successfully!';
PRINT '';
PRINT '📊 Summary of changes:';
PRINT '   - Enhanced company_recruiters table with invitation/approval system';
PRINT '   - Added columns: is_approved, joined_at, request_message, response_message';
PRINT '   - Added columns: invited_by, responded_by, responded_at, status, last_activity';
PRINT '   - Created performance indexes';
PRINT '   - Created vw_company_recruiter_stats view for easy querying';
PRINT '';
PRINT '🎯 The system now supports:';
PRINT '   ✅ Recruiter invitation system';
PRINT '   ✅ Company join requests';
PRINT '   ✅ Approval/rejection workflow';
PRINT '   ✅ Advanced statistics and reporting';
PRINT '   ✅ Performance tracking';


go
CREATE VIEW vw_company_recruiter_stats AS
SELECT 
    cr.company_id,
    c.name as company_name,
    cr.user_id,
    COALESCE(up.first_name + ' ' + up.last_name, u.email) as user_name,
    u.email as user_email,
    cr.role_in_company,
    cr.is_admin,
    cr.is_approved,
    cr.status,
    cr.joined_at,
    cr.last_activity,
    COUNT(p.position_id) as position_count,
    COUNT(CASE WHEN p.is_active = 1 THEN 1 END) as active_position_count,
    COUNT(a.application_id) as total_applications
FROM company_recruiters cr
INNER JOIN companies c ON cr.company_id = c.company_id
INNER JOIN users u ON cr.user_id = u.user_id
LEFT JOIN user_profiles up ON u.user_id = up.user_id
LEFT JOIN positions p ON p.company_id = cr.company_id AND p.created_by = cr.user_id
LEFT JOIN applications a ON a.position_id = p.position_id
GROUP BY 
    cr.company_id, c.name, cr.user_id, up.first_name, up.last_name, u.email,
    cr.role_in_company, cr.is_admin, cr.is_approved, cr.status, cr.joined_at, cr.last_activity;

GO