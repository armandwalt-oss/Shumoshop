// Services/ICategoryService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetActiveCategoriesWithSubsAsync();
}