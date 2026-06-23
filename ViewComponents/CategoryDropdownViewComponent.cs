using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolBoard.Data;
using System.Threading.Tasks;

namespace SchoolBoard.ViewComponents
{
    public class CategoryDropdownViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoryDropdownViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
            return View(categories);
        }
    }
}