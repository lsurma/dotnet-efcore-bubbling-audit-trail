namespace BubblingAuditTrail.Core.Entities;

/// <summary>
/// Product entity
/// </summary>
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
