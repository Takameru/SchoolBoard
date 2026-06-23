using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolBoard.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SchoolBoard.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId && s.CanEdit);

            ViewBag.LinkedStudent = student;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewBag.CanApply = user?.CanApply ?? false;

            return View();
        }
    }
}