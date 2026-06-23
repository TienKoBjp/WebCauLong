using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AspNetMvcApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Enable Distributed Memory Cache and Session Services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register ChatbotService & EmailService
builder.Services.AddSingleton<AspNetMvcApp.Services.ChatbotService>();
builder.Services.AddScoped<AspNetMvcApp.Services.EmailService>();

// Register DbContext with SQL Server Connection String
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings (relaxed for development)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder-client-id";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder-client-secret";
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "placeholder-app-id";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "placeholder-app-secret";
    });

var app = builder.Build();

// Seed Roles and Default Users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        // Tạo lại bảng Orders, OrderItems, Coupons và ProductReviews cho đúng schema
        var sql = @"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='ProductReviews' AND xtype='U') DROP TABLE ProductReviews;
            IF EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U') DROP TABLE OrderItems;
            IF EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U') DROP TABLE Orders;
            IF EXISTS (SELECT * FROM sysobjects WHERE name='Coupons' AND xtype='U') DROP TABLE Coupons;

            CREATE TABLE Coupons (
                Id int IDENTITY(1,1) PRIMARY KEY,
                Code nvarchar(50) NOT NULL UNIQUE,
                DiscountValue decimal(18,2) NOT NULL,
                DiscountType nvarchar(20) NOT NULL,
                ExpiryDate datetime2 NOT NULL,
                IsActive bit NOT NULL DEFAULT 1,
                UsageLimit int NOT NULL DEFAULT 100,
                UsageCount int NOT NULL DEFAULT 0
            );

            CREATE TABLE Orders (
                Id int IDENTITY(1,1) PRIMARY KEY,
                UserId nvarchar(max) NOT NULL,
                CustomerName nvarchar(100) NOT NULL,
                PhoneNumber nvarchar(20) NOT NULL,
                ShippingAddress nvarchar(255) NOT NULL,
                OrderDate datetime2 NOT NULL,
                TotalAmount decimal(18,2) NOT NULL,
                ShippingFee decimal(18,2) NOT NULL DEFAULT 0,
                Status nvarchar(50) NOT NULL,
                PaymentMethod nvarchar(50) NOT NULL DEFAULT 'COD',
                CouponCode nvarchar(50) NULL,
                DiscountAmount decimal(18,2) NOT NULL DEFAULT 0
            );

            CREATE TABLE OrderItems (
                Id int IDENTITY(1,1) PRIMARY KEY,
                OrderId int NOT NULL FOREIGN KEY REFERENCES Orders(Id) ON DELETE CASCADE,
                ProductId int NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                Quantity int NOT NULL,
                UnitPrice decimal(18,2) NOT NULL
            );

            CREATE TABLE ProductReviews (
                Id int IDENTITY(1,1) PRIMARY KEY,
                ProductId int NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                UserId nvarchar(450) NOT NULL,
                CustomerName nvarchar(256) NOT NULL,
                Rating int NOT NULL,
                Comment nvarchar(max) NOT NULL,
                CreatedAt datetime2 NOT NULL,
                OrderId int NOT NULL DEFAULT 0
            );

            -- Nạp mã giảm giá mẫu
            INSERT INTO Coupons (Code, DiscountValue, DiscountType, ExpiryDate, IsActive, UsageLimit, UsageCount)
            VALUES 
            ('YONEX100', 100000.00, 'Fixed', DATEADD(day, 30, GETDATE()), 1, 100, 0),
            ('KM50', 50000.00, 'Fixed', DATEADD(day, 30, GETDATE()), 1, 100, 0),
            ('GIAM10', 10.00, 'Percentage', DATEADD(day, 30, GETDATE()), 1, 100, 0);
        ";
        context.Database.ExecuteSqlRaw(sql);

        // Tự động xóa các sản phẩm không có hình ảnh thực tế trong thư mục
        var webRootPath = app.Environment.WebRootPath;
        if (!string.IsNullOrEmpty(webRootPath))
        {
            var imageFolder = Path.Combine(webRootPath, "product", "images");
            if (System.IO.Directory.Exists(imageFolder))
            {
                var products = context.Products.ToList();
                var toDelete = products.Where(p => !System.IO.File.Exists(Path.Combine(imageFolder, p.ImagePath))).ToList();
                if (toDelete.Any())
                {
                    context.Products.RemoveRange(toDelete);
                    context.SaveChanges();
                }
            }
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Create Roles
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin user
        var adminEmail = "admin@nhom10.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Tien Admin user
        var tienAdminEmail = "admin@tien.com";
        if (await userManager.FindByEmailAsync(tienAdminEmail) == null)
        {
            var adminUser = new IdentityUser
            {
                UserName = tienAdminEmail,
                Email = tienAdminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed requested Admin user
        var requestedAdminEmail = "tienbip0506@gmail.com";
        if (await userManager.FindByEmailAsync(requestedAdminEmail) == null)
        {
            var adminUser = new IdentityUser
            {
                UserName = requestedAdminEmail,
                Email = requestedAdminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Tienbui124@");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed regular User
        var userEmail = "user@nhom10.com";
        if (await userManager.FindByEmailAsync(userEmail) == null)
        {
            var regularUser = new IdentityUser
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(regularUser, "User@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, "User");
            }
        }
        
        // Initialize Chatbot data
        var chatbot = app.Services.GetRequiredService<AspNetMvcApp.Services.ChatbotService>();
        chatbot.Initialize();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi tạo hoặc khởi tạo dữ liệu cho Database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Area routing (must be before default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
