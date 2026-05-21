using System;
using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class ShopIndexViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<SubCategory> SubCategories { get; set; } = new();

        public string? SelectedCategory { get; set; }
        public string? SelectedSubCategory { get; set; }
        public string? SearchTerm { get; set; }

        public string PageTitle { get; set; } = "Shop Our Products";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public int TotalCount { get; set; }

        // NEW
        public string Sort { get; set; } = "name_asc";

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
