using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AspNetMvcApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public DashboardController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalProducts = await _context.Products.CountAsync();
        ViewBag.TotalCategories = await _context.Categories.CountAsync();
        ViewBag.TotalUsers = _userManager.Users.Count();
        ViewBag.InStockProducts = await _context.Products.CountAsync(p => p.StockStatus == "InStock");
        ViewBag.LowStockProducts = await _context.Products.CountAsync(p => p.StockStatus == "LowStock");
        ViewBag.OutOfStockProducts = await _context.Products.CountAsync(p => p.StockStatus == "OutOfStock");
        ViewBag.FeaturedProducts = await _context.Products.CountAsync(p => p.IsFeatured);

        // 1. Query Monthly Revenue for the last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlyRevenue = await _context.Orders
            .Where(o => o.OrderDate >= sixMonthsAgo && o.Status != "Cancelled")
            .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        ViewBag.MonthlyRevenueLabels = monthlyRevenue.Select(r => $"{r.Month}/{r.Year}").ToList();
        ViewBag.MonthlyRevenueData = monthlyRevenue.Select(r => r.Revenue).ToList();

        // 2. Query Category Sales Distribution
        var categorySales = await _context.OrderItems
            .Include(oi => oi.Product)
            .ThenInclude(p => p.Category)
            .GroupBy(oi => (oi.Product != null && oi.Product.Category != null) ? oi.Product.Category.Name : "Chưa phân loại")
            .Select(g => new
            {
                CategoryName = g.Key,
                TotalSales = g.Sum(oi => oi.Quantity * oi.UnitPrice)
            })
            .ToListAsync();

        ViewBag.CategorySalesLabels = categorySales.Select(c => c.CategoryName).ToList();
        ViewBag.CategorySalesData = categorySales.Select(c => c.TotalSales).ToList();

        return View();
    }
}
