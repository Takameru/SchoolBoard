using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolBoard.Models
{
    public enum ChangeStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class ProposedChange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public string ProposedByUserId { get; set; } = string.Empty;

        public DateTime ProposedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ChangeStatus Status { get; set; } = ChangeStatus.Pending;

        /// <summary>
        /// JSON-строка со старыми значениями (снимок до изменения)
        /// </summary>
        [Required]
        public string OldDataJson { get; set; } = "{}";

        /// <summary>
        /// JSON-строка с новыми значениями
        /// </summary>
        [Required]
        public string NewDataJson { get; set; } = "{}";

        /// <summary>
        /// JSON-строка со списком старых имён файлов фотографий
        /// </summary>
        public string? OldPhotosJson { get; set; }

        /// <summary>
        /// JSON-строка со списком новых имён файлов фотографий (или путей)
        /// </summary>
        public string? NewPhotosJson { get; set; }

        // Навигационные свойства
        public Student Student { get; set; } = null!;
        public ApplicationUser ProposedByUser { get; set; } = null!;
    }
}