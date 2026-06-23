using System.ComponentModel.DataAnnotations;

namespace SchoolBoard.Models
{
    public class StudentPhoto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Display(Name = "Главное фото")]
        public bool IsMain { get; set; }

        [Display(Name = "Порядок отображения")]
        public int UploadOrder { get; set; }

        // Навигационное свойство
        public Student Student { get; set; } = null!;
    }
}