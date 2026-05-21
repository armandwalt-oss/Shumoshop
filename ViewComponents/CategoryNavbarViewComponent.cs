using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.ViewComponents
{
    public class CategoryNavbarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoryNavbarViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }
    }
}