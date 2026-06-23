using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolBoard.Data;
using SchoolBoard.Models;
using SchoolBoard.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Ganss.Xss;

namespace SchoolBoard.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HtmlSanitizer _htmlSanitizer;
        private readonly string _uploadPath;
        private readonly string _uploadTempPath;

        public StudentsController(ApplicationDbContext context, HtmlSanitizer htmlSanitizer)
        {
            _context = context;
            _htmlSanitizer = htmlSanitizer;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");
            _uploadTempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "temp");
        }

        // GET: /Students/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.Photos)
                .Include(s => s.StudentCategories).ThenInclude(sc => sc.Category)
                .Include(s => s.Likes)
                .FirstOrDefaultAsync(s => s.Id == id && s.Status == StudentStatus.Active);

            if (student == null)
                return NotFound();

            bool isLiked = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                isLiked = student.Likes.Any(l => l.UserId == userId);
            }
            ViewData["IsLiked"] = isLiked;
            ViewData["LikesCount"] = student.Likes.Count;

            return View(student);
        }

        // POST: /Students/Like/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int id)
        {
            var student = await _context.Students
                .Include(s => s.Likes)
                .FirstOrDefaultAsync(s => s.Id == id && s.Status == StudentStatus.Active);
            if (student == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (student.Likes.Any(l => l.UserId == userId))
                return BadRequest("Вы уже поставили лайк.");

            var like = new Like
            {
                StudentId = student.Id,
                UserId = userId,
                CreatedDate = DateTime.UtcNow
            };
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = student.Id });
        }

        // POST: /Students/Unlike/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(int id)
        {
            var student = await _context.Students
                .Include(s => s.Likes)
                .FirstOrDefaultAsync(s => s.Id == id && s.Status == StudentStatus.Active);
            if (student == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var like = student.Likes.FirstOrDefault(l => l.UserId == userId);
            if (like == null)
                return BadRequest("Вы не ставили лайк.");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = student.Id });
        }

        // GET: /Students/EditMyCard
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditMyCard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students
                .Include(s => s.StudentCategories)
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId && s.CanEdit);

            if (student == null)
                return NotFound("Ваша анкета не привязана или у вас нет прав на редактирование.");

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(student);
        }

        // POST: /Students/EditMyCard
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyCard(Student model, List<int>? categoryIds, IFormFile? newPhoto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students
                .Include(s => s.StudentCategories)
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId && s.CanEdit);

            if (student == null)
                return NotFound();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            // Санитизация биографии
            string safeBio = string.IsNullOrEmpty(model.Biography)
                ? ""
                : _htmlSanitizer.Sanitize(model.Biography);

            string oldData = JsonSerializer.Serialize(new
            {
                student.LastName,
                student.FirstName,
                student.MiddleName,
                student.Class,
                student.Quote,
                student.Achievements,
                student.Biography
            });

            string newData = JsonSerializer.Serialize(new
            {
                model.LastName,
                model.FirstName,
                model.MiddleName,
                model.Class,
                model.Quote,
                model.Achievements,
                Biography = safeBio
            });

            string? tempPhotoName = null;
            if (newPhoto != null && newPhoto.Length > 0)
            {
                if (!FileValidationService.IsValidImage(newPhoto))
                {
                    ModelState.AddModelError("", "Файл не является допустимым изображением.");
                    return View(model);
                }

                if (newPhoto.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Фото слишком большое (макс. 2 МБ).");
                    return View(model);
                }

                var ext = Path.GetExtension(newPhoto.FileName).ToLowerInvariant();
                Directory.CreateDirectory(_uploadTempPath);
                tempPhotoName = $"{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(_uploadTempPath, tempPhotoName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newPhoto.CopyToAsync(stream);
                }
            }

            var change = new ProposedChange
            {
                StudentId = student.Id,
                ProposedByUserId = userId!,
                ProposedDate = DateTime.UtcNow,
                Status = ChangeStatus.Pending,
                OldDataJson = oldData,
                NewDataJson = newData,
                OldPhotosJson = JsonSerializer.Serialize(student.Photos.Select(p => p.FileName)),
                NewPhotosJson = tempPhotoName != null ? JsonSerializer.Serialize(new List<string> { tempPhotoName }) : null
            };

            _context.ProposedChanges.Add(change);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Изменения отправлены на модерацию.";
            return RedirectToAction("Index", "Profile");
        }

        // GET: /Students/CreateMyCard
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateMyCard()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(new Student());
        }

        // POST: /Students/CreateMyCard
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMyCard(Student model, List<int>? categoryIds, IFormFile? newPhoto)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            if (ModelState.ContainsKey("Photos"))
                ModelState.Remove("Photos");

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!string.IsNullOrEmpty(model.Biography))
                model.Biography = _htmlSanitizer.Sanitize(model.Biography);

            model.IdentityUserId = userId;
            model.Status = StudentStatus.Pending;
            model.CreatedDate = DateTime.UtcNow;

            _context.Students.Add(model);
            await _context.SaveChangesAsync();

            if (categoryIds != null && categoryIds.Any())
            {
                foreach (var catId in categoryIds)
                {
                    var category = await _context.Categories.FindAsync(catId);
                    if (category != null)
                    {
                        _context.StudentCategories.Add(new StudentCategory { StudentId = model.Id, CategoryId = catId });
                    }
                }
                await _context.SaveChangesAsync();
            }

            if (newPhoto != null && newPhoto.Length > 0)
            {
                if (!FileValidationService.IsValidImage(newPhoto))
                {
                    ModelState.AddModelError("", "Файл не является допустимым изображением.");
                    return View(model);
                }

                if (newPhoto.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Файл слишком большой (макс. 2 МБ).");
                    return View(model);
                }

                var ext = Path.GetExtension(newPhoto.FileName).ToLowerInvariant();
                string uniqueName = $"{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(_uploadPath, uniqueName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newPhoto.CopyToAsync(stream);
                }

                _context.StudentPhotos.Add(new StudentPhoto
                {
                    StudentId = model.Id,
                    FileName = uniqueName,
                    IsMain = true,
                    UploadOrder = 1
                });
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Заявка отправлена на модерацию.";
            return RedirectToAction("Index", "Profile");
        }
    }
}