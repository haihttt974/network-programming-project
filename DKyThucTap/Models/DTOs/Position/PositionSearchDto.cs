namespace DKyThucTap.Models.DTOs.Position
{
    public class PositionSearchDto
    {
        public string? SearchTerm { get; set; }
        public int? CompanyId { get; set; }
        public int? CategoryId { get; set; }
        public string? PositionType { get; set; }
        public string? Location { get; set; }
        public bool? IsRemote { get; set; }
        public bool? IsActive { get; set; }
        public List<int> SkillIds { get; set; } = new List<int>();
        public DateOnly? DeadlineFrom { get; set; }
        public DateOnly? DeadlineTo { get; set; }
        public DateTimeOffset? CreatedFrom { get; set; }
        public DateTimeOffset? CreatedTo { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        
        // Sorting
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }

    public class PositionSearchResultDto
    {
        public List<PositionListDto> Positions { get; set; } = new List<PositionListDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}
