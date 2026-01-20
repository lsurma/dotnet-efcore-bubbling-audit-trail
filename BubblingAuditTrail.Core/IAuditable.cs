namespace BubblingAuditTrail.Core;

/// <summary>
/// Interface for entities that support audit tracking
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Last modification timestamp for this entity (without bubbling)
    /// </summary>
    DateTime LastModified { get; set; }

    /// <summary>
    /// Last modification timestamp including changes from dependent entities (with bubbling)
    /// </summary>
    DateTime LastModifiedWithDependents { get; set; }
}
