using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;
    public List<Product> RelatedProducts { get; set; } = new();
}
