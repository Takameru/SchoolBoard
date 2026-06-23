using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolBoard.Models
{
    public enum StudentStatus
    {
        Pending,
        Active,
        Rejected
    }

    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна.")]
        [MaxLength(100)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно.")]
        [MaxLength(100)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Отчество")]
        public string? MiddleName { get; set; }

        [MaxLength(20)]
        [Display(Name = "Класс")]
        public string? Class { get; set; }

        [MaxLength(500)]
        [Display(Name = "Девиз / Цитата")]
        public string? Quote { get; set; }

        [Display(Name = "Биография")]
        public string? Biography { get; set; }  // HTML, будет проходить санитизацию

        [Required]
        [Display(Name = "Статус")]
        public StudentStatus Status { get; set; } = StudentStatus.Pending;

        [Display(Name = "Дата создания")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Внешние ключи для приоритетной и фактической категорий на доске
        [Display(Name = "Приоритетная категория")]
        public int? PreferredCategoryId { get; set; }
        [Display(Name = "Отображаемая категория")]
        public int? DisplayCategoryId { get; set; }

        // Навигационные свойства
        public Category? PreferredCategory { get; set; }
        public Category? DisplayCategory { get; set; }

        // Многие-ко-многим с категориями
        public ICollection<StudentCategory> StudentCategories { get; set; } = new List<StudentCategory>();

        // Фотографии ученика (1-5 штук)
        public ICollection<StudentPhoto> Photos { get; set; } = new List<StudentPhoto>();

        // Лайки
        public ICollection<Like> Likes { get; set; } = new List<Like>();

        // Предложенные изменения (от самого ученика, если CanEdit)
        public ICollection<ProposedChange> ProposedChanges { get; set; } = new List<ProposedChange>();

        // ... предыдущие поля ...

        [Display(Name = "Привязанный пользователь")]
        public string? IdentityUserId { get; set; }

        [Display(Name = "Может редактировать")]
        public bool CanEdit { get; set; }     

        [Display(Name = "Награды (каждая с новой строки)")]
        public string? Achievements { get; set; }   

        public string? SpecialStatus { get; set; }
        public bool ShowSpecialStatus { get; set; }

        [Display(Name = "Показывать бейдж новизны")]
        public bool ShowNewBadge { get; set; } = true;
    }
}