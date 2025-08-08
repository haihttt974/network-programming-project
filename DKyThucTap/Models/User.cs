using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ApplicantNote> ApplicantNotes { get; set; } = new List<ApplicantNote>();

    public virtual ICollection<ApplicationStatusHistory> ApplicationStatusHistories { get; set; } = new List<ApplicationStatusHistory>();

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<CompanyReview> CompanyReviews { get; set; } = new List<CompanyReview>();

    public virtual ICollection<Conversation> ConversationParticipant1Users { get; set; } = new List<Conversation>();

    public virtual ICollection<Conversation> ConversationParticipant2Users { get; set; } = new List<Conversation>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PositionHistory> PositionHistories { get; set; } = new List<PositionHistory>();

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual Role Role { get; set; } = null!;

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

    public virtual ICollection<WebsocketConnection> WebsocketConnections { get; set; } = new List<WebsocketConnection>();

    public virtual ICollection<CompanyRecruiter> CompanyRecruiters { get; set; } = new List<CompanyRecruiter>();
}
