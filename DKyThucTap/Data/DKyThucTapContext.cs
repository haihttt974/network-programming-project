using System;
using System.Collections.Generic;
using DKyThucTap.Models;
using Microsoft.EntityFrameworkCore;

namespace DKyThucTap.Data;

public partial class DKyThucTapContext : DbContext
{
    public DKyThucTapContext()
    {
    }

    public DKyThucTapContext(DbContextOptions<DKyThucTapContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApplicantNote> ApplicantNotes { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<CompanyReview> CompanyReviews { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<JobCategory> JobCategories { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<PositionHistory> PositionHistories { get; set; }

    public virtual DbSet<PositionSkill> PositionSkills { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserSkill> UserSkills { get; set; }

    public virtual DbSet<WebsocketConnection> WebsocketConnections { get; set; }

    public virtual DbSet<CompanyRecruiter> CompanyRecruiters { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicantNote>(entity =>
        {
            entity.HasKey(e => e.NoteId).HasName("PK__applican__CEDD0FA4004226EA");

            entity.ToTable("applicant_notes");

            entity.HasIndex(e => e.ApplicationId, "idx_applicant_notes_application");

            entity.HasIndex(e => e.InterviewerUserId, "idx_applicant_notes_interviewer");

            entity.Property(e => e.NoteId).HasColumnName("note_id");
            entity.Property(e => e.ApplicationId).HasColumnName("application_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.InterviewerUserId).HasColumnName("interviewer_user_id");
            entity.Property(e => e.NoteText).HasColumnName("note_text");

            entity.HasOne(d => d.Application).WithMany(p => p.ApplicantNotes)
                .HasForeignKey(d => d.ApplicationId)
                .HasConstraintName("FK__applicant__appli__76969D2E");

            entity.HasOne(d => d.InterviewerUser).WithMany(p => p.ApplicantNotes)
                .HasForeignKey(d => d.InterviewerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__applicant__inter__778AC167");
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__applicat__3BCBDCF283CC3597");

            entity.ToTable("applications");

            entity.HasIndex(e => new { e.PositionId, e.UserId }, "UQ__applicat__723B04D5507C8D2D").IsUnique();

            entity.HasIndex(e => e.PositionId, "idx_applications_position");

            entity.HasIndex(e => e.CurrentStatus, "idx_applications_status");

            entity.HasIndex(e => e.UserId, "idx_applications_user");

            entity.Property(e => e.ApplicationId).HasColumnName("application_id");
            entity.Property(e => e.AdditionalInfo).HasColumnName("additional_info");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("applied_at");
            entity.Property(e => e.CoverLetter).HasColumnName("cover_letter");
            entity.Property(e => e.CurrentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("applied")
                .HasColumnName("current_status");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Position).WithMany(p => p.Applications)
                .HasForeignKey(d => d.PositionId)
                .HasConstraintName("FK__applicati__posit__60A75C0F");

            entity.HasOne(d => d.User).WithMany(p => p.Applications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__applicati__user___619B8048");
        });

        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__applicat__096AA2E9414EB0C5");

            entity.ToTable("application_status_history");

            entity.HasIndex(e => e.ApplicationId, "idx_status_history_app");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ApplicationId).HasColumnName("application_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Application).WithMany(p => p.ApplicationStatusHistories)
                .HasForeignKey(d => d.ApplicationId)
                .HasConstraintName("FK__applicati__appli__66603565");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.ApplicationStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK__applicati__chang__68487DD7");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId).HasName("PK__companie__3E2672356AA5F854");

            entity.ToTable("companies");

            entity.HasIndex(e => e.Name, "idx_companies_name");

            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Industry)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("industry");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("location");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Website)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("website");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Companies)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__companies__creat__45F365D3");
        });

        modelBuilder.Entity<CompanyReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__company___60883D90A6CEF315");

            entity.ToTable("company_reviews");

            entity.HasIndex(e => e.CompanyId, "idx_company_reviews_company");

            entity.HasIndex(e => e.UserId, "idx_company_reviews_user");

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(false)
                .HasColumnName("is_approved");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Company).WithMany(p => p.CompanyReviews)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK__company_r__compa__70DDC3D8");

            entity.HasOne(d => d.User).WithMany(p => p.CompanyReviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__company_r__user___6FE99F9F");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__conversa__311E7E9A6663646F");

            entity.ToTable("conversations");

            entity.HasIndex(e => new { e.Participant1UserId, e.Participant2UserId }, "UQ__conversa__9921FA51A25F5994").IsUnique();

            entity.HasIndex(e => e.Participant1UserId, "idx_conversations_p1");

            entity.HasIndex(e => e.Participant2UserId, "idx_conversations_p2");

            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.LastMessageAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("last_message_at");
            entity.Property(e => e.Participant1UserId).HasColumnName("participant1_user_id");
            entity.Property(e => e.Participant2UserId).HasColumnName("participant2_user_id");

            entity.HasOne(d => d.Participant1User).WithMany(p => p.ConversationParticipant1Users)
                .HasForeignKey(d => d.Participant1UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__conversat__parti__7C4F7684");

            entity.HasOne(d => d.Participant2User).WithMany(p => p.ConversationParticipant2Users)
                .HasForeignKey(d => d.Participant2UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__conversat__parti__7D439ABD");
        });

        modelBuilder.Entity<JobCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__job_cate__D54EE9B409345593");

            entity.ToTable("job_categories");

            entity.HasIndex(e => e.CategoryName, "UQ__job_cate__5189E25556B148BE").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("category_name");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__messages__0BBF6EE692D1089A");

            entity.ToTable("messages");

            entity.HasIndex(e => e.ConversationId, "idx_messages_conversation");

            entity.HasIndex(e => e.SenderUserId, "idx_messages_sender");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.SenderUserId).HasColumnName("sender_user_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("sent_at");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK__messages__conver__02084FDA");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__messages__sender__02FC7413");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__notifica__E059842FE686EEB9");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.IsRead, "idx_notifications_read");

