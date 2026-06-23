using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolBoard.Data;
using SchoolBoard.Models;
using SchoolBoard.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ganss.Xss;
using Microsoft.Extensions.Logging;

namespace SchoolBoard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly HtmlSanitizer _htmlSanitizer;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _uploadPath;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, HtmlSanitizer htmlSanitizer, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _htmlSanitizer = htmlSanitizer;
            _userManager = userManager;
            _roleManager = roleManager;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");
        }

        // ==================== КАТЕГОРИИ ====================

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.StudentCategories)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = model.Name;
            category.Color = model.Color;
            category.Icon = model.Icon;
            category.SortOrder = model.SortOrder;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            bool hasStudents = await _context.StudentCategories.AnyAsync(sc => sc.CategoryId == id);
            if (hasStudents)
            {
                TempData["ErrorMessage"] = $"Нельзя удалить категорию «{category.Name}», так как к ней привязаны ученики.";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        // ==================== УЧЕНИКИ ====================

        [HttpGet]
        public async Task<IActionResult> AddStudent()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(new Student());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(Student model, IFormFile? photo, List<int>? categoryIds)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            if (ModelState.ContainsKey("Photos"))
                ModelState.Remove("Photos");

            if (!ModelState.IsValid)
                return View(model);

            if (!string.IsNullOrEmpty(model.Biography))
                model.Biography = _htmlSanitizer.Sanitize(model.Biography);

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

            if (photo != null && photo.Length > 0)
            {
                if (!FileValidationService.IsValidImage(photo))
                {
                    ModelState.AddModelError("", "Файл не является допустимым изображением.");
                    return View(model);
                }

                if (photo.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Файл слишком большой (макс. 2 МБ).");
                    return View(model);
                }

                var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
                string uniqueName = $"{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(_uploadPath, uniqueName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
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

            TempData["Message"] = "Ученик добавлен в ожидании модерации.";
            return RedirectToAction(nameof(PendingStudents));
        }

        public async Task<IActionResult> PendingStudents()
        {
            var pending = await _context.Students
                .Include(s => s.StudentCategories).ThenInclude(sc => sc.Category)
                .Where(s => s.Status == StudentStatus.Pending)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.StudentCategories)
                .FirstOrDefaultAsync(s => s.Id == id && s.Status == StudentStatus.Pending);
            if (student == null)
                return NotFound();

            student.Status = StudentStatus.Active;
            if (student.PreferredCategoryId == null)
            {
                if (student.StudentCategories.Any())
                {
                    var rnd = new Random();
                    int index = rnd.Next(student.StudentCategories.Count);
                    student.DisplayCategoryId = student.StudentCategories.ElementAt(index).CategoryId;
                }
            }
            else
            {
                student.DisplayCategoryId = student.PreferredCategoryId;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PendingStudents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.Id == id && s.Status == StudentStatus.Pending);
            if (student == null)
                return NotFound();

            foreach (var photo in student.Photos)
            {
                string filePath = Path.Combine(_uploadPath, photo.FileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PendingStudents));
        }

        public async Task<IActionResult> AllStudents(string? searchString, string? sortOrder)
        {
            ViewData["SearchString"] = searchString;
            ViewData["SortOrder"] = sortOrder;

            var studentsQuery = _context.Students
                .Include(s => s.StudentCategories).ThenInclude(sc => sc.Category)
                .Include(s => s.Photos)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string search = searchString.Trim().ToLower();
                studentsQuery = studentsQuery.Where(s =>
                    s.LastName.ToLower().Contains(search) ||
                    s.FirstName.ToLower().Contains(search) ||
                    (s.MiddleName != null && s.MiddleName.ToLower().Contains(search)));
            }

            studentsQuery = sortOrder switch
            {
                "name_asc" => studentsQuery.OrderBy(s => s.LastName).ThenBy(s => s.FirstName),
                "name_desc" => studentsQuery.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName),
                "oldest" => studentsQuery.OrderBy(s => s.CreatedDate),
                _ => studentsQuery.OrderByDescending(s => s.CreatedDate)
            };

            var students = await studentsQuery.ToListAsync();
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.Photos)
                .Include(s => s.StudentCategories)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (student == null)
                return NotFound();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Users = await _userManager.Users.ToListAsync();
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(int id, Student model, List<int>? categoryIds, int? preferredCategoryId, string? identityUserId, bool canEdit, string? specialStatus, bool showSpecialStatus, bool showNewBadge)
        {
            var student = await _context.Students
                .Include(s => s.Photos)
                .Include(s => s.StudentCategories)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (student == null)
                return NotFound();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Users = await _userManager.Users.ToListAsync();

            student.LastName = model.LastName;
            student.FirstName = model.FirstName;
            student.MiddleName = model.MiddleName;
            student.Class = model.Class;
            student.Quote = model.Quote;
            student.Achievements = model.Achievements;
            student.Biography = _htmlSanitizer.Sanitize(model.Biography ?? "");
            student.SpecialStatus = specialStatus;
            student.ShowSpecialStatus = showSpecialStatus;
            student.ShowNewBadge = showNewBadge;

            _context.StudentCategories.RemoveRange(student.StudentCategories);
            if (categoryIds != null && categoryIds.Any())
            {
                foreach (var catId in categoryIds)
                {
                    _context.StudentCategories.Add(new StudentCategory { StudentId = student.Id, CategoryId = catId });
                }
            }

            student.PreferredCategoryId = preferredCategoryId;
            if (student.PreferredCategoryId != null)
            {
                student.DisplayCategoryId = student.PreferredCategoryId;
            }
            else
            {
                var studentCats = await _context.StudentCategories
                    .Where(sc => sc.StudentId == student.Id)
                    .Select(sc => sc.CategoryId)
                    .ToListAsync();
                if (studentCats.Any())
                {
                    var rnd = new Random();
                    student.DisplayCategoryId = studentCats[rnd.Next(studentCats.Count)];
                }
                else
                {
                    student.DisplayCategoryId = null;
                }
            }

            student.IdentityUserId = identityUserId;
            student.CanEdit = canEdit;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Изменения сохранены.";
            return RedirectToAction(nameof(AllStudents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (student == null)
                return NotFound();

            foreach (var photo in student.Photos)
            {
                string filePath = Path.Combine(_uploadPath, photo.FileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AllStudents));
        }

        // ==================== МОДЕРАЦИЯ ИЗМЕНЕНИЙ ====================

        public async Task<IActionResult> ProposedChanges()
        {
            var changes = await _context.ProposedChanges
                .Include(pc => pc.Student)
                .Include(pc => pc.ProposedByUser)
                .OrderByDescending(pc => pc.ProposedDate)
                .ToListAsync();
            return View(changes);
        }

        [HttpGet]
        public async Task<IActionResult> ReviewProposedChange(int id)
        {
            var change = await _context.ProposedChanges
                .Include(pc => pc.Student).ThenInclude(s => s.Photos)
                .Include(pc => pc.ProposedByUser)
                .FirstOrDefaultAsync(pc => pc.Id == id);
            if (change == null) return NotFound();

            ViewBag.OldData = JsonSerializer.Deserialize<Dictionary<string, object>>(change.OldDataJson);
            ViewBag.NewData = JsonSerializer.Deserialize<Dictionary<string, object>>(change.NewDataJson);
            ViewBag.OldPhotos = string.IsNullOrEmpty(change.OldPhotosJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(change.OldPhotosJson);
            ViewBag.NewPhotos = string.IsNullOrEmpty(change.NewPhotosJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(change.NewPhotosJson);

            return View(change);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveChange(int id)
        {
            var change = await _context.ProposedChanges
                .Include(pc => pc.Student).ThenInclude(s => s.Photos)
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.Status == ChangeStatus.Pending);
            if (change == null) return NotFound();

            var student = change.Student;
            if (student == null) return NotFound();

            var newData = JsonSerializer.Deserialize<Dictionary<string, string>>(change.NewDataJson);
            if (newData != null)
            {
                if (newData.ContainsKey("LastName")) student.LastName = newData["LastName"];
                if (newData.ContainsKey("FirstName")) student.FirstName = newData["FirstName"];
                if (newData.ContainsKey("MiddleName")) student.MiddleName = newData["MiddleName"];
                if (newData.ContainsKey("Class")) student.Class = newData["Class"];
                if (newData.ContainsKey("Quote")) student.Quote = newData["Quote"];
                if (newData.ContainsKey("Achievements")) student.Achievements = newData["Achievements"];
                if (newData.ContainsKey("Biography")) student.Biography = newData["Biography"];
            }

            if (!string.IsNullOrEmpty(change.NewPhotosJson))
            {
                var newPhotos = JsonSerializer.Deserialize<List<string>>(change.NewPhotosJson);
                if (newPhotos != null && newPhotos.Any())
                {
                    foreach (var oldPhoto in student.Photos)
                    {
                        string oldPath = Path.Combine(_uploadPath, oldPhoto.FileName);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    _context.StudentPhotos.RemoveRange(student.Photos);

                    var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "temp");
                    string src = Path.Combine(tempPath, newPhotos[0]);
                    string dst = Path.Combine(_uploadPath, newPhotos[0]);
                    if (System.IO.File.Exists(src))
                        System.IO.File.Move(src, dst);

                    _context.StudentPhotos.Add(new StudentPhoto
                    {
                        StudentId = student.Id,
                        FileName = newPhotos[0],
                        IsMain = true,
                        UploadOrder = 1
                    });
                }
            }

            change.Status = ChangeStatus.Approved;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Изменения приняты.";
            return RedirectToAction(nameof(ProposedChanges));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectChange(int id)
        {
            var change = await _context.ProposedChanges
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.Status == ChangeStatus.Pending);
            if (change == null) return NotFound();

            if (!string.IsNullOrEmpty(change.NewPhotosJson))
            {
                var newPhotos = JsonSerializer.Deserialize<List<string>>(change.NewPhotosJson);
                var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "temp");
                if (newPhotos != null)
                {
                    foreach (var fileName in newPhotos)
                    {
                        string filePath = Path.Combine(tempPath, fileName);
                        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    }
                }
            }

            change.Status = ChangeStatus.Rejected;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Изменения отклонены.";
            return RedirectToAction(nameof(ProposedChanges));
        }

        // ==================== ПОЛЬЗОВАТЕЛИ ====================

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }
            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = new List<string> { "Admin", "User", "Student" };
            ViewBag.UserRoles = roles;
            ViewBag.AllRoles = allRoles;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, bool canApply, List<string>? selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.CanApply = canApply;
            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = selectedRoles?.Except(currentRoles).ToList() ?? new List<string>();
            var rolesToRemove = currentRoles.Except(selectedRoles ?? new List<string>()).ToList();

            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            TempData["Message"] = "Пользователь обновлён.";
            return RedirectToAction(nameof(Users));
        }
    }
}