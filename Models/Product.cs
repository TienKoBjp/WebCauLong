namespace AspNetMvcApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Rating { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string StockStatus { get; set; } = "InStock"; // "InStock", "LowStock", "OutOfStock"
    public bool IsFeatured { get; set; }

    // Foreign Key for Category
    public int CategoryId { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
