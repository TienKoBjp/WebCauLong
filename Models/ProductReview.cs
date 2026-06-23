using System;

namespace AspNetMvcApp.Models;

public class ProductReview
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; } // 1 to 5 stars
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Order reference to ensure verification and query uniqueness for reviews per order
    public int OrderId { get; set; }
}
