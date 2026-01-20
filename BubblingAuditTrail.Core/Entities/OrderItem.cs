namespace BubblingAuditTrail.Core.Entities;

/// <summary>
/// OrderItem entity - represents an item in an order
/// </summary>
public class OrderItem : AuditableEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
