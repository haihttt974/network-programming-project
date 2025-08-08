using System;
using System.Collections.Generic;

namespace DKyThucTap.Models;

public partial class ApplicantNote
{
    public int NoteId { get; set; }

    public int ApplicationId { get; set; }

    public int InterviewerUserId { get; set; }

    public string NoteText { get; set; } = null!;

    public DateTimeOffset? CreatedAt { get; set; }

    public virtual Application Application { get; set; } = null!;

    public virtual User InterviewerUser { get; set; } = null!;
}