            entity.HasIndex(e => e.UserId, "idx_notifications_user");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("notification_type");
            entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
            entity.Property(e => e.RelatedEntityType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("related_entity_type");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__notificat__user___07C12930");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__position__99A0E7A4833CC399");

            entity.ToTable("positions");

            entity.HasIndex(e => e.IsActive, "idx_positions_active");

            entity.HasIndex(e => e.CategoryId, "idx_positions_category");

            entity.HasIndex(e => e.CompanyId, "idx_positions_company");

            entity.HasIndex(e => e.PositionType, "idx_positions_type");

            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.ApplicationDeadline).HasColumnName("application_deadline");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsRemote)
                .HasDefaultValue(false)
                .HasColumnName("is_remote");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("location");
            entity.Property(e => e.PositionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("position_type");
            entity.Property(e => e.SalaryRange)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("salary_range");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");

            entity.HasOne(d => d.Category).WithMany(p => p.Positions)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__positions__categ__534D60F1");

            entity.HasOne(d => d.Company).WithMany(p => p.Positions)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK__positions__compa__4E88ABD4");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Positions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__positions__creat__52593CB8");
        });

        modelBuilder.Entity<PositionHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__position__096AA2E90595BBAD");

            entity.ToTable("position_history");

            entity.HasIndex(e => e.PositionId, "idx_position_history_position");

            entity.HasIndex(e => e.ChangedByUserId, "idx_position_history_user");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ChangeType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("change_type");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedByUserId).HasColumnName("changed_by_user_id");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.PositionId).HasColumnName("position_id");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.PositionHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK__position___chang__6C190EBB");

            entity.HasOne(d => d.Position).WithMany(p => p.PositionHistories)
                .HasForeignKey(d => d.PositionId)
                .HasConstraintName("FK__position___posit__6B24EA82");
        });

        modelBuilder.Entity<PositionSkill>(entity =>
        {
            entity.HasKey(e => new { e.PositionId, e.SkillId }).HasName("PK__position__161B4F93C6D5194C");

            entity.ToTable("position_skills");

            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(true)
                .HasColumnName("is_required");

            entity.HasOne(d => d.Position).WithMany(p => p.PositionSkills)
                .HasForeignKey(d => d.PositionId)
                .HasConstraintName("FK__position___posit__5629CD9C");

            entity.HasOne(d => d.Skill).WithMany(p => p.PositionSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("FK__position___skill__571DF1D5");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__roles__760965CC63F556A9");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "UQ__roles__783254B1CF58DF03").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Permissions).HasColumnName("permissions");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("PK__skills__FBBA837931BE3606");

            entity.ToTable("skills");

            entity.HasIndex(e => e.Name, "UQ__skills__72E12F1B54A68378").IsUnique();

            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("category");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F8D686768");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E61645EE4A0D7").IsUnique();

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.RoleId, "idx_users_role");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__users__role_id__3B75D760");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__user_pro__AEBB701F4C75715A");

            entity.ToTable("user_profiles");

            entity.HasIndex(e => e.UserId, "UQ__user_pro__B9BE370E5EAA0AD3").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CvUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("cv_url");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.ProfilePictureUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("profile_picture_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .HasConstraintName("FK__user_prof__user___412EB0B6");
        });

        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SkillId }).HasName("PK__user_ski__36059F38D9260169");

            entity.ToTable("user_skills");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.ProficiencyLevel).HasColumnName("proficiency_level");

            entity.HasOne(d => d.Skill).WithMany(p => p.UserSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("FK__user_skil__skill__5BE2A6F2");

            entity.HasOne(d => d.User).WithMany(p => p.UserSkills)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__user_skil__user___5AEE82B9");
        });

        modelBuilder.Entity<WebsocketConnection>(entity =>
        {
            entity.HasKey(e => e.ConnectionId).HasName("PK__websocke__E4AA4DD099707A48");

            entity.ToTable("websocket_connections");

            entity.HasIndex(e => e.UserId, "idx_websocket_user");

            entity.Property(e => e.ConnectionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("connection_id");
            entity.Property(e => e.ClientInfo).HasColumnName("client_info");
            entity.Property(e => e.ConnectedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("connected_at");
            entity.Property(e => e.LastActivity)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("last_activity");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.WebsocketConnections)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__websocket__user___0C85DE4D");
        });

        modelBuilder.Entity<CompanyRecruiter>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.CompanyId });

            entity.ToTable("company_recruiters");

            // Column mappings
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.RoleInCompany)
                .HasMaxLength(100)
                .HasColumnName("role_in_company");
            entity.Property(e => e.IsAdmin)
                .HasDefaultValue(false)
                .HasColumnName("is_admin");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("assigned_at");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(true)
                .HasColumnName("is_approved");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("joined_at");
            entity.Property(e => e.RequestMessage)
                .HasMaxLength(500)
                .HasColumnName("request_message");
            entity.Property(e => e.ResponseMessage)
                .HasMaxLength(500)
                .HasColumnName("response_message");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.RespondedBy).HasColumnName("responded_by");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("approved")
                .HasColumnName("status");
            entity.Property(e => e.LastActivity)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("last_activity");

            // Relationships
            entity.HasOne(d => d.User)
                  .WithMany(p => p.CompanyRecruiters)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_company_recruiters_users");

            entity.HasOne(d => d.Company)
                  .WithMany(p => p.CompanyRecruiters)
                  .HasForeignKey(d => d.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_company_recruiters_companies");

            entity.HasOne(d => d.InvitedByNavigation)
                  .WithMany()
                  .HasForeignKey(d => d.InvitedBy)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_company_recruiters_invited_by");

            entity.HasOne(d => d.RespondedByNavigation)
                  .WithMany()
                  .HasForeignKey(d => d.RespondedBy)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_company_recruiters_responded_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
