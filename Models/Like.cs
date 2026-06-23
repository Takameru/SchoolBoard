using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolBoard.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public Student Student { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}