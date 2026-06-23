using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderManagementController : Controller
{
    private readonly AppDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;
    private readonly AspNetMvcApp.Services.EmailService _emailService;

    public OrderManagementController(
        AppDbContext context,
        Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager,
        AspNetMvcApp.Services.EmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            order.Status = status;
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(order.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendOrderStatusEmailAsync(user.Email, order);
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ExportToCsv()
    {
        var orders = await _context.Orders
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        // UTF-8 BOM so Excel displays Vietnamese correctly
        builder.AppendLine("Mã Đơn Hàng,Khách Hàng,Số Điện Thoại,Địa Chỉ Giao Hàng,Ngày Đặt,Tổng Thanh Toán,Trạng Thái,Phương Thức");

        foreach (var order in orders)
        {
            var customer = $"\"{order.CustomerName.Replace("\"", "\"\"")}\"";
            var phone = $"\"{order.PhoneNumber.Replace("\"", "\"\"")}\"";
            var address = $"\"{order.ShippingAddress.Replace("\"", "\"\"")}\"";
            var status = $"\"{order.Status.Replace("\"", "\"\"")}\"";
            var method = $"\"{order.PaymentMethod.Replace("\"", "\"\"")}\"";
            
            builder.AppendLine($"{order.Id},{customer},{phone},{address},{order.OrderDate.ToLocalTime():yyyy-MM-dd HH:mm:ss},{order.TotalAmount},{status},{method}");
        }

        var csvBytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var fileBytes = new byte[bom.Length + csvBytes.Length];
        Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        Buffer.BlockCopy(csvBytes, 0, fileBytes, bom.Length, csvBytes.Length);

        return File(fileBytes, "text/csv", $"orders_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}
