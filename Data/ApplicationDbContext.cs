using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolBoard.Models;

namespace SchoolBoard.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<StudentCategory> StudentCategories => Set<StudentCategory>();
        public DbSet<StudentPhoto> StudentPhotos => Set<StudentPhoto>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<ProposedChange> ProposedChanges => Set<ProposedChange>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // обязательно для Identity

            // Связь многие-ко-многим через StudentCategory
            builder.Entity<StudentCategory>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCategories)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentCategory>()
                .HasOne(sc => sc.Category)
                .WithMany(c => c.StudentCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student -> Photos (каскадное удаление)
            builder.Entity<StudentPhoto>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Photos)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student -> Likes (каскад)
            builder.Entity<Like>()
                .HasOne(l => l.Student)
                .WithMany(s => s.Likes)
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Уникальный индекс на Like (UserId, StudentId)
            builder.Entity<Like>()
                .HasIndex(l => new { l.UserId, l.StudentId })
                .IsUnique();

            // Внешние ключи для PreferredCategory и DisplayCategory
            builder.Entity<Student>()
                .HasOne(s => s.PreferredCategory)
                .WithMany()
                .HasForeignKey(s => s.PreferredCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Student>()
                .HasOne(s => s.DisplayCategory)
                .WithMany()
                .HasForeignKey(s => s.DisplayCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // ProposedChange -> Student (каскад)
            builder.Entity<ProposedChange>()
                .HasOne(pc => pc.Student)
                .WithMany(s => s.ProposedChanges)
                .HasForeignKey(pc => pc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProposedChange -> User
            builder.Entity<ProposedChange>()
                .HasOne(pc => pc.ProposedByUser)
                .WithMany()
                .HasForeignKey(pc => pc.ProposedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}