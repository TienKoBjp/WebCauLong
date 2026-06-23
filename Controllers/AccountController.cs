using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Net;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;

namespace AspNetMvcApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly AppDbContext _context;
    private readonly AspNetMvcApp.Services.EmailService _emailService;

    public AccountController(
        UserManager<IdentityUser> userManager, 
        SignInManager<IdentityUser> signInManager, 
        IWebHostEnvironment webHostEnvironment, 
        AppDbContext context,
        AspNetMvcApp.Services.EmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _webHostEnvironment = webHostEnvironment;
        _context = context;
        _emailService = emailService;
    }

    private static string DecodeProfileValue(string? value)
    {
        return WebUtility.HtmlDecode(value ?? string.Empty).Trim();
    }

    // GET: /Account/Login
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "ProductManagement", new { area = "Admin" });
            }
            return RedirectToAction("Index", "Products");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "ProductManagement", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Products");
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
        }

        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Products");
        }
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign default "User" role
                await _userManager.AddToRoleAsync(user, "User");

                // Save FullName as a claim
                var fullName = DecodeProfileValue(model.FullName);
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    await _userManager.AddClaimAsync(user, new Claim("FullName", fullName));
                }

                // Auto sign-in after registration
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Products");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        // Cart is intentionally NOT cleared here so that when the user logs back in,
        // their previously added items are still present in the session.
        return RedirectToAction("Index", "Products");
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // GET: /Account/Profile
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // Retrieve user's orders with items & products
        var orders = _context.Orders
            .Where(o => o.UserId == user.Id)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        var reviewedItems = _context.ProductReviews
            .Where(r => r.UserId == user.Id)
            .Select(r => r.OrderId + "_" + r.ProductId)
            .ToList()
            .ToHashSet();
        ViewBag.ReviewedItems = reviewedItems;

        var claims = await _userManager.GetClaimsAsync(user);
        ViewBag.FullName = DecodeProfileValue(claims.FirstOrDefault(c => c.Type == "FullName")?.Value);
        ViewBag.PhoneNumber = DecodeProfileValue(user.PhoneNumber ?? claims.FirstOrDefault(c => c.Type == "PhoneNumber")?.Value);
        ViewBag.Address = DecodeProfileValue(claims.FirstOrDefault(c => c.Type == "Address")?.Value);
        ViewBag.AvatarPath = claims.FirstOrDefault(c => c.Type == "AvatarPath")?.Value;
        ViewBag.Orders = orders;
        return View(user);
    }

    // POST: /Account/Profile
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(IFormFile? avatarFile, string? fullName, string? phoneNumber, string? address)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        bool hasChanges = false;
        var existingClaims = await _userManager.GetClaimsAsync(user);

        // 1. Update FullName claim
        if (fullName != null)
        {
            foreach (var fullNameClaim in existingClaims.Where(c => c.Type == "FullName").ToList())
            {
                await _userManager.RemoveClaimAsync(user, fullNameClaim);
            }
            await _userManager.AddClaimAsync(user, new Claim("FullName", DecodeProfileValue(fullName)));
            hasChanges = true;
        }

        // 2. Update Address claim
        if (address != null)
        {
            foreach (var addressClaim in existingClaims.Where(c => c.Type == "Address").ToList())
            {
                await _userManager.RemoveClaimAsync(user, addressClaim);
            }
            await _userManager.AddClaimAsync(user, new Claim("Address", DecodeProfileValue(address)));
            hasChanges = true;
        }

        // 3. Update PhoneNumber
        if (phoneNumber != null)
        {
            var cleanPhoneNumber = DecodeProfileValue(phoneNumber);
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, cleanPhoneNumber);
            if (phoneResult.Succeeded)
            {
                hasChanges = true;
            }
            foreach (var phoneClaim in existingClaims.Where(c => c.Type == "PhoneNumber").ToList())
            {
                await _userManager.RemoveClaimAsync(user, phoneClaim);
            }
            await _userManager.AddClaimAsync(user, new Claim("PhoneNumber", cleanPhoneNumber));
            hasChanges = true;
        }

        // 4. Update Avatar
        if (avatarFile != null && avatarFile.Length > 0)
        {
            var originalName = Path.GetFileName(avatarFile.FileName);
            var extension = Path.GetExtension(originalName);
            var uniqueName = "avatar_" + user.Id.Substring(0, 8) + "_" + Guid.NewGuid().ToString().Substring(0, 6) + extension;
            
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product", "images");
            Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, uniqueName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(fileStream);
            }

            var avatarClaim = existingClaims.FirstOrDefault(c => c.Type == "AvatarPath");
            if (avatarClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, avatarClaim);
                var oldFilePath = Path.Combine(uploadsFolder, avatarClaim.Value);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            await _userManager.AddClaimAsync(user, new Claim("AvatarPath", uniqueName));
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không có thông tin nào thay đổi hoặc có lỗi xảy ra.";
        }

        var orders = _context.Orders
            .Where(o => o.UserId == user.Id)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToList();
        var reviewedItems = _context.ProductReviews
            .Where(r => r.UserId == user.Id)
            .Select(r => r.OrderId + "_" + r.ProductId)
            .ToList()
            .ToHashSet();
        ViewBag.ReviewedItems = reviewedItems;

        var updatedClaims = await _userManager.GetClaimsAsync(user);
        ViewBag.FullName = DecodeProfileValue(updatedClaims.FirstOrDefault(c => c.Type == "FullName")?.Value);
        ViewBag.PhoneNumber = DecodeProfileValue(user.PhoneNumber ?? updatedClaims.FirstOrDefault(c => c.Type == "PhoneNumber")?.Value);
        ViewBag.Address = DecodeProfileValue(updatedClaims.FirstOrDefault(c => c.Type == "Address")?.Value);
        ViewBag.AvatarPath = updatedClaims.FirstOrDefault(c => c.Type == "AvatarPath")?.Value;
        ViewBag.Orders = orders;

        return View(user);
    }

    // POST: /Account/CancelOrder
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status == "Pending")
        {
            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đơn hàng của bạn đã được hủy thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể hủy đơn hàng vì đơn đang được xử lý hoặc đã giao/hủy.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi từ dịch vụ bên ngoài: {remoteError}");
            return View("Login");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser { UserName = email, Email = email };
                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                }
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            TempData["ErrorMessage"] = "Không thể đăng nhập bằng tài khoản này.";
            return RedirectToAction(nameof(Login));
        }
    }

    // GET: /Account/ForgotPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: /Account/ForgotPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist to prevent enumeration attacks,
                // but redirect to validation confirmation with exists=false.
                return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email = model.Email, exists = false });
            }

            // Generate reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Build absolute link
            var resetLink = Url.Action("ResetPassword", "Account", 
                new { token = token, email = model.Email }, 
                Request.Scheme);

            // Send mail
            var claims = await _userManager.GetClaimsAsync(user);
            var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? user.UserName ?? "Khách hàng";

            await _emailService.SendPasswordResetEmailAsync(model.Email, fullName, resetLink ?? string.Empty);

            return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email = model.Email, exists = true, token = token });
        }

        return View(model);
    }

    // GET: /Account/ForgotPasswordConfirmation
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation(string email, bool exists, string? token = null)
    {
        ViewBag.Email = email;
        ViewBag.Exists = exists;
        ViewBag.Token = token;
        return View();
    }

    // GET: /Account/ResetPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
        {
            return BadRequest("Token và Email không hợp lệ.");
        }

        var model = new ResetPasswordViewModel
        {
            Token = token,
            Email = email
        };
        return View(model);
    }

    // POST: /Account/ResetPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    // GET: /Account/ResetPasswordConfirmation
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    // POST: /Account/SubmitReview
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(int productId, int orderId, int rating, string comment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
        }

        if (rating < 1 || rating > 5)
        {
            return Json(new { success = false, message = "Đánh giá sao không hợp lệ (phải từ 1 đến 5 sao)." });
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

        if (order == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng tương ứng." });
        }

        if (order.Status != "Delivered")
        {
            return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sản phẩm của đơn hàng đã giao thành công." });
        }

        var hasProduct = order.OrderItems.Any(oi => oi.ProductId == productId);
        if (!hasProduct)
        {
            return Json(new { success = false, message = "Sản phẩm không thuộc đơn hàng này." });
        }

        var existingReview = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id && r.OrderId == orderId);

        if (existingReview != null)
        {
            return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này cho đơn hàng này rồi." });
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = user.Email?.Split('@')[0] ?? "Khách hàng";
        }

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = user.Id,
            CustomerName = DecodeProfileValue(fullName),
            Rating = rating,
            Comment = comment ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            OrderId = orderId
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        // Recalculate average rating of the product
        var productReviews = await _context.ProductReviews.Where(r => r.ProductId == productId).ToListAsync();
        if (productReviews.Any())
        {
            var avgRating = productReviews.Average(r => r.Rating);
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.Rating = Math.Round(avgRating, 1);
                await _context.SaveChangesAsync();
            }
        }

        return Json(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá sản phẩm!" });
    }
}
