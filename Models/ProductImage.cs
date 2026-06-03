using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetMvcApp.Models;

public class ProductImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ImagePath { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    // Foreign Key for Product
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
