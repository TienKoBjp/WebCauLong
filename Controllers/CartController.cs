using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using AspNetMvcApp.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMvcApp.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Cart
    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        ViewBag.CartTotal = cart.Sum(item => item.TotalPrice);
        return View(cart);
    }

    // POST: /Cart/AddToCart
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImagePath = product.ImagePath,
                Price = product.Price,
                Quantity = quantity
            });
        }

        HttpContext.Session.SetObjectAsJson("Cart", cart);
        var totalQuantity = cart.Sum(item => item.Quantity);

        // Check if request is AJAX
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, cartCount = totalQuantity });
        }

        TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
        return RedirectToAction("Index", "Products");
    }

    // POST: /Cart/UpdateQuantity
    [HttpPost]
    public IActionResult UpdateQuantity(int productId, int quantity)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

        if (cartItem != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
            }
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        var totalQuantity = cart.Sum(item => item.Quantity);
        var cartTotal = cart.Sum(item => item.TotalPrice);
        var itemTotal = cartItem != null ? cartItem.TotalPrice : 0;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new 
            { 
                success = true, 
                cartCount = totalQuantity, 
                itemTotal = itemTotal, 
                cartTotal = cartTotal,
                removed = quantity <= 0
            });
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/RemoveItem
    [HttpPost]
    public IActionResult RemoveItem(int productId)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

        if (cartItem != null)
        {
            cart.Remove(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        var totalQuantity = cart.Sum(item => item.Quantity);
        var cartTotal = cart.Sum(item => item.TotalPrice);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, cartCount = totalQuantity, cartTotal = cartTotal });
        }

        TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/ClearCart
    [HttpPost]
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove("Cart");
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, cartCount = 0, cartTotal = 0 });
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Cart/GetCartCount
    [HttpGet]
    public IActionResult GetCartCount()
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        var totalQuantity = cart.Sum(item => item.Quantity);
        return Json(new { cartCount = totalQuantity });
    }
}
