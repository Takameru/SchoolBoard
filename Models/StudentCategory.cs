using System.ComponentModel.DataAnnotations;

namespace SchoolBoard.Models
{
    public class StudentCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        // Навигационные свойства
        public Student Student { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}