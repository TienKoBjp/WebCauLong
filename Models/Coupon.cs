using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetMvcApp.Models;

public class Coupon
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    [Required]
    [StringLength(20)]
    public string DiscountType { get; set; } = "Fixed"; // "Fixed" or "Percentage"

    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(30);

    public bool IsActive { get; set; } = true;

    public int UsageLimit { get; set; } = 100;

    public int UsageCount { get; set; } = 0;
}
