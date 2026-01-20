namespace BubblingAuditTrail.Core.Entities;

/// <summary>
/// Order entity
/// </summary>
public class Order : AuditableEntity
{
    public string CustomerName { get; set; } = string.Empty;
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
