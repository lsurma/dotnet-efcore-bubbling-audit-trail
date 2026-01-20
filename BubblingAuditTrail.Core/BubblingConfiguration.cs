namespace BubblingAuditTrail.Core;

/// <summary>
/// Configuration for audit trail bubbling behavior
/// </summary>
public class BubblingConfiguration
{
    private readonly HashSet<(Type childType, Type parentType)> _bubblingRelationships = new();

    /// <summary>
    /// Configure that changes in child entity should bubble to parent entity
    /// </summary>
    public void ConfigureBubbling<TChild, TParent>()
        where TChild : IAuditable
        where TParent : IAuditable
    {
        _bubblingRelationships.Add((typeof(TChild), typeof(TParent)));
    }

    /// <summary>
    /// Check if changes in child should bubble to parent
    /// </summary>
    public bool ShouldBubble(Type childType, Type parentType)
    {
        return _bubblingRelationships.Contains((childType, parentType));
    }
}
