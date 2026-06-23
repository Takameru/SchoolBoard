using System.Collections.Generic;
using SchoolBoard.Models;

namespace SchoolBoard.Models.ViewModels
{
    public class BoardIndexViewModel
    {
        public List<Student> GridStudents { get; set; } = new List<Student>();
        public string? SearchString { get; set; }
        public string? SortOrder { get; set; }

        // Для лент: список категорий, в каждой — список её учеников согласно DisplayCategoryId
        public List<CategoryRibbon> CategoryRibbons { get; set; } = new List<CategoryRibbon>();
    }

    public class CategoryRibbon
    {
        public Category Category { get; set; } = null!;
        public List<Student> Students { get; set; } = new List<Student>();
    }
}