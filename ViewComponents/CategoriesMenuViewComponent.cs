// ViewComponents/CategoriesMenuViewComponent.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

public class CategoriesMenuViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public CategoriesMenuViewComponent(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _categoryService.GetActiveCategoriesWithSubsAsync();
        return View(categories);
    }
}
