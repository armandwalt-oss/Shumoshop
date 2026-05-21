// Services/CategoryService.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models; // adjust if your DbContext or models live elsewhere

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db; // replace with your actual DbContext name

    public CategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetActiveCategoriesWithSubsAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .Include(c => c.SubCategories)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                IconName = c.IconName,
                SubCategories = c.SubCategories
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s => new SubCategoryDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        IsActive = s.IsActive
                    }).ToList()
            })
            .ToListAsync();
    }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? IconName { get; set; }
    public List<SubCategoryDto> SubCategories { get; set; } = new();
}

public class SubCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}
