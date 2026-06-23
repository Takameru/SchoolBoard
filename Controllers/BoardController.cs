using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolBoard.Data;
using SchoolBoard.Models;
using SchoolBoard.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolBoard.Controllers
{
    public class BoardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchString, string? sortOrder)
        {
            // Сохраняем параметры для View
            ViewData["SearchString"] = searchString;
            ViewData["SortOrder"] = sortOrder;

            // Загружаем активных учеников с фото, категориями, лайками
            var studentsQuery = _context.Students
                .Include(s => s.Photos)
                .Include(s => s.StudentCategories).ThenInclude(sc => sc.Category)
                .Where(s => s.Status == StudentStatus.Active);

            // Поиск по имени/фамилии (без учёта регистра)
            if (!string.IsNullOrEmpty(searchString))
            {
                string search = searchString.Trim().ToLower();
                studentsQuery = studentsQuery.Where(s =>
                    s.LastName.ToLower().Contains(search) ||
                    s.FirstName.ToLower().Contains(search) ||
                    (s.MiddleName != null && s.MiddleName.ToLower().Contains(search)));
            }

            // Сортировка
            studentsQuery = sortOrder switch
            {
                "name_asc" => studentsQuery.OrderBy(s => s.LastName).ThenBy(s => s.FirstName),
                "name_desc" => studentsQuery.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName),
                "oldest" => studentsQuery.OrderBy(s => s.CreatedDate),
                _ => studentsQuery.OrderByDescending(s => s.CreatedDate) // по умолчанию - новые
            };

            var gridStudents = await studentsQuery.ToListAsync();

            // Обеспечиваем наличие DisplayCategoryId у каждого ученика
            foreach (var student in gridStudents)
            {
                if (student.DisplayCategoryId == null && student.StudentCategories.Any())
                {
                    // Временно присваиваем случайную категорию без сохранения в БД
                    var rnd = new Random();
                    int index = rnd.Next(student.StudentCategories.Count);
                    student.DisplayCategoryId = student.StudentCategories.ElementAt(index).CategoryId;
                }
            }

            // Категории с их учениками (ленты)
            var categories = await _context.Categories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var ribbons = new List<CategoryRibbon>();
            foreach (var cat in categories)
            {
                var studentsInCat = gridStudents
                    .Where(s => s.DisplayCategoryId == cat.Id)
                    .ToList();
                ribbons.Add(new CategoryRibbon
                {
                    Category = cat,
                    Students = studentsInCat
                });
            }

            var viewModel = new BoardIndexViewModel
            {
                GridStudents = gridStudents,
                SearchString = searchString,
                SortOrder = sortOrder,
                CategoryRibbons = ribbons
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Category(int id, string? searchString, string? sortOrder)
{
    var category = await _context.Categories
        .Include(c => c.StudentCategories)
        .ThenInclude(sc => sc.Student).ThenInclude(s => s.Photos)
        .FirstOrDefaultAsync(c => c.Id == id);
    if (category == null) return NotFound();

    var students = category.StudentCategories
        .Select(sc => sc.Student)
        .Where(s => s.Status == StudentStatus.Active)
        .AsEnumerable();   // переносим данные в память

    // Поиск в памяти
    if (!string.IsNullOrEmpty(searchString))
    {
        string search = searchString.Trim().ToLower();
        students = students.Where(s =>
            s.LastName.ToLower().Contains(search) ||
            s.FirstName.ToLower().Contains(search) ||
            (s.MiddleName != null && s.MiddleName.ToLower().Contains(search)));
    }

    // Сортировка в памяти
    students = sortOrder switch
    {
        "name_asc" => students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName),
        "name_desc" => students.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName),
        "oldest" => students.OrderBy(s => s.CreatedDate),
        _ => students.OrderByDescending(s => s.CreatedDate)
    };

    var studentList = students.ToList();

    ViewBag.Category = category;
    ViewBag.SearchString = searchString;
    ViewBag.SortOrder = sortOrder;
    return View(studentList);
}
    }
}