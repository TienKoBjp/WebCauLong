using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation property for products under this category
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
