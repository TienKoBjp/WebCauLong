using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;

namespace AspNetMvcApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Hiển thị danh sách tài khoản người dùng
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userVMs = new List<UserVM>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userVMs.Add(new UserVM
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Không có vai trò"
                });
            }

            return View(userVMs);
        }

        // Hiển thị form chỉnh sửa tài khoản & vai trò
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleList = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.Roles = new SelectList(roleList, roles.FirstOrDefault());

            var userVM = new UserVM
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty
            };

            return View(userVM);
        }

        // Xử lý cập nhật vai trò người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserVM userVM)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(userVM.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Cập nhật email và username
                user.Email = userVM.Email;
                user.UserName = userVM.Email; // Standard identity configuration: UserName is usually Email

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var oldRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);

                    if (!string.IsNullOrEmpty(userVM.Role))
                    {
                        await _userManager.AddToRoleAsync(user, userVM.Role);
                    }

                    TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var roleList = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.Roles = new SelectList(roleList, userVM.Role);
            return View(userVM);
        }

        // Hiển thị màn hình xác nhận xóa tài khoản
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userVM = new UserVM
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Không có vai trò"
            };

            return View(userVM);
        }

        // Xử lý xóa tài khoản
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Ngăn chặn admin tự xóa tài khoản của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["SuccessMessage"] = "Không thể tự xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Xóa tài khoản thành công!";
            }
            else
            {
                TempData["SuccessMessage"] = "Có lỗi xảy ra khi xóa tài khoản.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
