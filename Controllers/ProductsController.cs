using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMvcApp.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? category, string? sortBy, int page = 1)
    {
        // 1. Get database query including Category relationship
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        // 2. Apply search filter (case-insensitive contains on Name/Description)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        // 3. Apply category filter
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Category != null && p.Category.Name == category);
        }

        // 4. Apply sorting
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "rating_desc" => query.OrderByDescending(p => p.Rating),
            "name_desc" => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name) // Default: Name A-Z
        };

        // 5. Calculate pagination math
        const int pageSize = 6;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        // Clamp page value within boundaries
        page = Math.Max(1, Math.Min(totalPages == 0 ? 1 : totalPages, page));

        // Fetch paginated chunk from DB
        var paginatedProducts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 6. Map to the ViewModel
        var model = new ProductListViewModel
        {
            Products = paginatedProducts,
            SearchTerm = searchTerm,
            SelectedCategory = category ?? "All",
            SelectedSort = sortBy ?? "name_asc",
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(model);
    }

    [Route("Products/Details/{id}")]
    [Route("product/detail/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        // 1. Fetch the product by Id with its Category and Images
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        // 2. Fetch related products from the same category, excluding current product, taking 3 items
        var relatedProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
            .Take(3)
            .ToListAsync();

        // 3. Construct view model
        var model = new ProductDetailsViewModel
        {
            Product = product,
            RelatedProducts = relatedProducts
        };

        return View(model);
    }
}



