using Microsoft.AspNetCore.Identity;

namespace SchoolBoard.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool CanApply { get; set; }
    }
}