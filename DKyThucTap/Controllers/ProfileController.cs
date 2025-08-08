using System.Linq;
using System.Threading.Tasks;
using DKyThucTap.Data;
using DKyThucTap.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DKyThucTap.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class ProfileController : Controller
    {
        private readonly DKyThucTapContext _context;
        public ProfileController(DKyThucTapContext context)
        {
            _context = context;
        }
        public IActionResult MyClaims()
        {
            return Json(User.Claims.Select(c => new { c.Type, c.Value }));
        }

        [Authorize(Policy = "CandidateOrAdmin")]
        [HttpGet("Profile/Recruiter/{recruiterId}")]
        public async Task<IActionResult> Recruiter(int recruiterId)
        {
            var recruiter = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Role)
                .Include(u => u.Companies)
                .FirstOrDefaultAsync(u => u.UserId == recruiterId && u.RoleId == 2);

            if (recruiter == null)
                return NotFound();

            // lấy danh sách vị trí đăng tuyển
            var positions = await _context.Positions
                .Include(p => p.Company)
                .Where(p => p.CreatedBy == recruiter.UserId)
                .ToListAsync();

            var vm = new RecruiterProfileViewModel
            {
                RecruiterId = recruiter.UserId,
                FullName = $"{recruiter.UserProfile?.FirstName} {recruiter.UserProfile?.LastName}",
                Email = recruiter.Email,
                Phone = recruiter.UserProfile?.Phone,
                ProfilePictureUrl = recruiter.UserProfile?.ProfilePictureUrl,
                Bio = recruiter.UserProfile?.Bio,
                Companies = recruiter.Companies
                    .Select(c => new RecruiterCompanyDto
                    {
                        CompanyId = c.CompanyId,
                        Name = c.Name
                    }).ToList(),
                PostedPositions = positions.Select(p => new RecruiterPositionDto
                {
                    Title = p.Title,
                    CompanyName = p.Company?.Name ?? "Chưa rõ",
                    IsActive = p.IsActive == true,
                    CreatedAt = p.CreatedAt ?? DateTimeOffset.MinValue
                }).ToList()
            };

            return View(vm);
        }

        // 🎯 Ứng viên (role_id = 1) hoặc Admin (role_id = 3) xem Employer
        //[Authorize(Policy = "CandidateOrAdmin")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Employer(int companyId)
        {
            var company = await _context.Companies
                .Include(c => c.Positions)
                .Include(c => c.CompanyReviews)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company == null)
                return NotFound();

            var avgRating = company.CompanyReviews
                .Where(r => r.IsApproved == true)
                .Select(r => (double?)r.Rating)
                .Average() ?? 0;

            var vm = new EmployerProfileViewModel
            {
                CompanyName = company.Name,
                Description = company.Description,
                Website = company.Website,
                Industry = company.Industry,
                Location = company.Location,
                LogoUrl = company.LogoUrl,   // ✅ map từ DB
                AverageRating = avgRating,
                ActivePositions = company.Positions
                    .Where(p => p.IsActive == true)
                    .Select(p => p.Title)
                    .ToList(),
                            Reviews = company.CompanyReviews
                    .Where(r => r.IsApproved == true)
                    .Select(r => r.Comment)
                    .ToList()
            };


            return View(vm);
        }

        // 🎯 Nhà tuyển dụng (role_id = 2) hoặc Admin (role_id = 3) xem Candidate
        [Authorize(Policy = "RecruiterOrAdmin")]
        [HttpGet]
        public async Task<IActionResult> Candidate(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.UserSkills).ThenInclude(us => us.Skill)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role.RoleName == "candidate");

            if (user == null || user.UserProfile == null)
                return NotFound();

            var vm = new CandidateProfileViewModel
            {
                FullName = $"{user.UserProfile.FirstName} {user.UserProfile.LastName}",
                Email = user.Email,
                Phone = user.UserProfile.Phone,
                CvUrl = user.UserProfile.CvUrl,
                ProfilePictureUrl = user.UserProfile.ProfilePictureUrl,
                Bio = user.UserProfile.Bio,
                Skills = user.UserSkills
                    .Select(us => $"{us.Skill.Name} (Level {us.ProficiencyLevel})")
                    .ToList()
            };

            return View(vm);
        }
    }
}
