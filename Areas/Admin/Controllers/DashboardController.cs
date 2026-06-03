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

        return View();
    }
}
