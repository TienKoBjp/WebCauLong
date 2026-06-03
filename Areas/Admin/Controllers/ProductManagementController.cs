using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using AspNetMvcApp.Models;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductManagementController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductManagementController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: Admin/ProductManagement
    public async Task<IActionResult> Index(int page = 1)
    {
        if (page < 1) page = 1;
        int pageSize = 5; // 5 items per page for testing

        var totalItems = await _context.Products.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        if (page > totalPages && totalPages > 0) page = totalPages;

        var products = await _context.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;

        return View(products);
    }

    // GET: Admin/ProductManagement/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        return View();
    }

    // POST: Admin/ProductManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile, List<IFormFile>? detailImages)
    {
        // Remove navigation properties from validation
        ModelState.Remove("Category");
        if (imageFile != null && imageFile.Length > 0)
        {
            ModelState.Remove("ImagePath");
        }

        if (ModelState.IsValid)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product", "images");
            Directory.CreateDirectory(uploadsFolder);

            if (imageFile != null && imageFile.Length > 0)
            {
                var originalName = Path.GetFileName(imageFile.FileName);
                var extension = Path.GetExtension(originalName);
                var uniqueName = Path.GetFileNameWithoutExtension(originalName) + "_" + Guid.NewGuid().ToString().Substring(0, 6) + extension;
                var filePath = Path.Combine(uploadsFolder, uniqueName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                product.ImagePath = uniqueName;
            }

            // Save detail images
            if (detailImages != null && detailImages.Count > 0)
            {
                int order = 1;
                foreach (var file in detailImages)
                {
                    if (file.Length > 0)
                    {
                        var originalName = Path.GetFileName(file.FileName);
                        var extension = Path.GetExtension(originalName);
                        var uniqueName = Path.GetFileNameWithoutExtension(originalName) + "_" + Guid.NewGuid().ToString().Substring(0, 6) + extension;
                        var filePath = Path.Combine(uploadsFolder, uniqueName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        product.ProductImages.Add(new ProductImage
                        {
                            ImagePath = uniqueName,
                            DisplayOrder = order++
                        });
                    }
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // GET: Admin/ProductManagement/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // POST: Admin/ProductManagement/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile, List<IFormFile>? detailImages)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        // Remove navigation properties from validation
        ModelState.Remove("Category");
        if (imageFile != null && imageFile.Length > 0)
        {
            ModelState.Remove("ImagePath");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product", "images");
                Directory.CreateDirectory(uploadsFolder);

                if (imageFile != null && imageFile.Length > 0)
                {
                    var originalName = Path.GetFileName(imageFile.FileName);
                    var extension = Path.GetExtension(originalName);
                    var uniqueName = Path.GetFileNameWithoutExtension(originalName) + "_" + Guid.NewGuid().ToString().Substring(0, 6) + extension;
                    var filePath = Path.Combine(uploadsFolder, uniqueName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    product.ImagePath = uniqueName;
                }
                else if (string.IsNullOrWhiteSpace(product.ImagePath))
                {
                    var originalProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (originalProduct != null)
                    {
                        product.ImagePath = originalProduct.ImagePath;
                    }
                }

                // Handle detail images
                if (detailImages != null && detailImages.Count > 0)
                {
                    var existingImages = await _context.ProductImages.Where(i => i.ProductId == id).ToListAsync();
                    int maxOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) : 0;
                    
                    foreach (var file in detailImages)
                    {
                        if (file.Length > 0)
                        {
                            var originalName = Path.GetFileName(file.FileName);
                            var extension = Path.GetExtension(originalName);
                            var uniqueName = Path.GetFileNameWithoutExtension(originalName) + "_" + Guid.NewGuid().ToString().Substring(0, 6) + extension;
                            var filePath = Path.Combine(uploadsFolder, uniqueName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            
                            var newImg = new ProductImage
                            {
                                ProductId = id,
                                ImagePath = uniqueName,
                                DisplayOrder = ++maxOrder
                            };
                            _context.ProductImages.Add(newImg);
                        }
                    }
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(p => p.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // POST: Admin/ProductManagement/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/ProductManagement/DeleteProductImage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductImage([FromRoute] int id)
    {
        var img = await _context.ProductImages.FindAsync(id);
        if (img != null)
        {
            var productId = img.ProductId;
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "product", "images", img.ImagePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa ảnh chi tiết thành công!";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
        return NotFound();
    }

    // POST: Admin/ProductManagement/SetMainImage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage([FromRoute] int id)
    {
        var img = await _context.ProductImages.FindAsync(id);
        if (img != null)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == img.ProductId);

            if (product != null)
            {
                var oldMainImage = product.ImagePath;
                var newMainImage = img.ImagePath;

                if (oldMainImage != newMainImage)
                {
                    // 1. Set the new main image
                    product.ImagePath = newMainImage;

                    // 2. Add old main image to ProductImages if it is not already there
                    if (!string.IsNullOrWhiteSpace(oldMainImage))
                    {
                        var exists = product.ProductImages.Any(pi => pi.ImagePath == oldMainImage);
                        if (!exists)
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImagePath = oldMainImage,
                                DisplayOrder = product.ProductImages.Any() ? product.ProductImages.Max(pi => pi.DisplayOrder) + 1 : 1
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đặt làm ảnh chính thành công!";
                }
                return RedirectToAction(nameof(Edit), new { id = product.Id });
            }
        }
        return NotFound();
    }
}
