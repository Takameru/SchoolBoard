using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolBoard.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название категории обязательно.")]
        [MaxLength(100)]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)] // например, #e67e22
        [Display(Name = "Цвет")]
        public string? Color { get; set; }

        [MaxLength(50)]
        [Display(Name = "Иконка Font Awesome (класс)")]
        public string? Icon { get; set; }   // например, "fa-solid fa-star"

        [Display(Name = "Порядок сортировки")]
        public int SortOrder { get; set; }

        // Навигационное свойство: категория может быть связана со многими учениками
        public ICollection<StudentCategory> StudentCategories { get; set; } = new List<StudentCategory>();
    }
}