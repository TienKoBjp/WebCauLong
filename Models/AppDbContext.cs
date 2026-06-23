using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Coupon> Coupons { get; set; } = null!;
    public DbSet<ProductReview> ProductReviews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product-ProductReview One-to-Many Relationship
        modelBuilder.Entity<ProductReview>()
            .HasOne(pr => pr.Product)
            .WithMany(p => p.ProductReviews)
            .HasForeignKey(pr => pr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Category-Product One-to-Many Relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Vợt cầu lông" },
            new Category { Id = 2, Name = "Giày cầu lông" },
            new Category { Id = 3, Name = "Phụ kiện & Áo" }
        );

        // Seed 10 Sample Products using the badminton images in wwwroot/imager
        modelBuilder.Entity<Product>().HasData(
            new Product 
            { 
                Id = 1, 
                Name = "Yonex Astrox 100 ZZ", 
                CategoryId = 1, 
                Price = 4200000m, 
                Rating = 4.9, 
                Description = "Vợt cầu lông cao cấp hỗ trợ tấn công mạnh mẽ với thân vợt siêu cứng và đầu vợt nặng, sự lựa chọn của các nhà vô địch.", 
                ImagePath = "yonex_astrox_100zz.png", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 2, 
                Name = "Victor Thruster Ryuga II", 
                CategoryId = 1, 
                Price = 3500000m, 
                Rating = 4.7, 
                Description = "Dòng vợt tấn công uy lực với công nghệ Free Core cải tiến tay cầm, giúp tạo ra những cú đập cầu cắm và nhanh.", 
                ImagePath = "victor_ryuga.png", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 3, 
                Name = "Li-Ning Tectonic 9", 
                CategoryId = 1, 
                Price = 3800000m, 
                Rating = 4.8, 
                Description = "Sử dụng sợi carbon T1100G siêu đàn hồi, tối ưu hóa khả năng phản tạt và tấn công linh hoạt trên mọi góc sân.", 
                ImagePath = "lining_tectonic9.png", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 4, 
                Name = "Yonex Power Cushion 65Z3", 
                CategoryId = 2, 
                Price = 2900000m, 
                Rating = 4.9, 
                Description = "Giày cầu lông huyền thoại tích hợp đệm Power Cushion+ hấp thụ chấn động cực tốt và tăng độ bám sân vượt trội.", 
                ImagePath = "yonex_65z3.png", 
                StockStatus = "InStock", 
                IsFeatured = true 
            },
            new Product 
            { 
                Id = 5, 
                Name = "Victor P9200II", 
                CategoryId = 2, 
                Price = 2500000m, 
                Rating = 4.6, 
                Description = "Độ bền bỉ tối đa và chống lật cổ chân hiệu quả với công nghệ đỡ gót đệm carbon thế hệ mới.", 
                ImagePath = "victor_p9200.png", 
                StockStatus = "LowStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 6, 
                Name = "Li-Ning Ranger VI", 
                CategoryId = 2, 
                Price = 2200000m, 
                Rating = 4.5, 
                Description = "Thiết kế trẻ trung, thông thoáng khí tốt và hỗ trợ di chuyển linh hoạt tối đa cho người chơi phong trào.", 
                ImagePath = "lining_ranger.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 7, 
                Name = "Hộp Cầu Lông Yonex AS-50", 
                CategoryId = 3, 
                Price = 450000m, 
                Rating = 4.8, 
                Description = "Hộp 12 quả cầu lông vũ cao cấp chuẩn thi đấu quốc tế, độ bền cực cao và đường bay cực kỳ ổn định.", 
                ImagePath = "yonex_as50.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 8, 
                Name = "Bao Vợt Yonex Pro Bag", 
                CategoryId = 3, 
                Price = 1800000m, 
                Rating = 4.6, 
                Description = "Túi đựng vợt cầu lông chuyên nghiệp với ngăn chống nhiệt bảo vệ vợt tối ưu và nhiều ngăn chứa đồ rộng rãi.", 
                ImagePath = "yonex_pro_bag.png", 
                StockStatus = "LowStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 9, 
                Name = "Quấn Cán Yonex AC102EX", 
                CategoryId = 3, 
                Price = 50000m, 
                Rating = 4.3, 
                Description = "Quấn cán cao su cao cấp siêu bám tay, thấm hút mồ hôi hiệu quả và tăng cảm giác cầm vợt êm ái.", 
                ImagePath = "yonex_grip.png", 
                StockStatus = "OutOfStock", 
                IsFeatured = false 
            },
            new Product 
            { 
                Id = 10, 
                Name = "Áo Thun Cầu Lông Yonex", 
                CategoryId = 3, 
                Price = 350000m, 
                Rating = 4.5, 
                Description = "Áo thun thể thao cầu lông chất liệu cao cấp siêu thoáng mát, thấm hút mồ hôi nhanh và thiết kế năng động.", 
                ImagePath = "yonex_shirt.png", 
                StockStatus = "InStock", 
                IsFeatured = false 
            }
        );

        // Configure Product-ProductImage One-to-Many Relationship
        modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.ProductImages)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed Product Images for Details Gallery (3 images per product)
        modelBuilder.Entity<ProductImage>().HasData(
            // Product 1: Yonex Astrox 100 ZZ
            new ProductImage { Id = 1, ProductId = 1, ImagePath = "yonex_astrox_100zz.png", DisplayOrder = 0 },
            new ProductImage { Id = 2, ProductId = 1, ImagePath = "vot-cau-long-yonex-astrox-100zz-kurenai-do-new-2021-5_c525b0.webp", DisplayOrder = 1 },
            new ProductImage { Id = 3, ProductId = 1, ImagePath = "vot-cau-long-yonex-astrox-100zz-kurenai-do-new-2021-5_f950b4.webp", DisplayOrder = 2 },

            // Product 2: Victor Thruster Ryuga II
            new ProductImage { Id = 4, ProductId = 2, ImagePath = "victor_ryuga.png", DisplayOrder = 0 },
            new ProductImage { Id = 5, ProductId = 2, ImagePath = "vot-cau-long-victor-mjolnir-metallic-limited-2024-ma-taiwan-1_1716431827_3d72c0.webp", DisplayOrder = 1 },
            new ProductImage { Id = 6, ProductId = 2, ImagePath = "tai_xuong__13__8fee50d95d54432fa3de9c82cb524f25_21c56f.webp", DisplayOrder = 2 },

            // Product 3: Li-Ning Tectonic 9
            new ProductImage { Id = 7, ProductId = 3, ImagePath = "lining_tectonic9.png", DisplayOrder = 0 },
            new ProductImage { Id = 8, ProductId = 3, ImagePath = "vot-cau-long-lining-axforce-100-black-golden-chinh-hang-3_b6db4a.webp", DisplayOrder = 1 },
            new ProductImage { Id = 9, ProductId = 3, ImagePath = "vot-cau-long-yonex-nanoflare-1000-game-chinh-hang-1_1ce6b6.jpg", DisplayOrder = 2 },

            // Product 4: Yonex Power Cushion 65Z3
            new ProductImage { Id = 10, ProductId = 4, ImagePath = "yonex_65z3.png", DisplayOrder = 0 },
            new ProductImage { Id = 11, ProductId = 4, ImagePath = "giay-cau-long-yonex-shb-65z4-men-trang-2025-chinh-hang_1736970262_1a4dea.webp", DisplayOrder = 1 },
            new ProductImage { Id = 12, ProductId = 4, ImagePath = "tai_xuong__14__4e41637d74a94e0ab5e21bfb45e94c66_87d39d.webp", DisplayOrder = 2 },

            // Product 5: Victor P9200II
            new ProductImage { Id = 13, ProductId = 5, ImagePath = "victor_p9200.png", DisplayOrder = 0 },
            new ProductImage { Id = 14, ProductId = 5, ImagePath = "tai_xuong__14__4e41637d74a94e0ab5e21bfb45e94c66_87d39d.webp", DisplayOrder = 1 },
            new ProductImage { Id = 15, ProductId = 5, ImagePath = "giay-cau-long-shi-yuqi-lining-ayar015-2-noi-dia-trung-5_cdb716.webp", DisplayOrder = 2 },

            // Product 6: Li-Ning Ranger VI
            new ProductImage { Id = 16, ProductId = 6, ImagePath = "lining_ranger.png", DisplayOrder = 0 },
            new ProductImage { Id = 17, ProductId = 6, ImagePath = "giay-cau-long-shi-yuqi-lining-ayar015-2-noi-dia-trung-5_cdb716.webp", DisplayOrder = 1 },
            new ProductImage { Id = 18, ProductId = 6, ImagePath = "giay-cau-long-yonex-shb-65z4-men-trang-2025-chinh-hang_1736970262_1a4dea.webp", DisplayOrder = 2 },

            // Product 7: Hộp Cầu Lông Yonex AS-50
            new ProductImage { Id = 19, ProductId = 7, ImagePath = "yonex_as50.png", DisplayOrder = 0 },
            new ProductImage { Id = 20, ProductId = 7, ImagePath = "ong-cau-long-yonex-as50-1_3560e6.webp", DisplayOrder = 1 },
            new ProductImage { Id = 21, ProductId = 7, ImagePath = "yonex_as50_detail.png", DisplayOrder = 2 },

            // Product 8: Bao Vợt Yonex Pro Bag
            new ProductImage { Id = 22, ProductId = 8, ImagePath = "yonex_pro_bag.png", DisplayOrder = 0 },
            new ProductImage { Id = 23, ProductId = 8, ImagePath = "yonex_pro_bag_side.png", DisplayOrder = 1 },
            new ProductImage { Id = 24, ProductId = 8, ImagePath = "yonex_pro_bag_inside.png", DisplayOrder = 2 },

            // Product 9: Quấn Cán Yonex AC102EX
            new ProductImage { Id = 25, ProductId = 9, ImagePath = "yonex_grip.png", DisplayOrder = 0 },
            new ProductImage { Id = 26, ProductId = 9, ImagePath = "tai_xuong__11__3160c4d902594364bb52dcf8d27f7dba_1024x1024-768x768_722938.webp", DisplayOrder = 1 },
            new ProductImage { Id = 27, ProductId = 9, ImagePath = "yonex_grip_detail.png", DisplayOrder = 2 },

            // Product 10: Áo Thun Cầu Lông Yonex
            new ProductImage { Id = 28, ProductId = 10, ImagePath = "yonex_shirt.png", DisplayOrder = 0 },
            new ProductImage { Id = 29, ProductId = 10, ImagePath = "ao-cau-long-yonex-2097-do-nam_1712176975_f05d63.webp", DisplayOrder = 1 },
            new ProductImage { Id = 30, ProductId = 10, ImagePath = "abju029-1_16ba38252602426590bcccf5cd7a735a-768x768_25168b.webp", DisplayOrder = 2 }
        );
    }
}
