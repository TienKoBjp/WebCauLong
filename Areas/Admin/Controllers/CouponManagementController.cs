using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponManagementController : Controller
{
    private readonly AppDbContext _context;

    public CouponManagementController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/CouponManagement
    public async Task<IActionResult> Index()
    {
        var coupons = await _context.Coupons.OrderByDescending(c => c.Id).ToListAsync();
        return View(coupons);
    }

    // GET: /Admin/CouponManagement/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Admin/CouponManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        if (ModelState.IsValid)
        {
            coupon.Code = coupon.Code.ToUpper().Trim();
            
            // Check if coupon code already exists
            var existingCoupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == coupon.Code);
            if (existingCoupon != null)
            {
                ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
                return View(coupon);
            }

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(coupon);
    }

    // POST: /Admin/CouponManagement/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon != null)
        {
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa mã giảm giá thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá cần xóa.";
        }
        return RedirectToAction(nameof(Index));
    }
}
