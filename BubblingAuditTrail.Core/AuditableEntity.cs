namespace BubblingAuditTrail.Core;

/// <summary>
/// Base class for entities that support audit tracking
/// </summary>
public abstract class AuditableEntity : IAuditable
{
    public int Id { get; set; }
    
    public DateTime LastModified { get; set; }
    
    public DateTime LastModifiedWithDependents { get; set; }
}
