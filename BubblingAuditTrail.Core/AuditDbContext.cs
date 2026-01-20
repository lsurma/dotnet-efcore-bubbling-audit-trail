using BubblingAuditTrail.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BubblingAuditTrail.Core;

/// <summary>
/// Database context with bubbling audit trail support
/// </summary>
public class AuditDbContext : DbContext
{
    private readonly BubblingConfiguration _bubblingConfig;

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public AuditDbContext(DbContextOptions<AuditDbContext> options, BubblingConfiguration bubblingConfig)
        : base(options)
    {
        _bubblingConfig = bubblingConfig;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override int SaveChanges()
    {
        ApplyAuditTrail();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTrail();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTrail()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        // First pass: Update LastModified for all modified entities
        foreach (var entry in entries)
        {
            entry.Entity.LastModified = now;
            entry.Entity.LastModifiedWithDependents = now;
        }

        // Second pass: Bubble changes to parent entities
        var processedEntities = new HashSet<object>();
        
        foreach (var entry in entries)
        {
            BubbleChangesToParents(entry.Entity, now, processedEntities);
        }
    }

    private void BubbleChangesToParents(IAuditable entity, DateTime timestamp, HashSet<object> processedEntities)
    {
        if (!processedEntities.Add(entity))
        {
            return; // Already processed
        }

        var entityType = entity.GetType();
        var entry = Entry(entity);
        
        // Find all navigation properties that point to parent entities (many-to-one relationships)
        var navigationProperties = entry.Navigations
            .Where(n => n.Metadata is Microsoft.EntityFrameworkCore.Metadata.INavigation nav && !nav.IsCollection)
            .ToList();

        foreach (var navigation in navigationProperties)
        {
            if (navigation.CurrentValue is IAuditable parentEntity)
            {
                var parentType = parentEntity.GetType();
                
                // Check if we should bubble from child to parent
                if (_bubblingConfig.ShouldBubble(entityType, parentType))
                {
                    // Update parent's LastModifiedWithDependents
                    if (parentEntity.LastModifiedWithDependents < timestamp)
                    {
                        parentEntity.LastModifiedWithDependents = timestamp;
                        
                        // Mark parent as modified if it's not already being tracked
                        var parentEntry = Entry(parentEntity);
                        if (parentEntry.State == EntityState.Unchanged)
                        {
                            parentEntry.State = EntityState.Modified;
                        }
                        
                        // Continue bubbling up the chain
                        BubbleChangesToParents(parentEntity, timestamp, processedEntities);
                    }
                }
            }
        }
    }
}
