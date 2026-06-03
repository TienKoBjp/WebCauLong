using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    
    // Filtering, Search and Sort inputs
    public string? SearchTerm { get; set; }
    public string? SelectedCategory { get; set; }
    public string? SelectedSort { get; set; }
    
    // Pagination data
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
}
