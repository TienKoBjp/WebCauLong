using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using AspNetMvcApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMvcApp.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AspNetMvcApp.Services.EmailService _emailService;
    private readonly IConfiguration _configuration;

    public CartController(AppDbContext context, UserManager<IdentityUser> userManager, AspNetMvcApp.Services.EmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    // GET: /Cart
    [Microsoft.AspNetCore.Authorization.Authorize]
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
        // Require authentication to add to cart
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, requiresLogin = true, message = "Vui lòng đăng nhập để thêm vào giỏ hàng!" });
            }
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Products") });
        }

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
        var shippingFee = cartTotal > 1500000m ? 0m : (cart.Any() ? 30000m : 0m);
        var grandTotal = cartTotal + shippingFee;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, cartCount = totalQuantity, cartTotal = cartTotal, shippingFee = shippingFee, grandTotal = grandTotal });
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
            return Json(new { success = true, cartCount = 0, cartTotal = 0m, shippingFee = 0m, grandTotal = 0m });
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

    // POST: /Cart/AddBundleToCart
    [HttpPost]
    public async Task<IActionResult> AddBundleToCart(List<int> productIds)
    {
        if (productIds == null || !productIds.Any())
        {
            return Json(new { success = false, message = "Danh sách sản phẩm trống." });
        }

        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

        foreach (var id in productIds)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                var cartItem = cart.FirstOrDefault(item => item.ProductId == id);
                if (cartItem != null)
                {
                    cartItem.Quantity += 1;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ImagePath = product.ImagePath,
                        Price = product.Price,
                        Quantity = 1
                    });
                }
            }
        }

        HttpContext.Session.SetObjectAsJson("Cart", cart);
        var totalQuantity = cart.Sum(item => item.Quantity);

        return Json(new { success = true, cartCount = totalQuantity });
    }

    // GET: /Cart/Checkout
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Checkout()
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction("Index");
        }

        decimal cartTotal = cart.Sum(item => item.TotalPrice);
        
        string userFullName = "";
        string userPhone = "";
        string userAddress = "";
        
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name ?? "");
            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                userFullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? user.UserName ?? "";
                userPhone = user.PhoneNumber ?? claims.FirstOrDefault(c => c.Type == "PhoneNumber")?.Value ?? "";
                userAddress = claims.FirstOrDefault(c => c.Type == "Address")?.Value ?? "";
            }
        }

        ViewBag.UserFullName = userFullName;
        ViewBag.UserPhone = userPhone;
        ViewBag.UserAddress = userAddress;
        ViewBag.CartTotal = cartTotal;
        
        // Shipping fee is free over 1.5M, otherwise 30,000 đ
        decimal shippingFee = cartTotal > 1500000m ? 0m : 30000m;
        ViewBag.ShippingFee = shippingFee;
        ViewBag.GrandTotal = cartTotal + shippingFee;

        return View(cart);
    }

    // POST: /Cart/Checkout
    [HttpPost]
    public async Task<IActionResult> Checkout(string customerName, string phoneNumber, string shippingAddress, string paymentMethod)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction("Index");
        }

        decimal cartTotal = cart.Sum(item => item.TotalPrice);
        decimal shippingFee = cartTotal > 1500000m ? 0m : 30000m;
        
        decimal discount = 0;
        var couponCode = HttpContext.Session.GetString("AppliedCoupon");
        if (!string.IsNullOrEmpty(couponCode))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive && c.ExpiryDate > DateTime.UtcNow);
            if (coupon != null && coupon.UsageCount < coupon.UsageLimit)
            {
                if (coupon.DiscountType == "Percentage")
                {
                    discount = cartTotal * (coupon.DiscountValue / 100m);
                }
                else
                {
                    discount = coupon.DiscountValue;
                }
                if (discount > cartTotal) discount = cartTotal;
                
                coupon.UsageCount++;
                _context.Update(coupon);
            }
        }

        decimal totalAmount = cartTotal - discount + shippingFee;
        if (totalAmount < 0) totalAmount = 0;

        string userId = "guest";
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                userId = user.Id;
            }
        }

        var order = new Order
        {
            UserId = userId,
            CustomerName = customerName,
            PhoneNumber = phoneNumber,
            ShippingAddress = shippingAddress,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            ShippingFee = shippingFee,
            Status = paymentMethod == "Momo" ? "Unpaid" : "Pending",
            PaymentMethod = paymentMethod,
            CouponCode = couponCode,
            DiscountAmount = discount
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in cart)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            };
            _context.OrderItems.Add(orderItem);
        }

        await _context.SaveChangesAsync();

        if (paymentMethod == "Momo")
        {
            var payUrl = await CreateMomoPaymentUrl(order);
            if (!string.IsNullOrEmpty(payUrl))
            {
                return Redirect(payUrl);
            }
            else
            {
                // Clean up order if payment creation fails
                _context.OrderItems.RemoveRange(_context.OrderItems.Where(oi => oi.OrderId == order.Id));
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Không thể kết nối đến cổng thanh toán MoMo Sandbox. Vui lòng thử lại.";
                return RedirectToAction("Checkout");
            }
        }

        // Clear Session Cart and Coupon
        HttpContext.Session.Remove("Cart");
        HttpContext.Session.Remove("AppliedCoupon");

        // Send Email Receipt
        try
        {
            string userEmail = "";
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name ?? "");
                if (user != null)
                {
                    userEmail = user.Email ?? "";
                }
            }
            await _emailService.SendOrderReceiptEmailAsync(order, cart, userEmail);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending email: " + ex.Message);
        }

        return RedirectToAction("CheckoutSuccess", new { id = order.Id });
    }

    // GET: /Cart/CheckoutSuccess/{id}
    [HttpGet]
    public async Task<IActionResult> CheckoutSuccess(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // POST: /Cart/ApplyCoupon
    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
        }

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive && c.ExpiryDate > DateTime.UtcNow);
        if (coupon == null)
        {
            return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn." });
        }

        if (coupon.UsageCount >= coupon.UsageLimit)
        {
            return Json(new { success = false, message = "Mã giảm giá đã đạt giới hạn sử dụng." });
        }

        var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        decimal cartTotal = cart.Sum(item => item.TotalPrice);

        decimal discount = 0;
        if (coupon.DiscountType == "Percentage")
        {
            discount = cartTotal * (coupon.DiscountValue / 100m);
        }
        else
        {
            discount = coupon.DiscountValue;
        }

        if (discount > cartTotal) discount = cartTotal;

        decimal shippingFee = cartTotal > 1500000m ? 0m : 30000m;
        decimal grandTotal = cartTotal - discount + shippingFee;
        if (grandTotal < 0) grandTotal = 0;

        HttpContext.Session.SetString("AppliedCoupon", coupon.Code);

        return Json(new { 
            success = true, 
            message = $"Áp dụng mã {coupon.Code} thành công!", 
            discount = discount, 
            shippingFee = shippingFee, 
            grandTotal = grandTotal 
        });
    }

    private string ComputeHmacSha256(string message, string secretKey)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private async Task<string?> CreateMomoPaymentUrl(Order order)
    {
        var partnerCode = _configuration["Momo:PartnerCode"] ?? "MOMOBKUN20180529";
        var accessKey = _configuration["Momo:AccessKey"] ?? "klm05TvNBzhg7h7j";
        var secretKey = _configuration["Momo:SecretKey"] ?? "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";
        var apiUrl = _configuration["Momo:ApiUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";

        var requestId = Guid.NewGuid().ToString();
        var momoOrderId = $"{order.Id}_{DateTime.UtcNow.Ticks}";
        var amount = (long)order.TotalAmount;
        var orderInfo = $"Thanh toan don hang #{order.Id} tai Shop Yonex";
        
        var redirectUrl = $"{Request.Scheme}://{Request.Host}/Cart/MomoReturn";
        var ipnUrl = $"{Request.Scheme}://{Request.Host}/Cart/MomoNotify";
        
        var requestType = "captureWallet";
        var extraData = "";
        var lang = "vi";

        var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={momoOrderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
        var signature = ComputeHmacSha256(rawSignature, secretKey);

        var requestBody = new
        {
            partnerCode,
            partnerName = "Shop Yonex",
            storeId = "ShopYonex",
            requestId,
            amount,
            orderId = momoOrderId,
            orderInfo,
            redirectUrl,
            ipnUrl,
            requestType,
            extraData,
            lang,
            signature
        };

        using (var client = new System.Net.Http.HttpClient())
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using (var doc = System.Text.Json.JsonDocument.Parse(responseString))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("resultCode", out var codeProperty) && codeProperty.GetInt32() == 0)
                        {
                            if (root.TryGetProperty("payUrl", out var payUrlProperty))
                            {
                                return payUrlProperty.GetString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in CreateMomoPaymentUrl: " + ex.Message);
            }
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> MomoReturn()
    {
        var accessKey = _configuration["Momo:AccessKey"] ?? "klm05TvNBzhg7h7j";
        var secretKey = _configuration["Momo:SecretKey"] ?? "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";

        // Read query parameters directly
        string partnerCode = Request.Query["partnerCode"].ToString() ?? "";
        string orderId = Request.Query["orderId"].ToString() ?? "";
        string requestId = Request.Query["requestId"].ToString() ?? "";
        string amount = Request.Query["amount"].ToString() ?? "";
        string orderInfo = Request.Query["orderInfo"].ToString() ?? "";
        string orderType = Request.Query["orderType"].ToString() ?? "";
        string transId = Request.Query["transId"].ToString() ?? "";
        string resultCode = Request.Query["resultCode"].ToString() ?? "";
        string message = Request.Query["message"].ToString() ?? "";
        string payType = Request.Query["payType"].ToString() ?? "";
        string responseTime = Request.Query["responseTime"].ToString() ?? "";
        string extraData = Request.Query["extraData"].ToString() ?? "";
        string signature = Request.Query["signature"].ToString() ?? "";

        // Log parameters for debugging
        Console.WriteLine("--- MOMO RETURN DEBUG INFO ---");
        Console.WriteLine($"partnerCode: {partnerCode}");
        Console.WriteLine($"orderId: {orderId}");
        Console.WriteLine($"requestId: {requestId}");
        Console.WriteLine($"amount: {amount}");
        Console.WriteLine($"orderInfo: {orderInfo}");
        Console.WriteLine($"orderType: {orderType}");
        Console.WriteLine($"transId: {transId}");
        Console.WriteLine($"resultCode: {resultCode}");
        Console.WriteLine($"message: {message}");
        Console.WriteLine($"payType: {payType}");
        Console.WriteLine($"responseTime: {responseTime}");
        Console.WriteLine($"extraData: {extraData}");
        Console.WriteLine($"signature: {signature}");

        // Build raw signature string
        var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
        Console.WriteLine($"Raw Signature String: {rawSignature}");

        var computedSignature = ComputeHmacSha256(rawSignature, secretKey);
        Console.WriteLine($"Computed Signature: {computedSignature}");
        Console.WriteLine($"Received Signature: {signature}");
        Console.WriteLine("------------------------------");

        if (computedSignature == signature && resultCode == "0")
        {
            // Payment successful
            var dbOrderIdStr = orderId.Split('_')[0];
            if (int.TryParse(dbOrderIdStr, out var dbOrderId))
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == dbOrderId);

                if (order != null)
                {
                    order.Status = "Processing";
                    _context.Update(order);
                    await _context.SaveChangesAsync();

                    var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
                    HttpContext.Session.Remove("Cart");
                    HttpContext.Session.Remove("AppliedCoupon");

                    try
                    {
                        string userEmail = "";
                        if (User.Identity != null && User.Identity.IsAuthenticated)
                        {
                            var user = await _userManager.FindByNameAsync(User.Identity.Name ?? "");
                            if (user != null)
                            {
                                userEmail = user.Email ?? "";
                            }
                        }
                        await _emailService.SendOrderReceiptEmailAsync(order, cart, userEmail);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error sending email: " + ex.Message);
                    }

                    return RedirectToAction("CheckoutSuccess", new { id = order.Id });
                }
            }
        }
        
        // If signature validation fails or resultCode != "0", cancel the order
        var cancelOrderIdStr = orderId.Split('_')[0];
        if (int.TryParse(cancelOrderIdStr, out var cancelOrderId))
        {
            var order = await _context.Orders.FindAsync(cancelOrderId);
            if (order != null)
            {
                order.Status = "Cancelled";
                _context.Update(order);
                await _context.SaveChangesAsync();
            }
        }

        string userFriendlyMessage = resultCode == "1006" ? "Thanh toán qua Ví MoMo đã bị hủy bởi khách hàng." : $"Thanh toán MoMo không thành công (Mã lỗi: {resultCode}, {message}).";
        if (computedSignature != signature && resultCode == "0")
        {
            userFriendlyMessage = "Xác thực chữ ký giao dịch MoMo trực tuyến thất bại. Vui lòng liên hệ hỗ trợ.";
        }
        
        TempData["ErrorMessage"] = userFriendlyMessage;
        return RedirectToAction("Checkout");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MomoNotify([FromBody] MomoNotifyRequest request)
    {
        var accessKey = _configuration["Momo:AccessKey"] ?? "klm05TvNBzhg7h7j";
        var secretKey = _configuration["Momo:SecretKey"] ?? "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";

        var rawSignature = $"accessKey={accessKey}&amount={request.Amount}&extraData={request.ExtraData}&message={request.Message}&orderId={request.OrderId}&orderInfo={request.OrderInfo}&partnerCode={request.PartnerCode}&requestId={request.RequestId}&responseTime={request.ResponseTime}&resultCode={request.ResultCode}&transId={request.TransId}";
        var computedSignature = ComputeHmacSha256(rawSignature, secretKey);

        if (computedSignature == request.Signature)
        {
            var dbOrderIdStr = request.OrderId.Split('_')[0];
            if (int.TryParse(dbOrderIdStr, out var dbOrderId))
            {
                var order = await _context.Orders.FindAsync(dbOrderId);
                if (order != null && order.Status == "Unpaid" && request.ResultCode == 0)
                {
                    order.Status = "Processing";
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
            }
        }

        return NoContent();
    }

    public class MomoNotifyRequest
    {
        public string PartnerCode { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string RequestId { get; set; } = "";
        public long Amount { get; set; }
        public string OrderInfo { get; set; } = "";
        public string OrderType { get; set; } = "";
        public string TransId { get; set; } = "";
        public int ResultCode { get; set; }
        public string Message { get; set; } = "";
        public string PayType { get; set; } = "";
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}
