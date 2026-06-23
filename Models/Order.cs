using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetMvcApp.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string ShippingAddress { get; set; } = null!;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingFee { get; set; } = 0;

    // Status: Pending, Processing, Shipped, Delivered, Cancelled
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "COD";

    [StringLength(50)]
    public string? CouponCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    // Navigation property
    public List<OrderItem> OrderItems { get; set; } = new();
}
