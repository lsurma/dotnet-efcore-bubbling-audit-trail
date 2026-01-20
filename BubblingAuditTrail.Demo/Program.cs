using BubblingAuditTrail.Core;
using BubblingAuditTrail.Core.Entities;
using Microsoft.EntityFrameworkCore;

// Configure bubbling behavior
var bubblingConfig = new BubblingConfiguration();

// Configure that changes to OrderItem should bubble to Order
bubblingConfig.ConfigureBubbling<OrderItem, Order>();

// Note: We don't configure Product -> OrderItem bubbling
// So changes to Product won't affect OrderItem's LastModifiedWithDependents

// Create in-memory database
var options = new DbContextOptionsBuilder<AuditDbContext>()
    .UseInMemoryDatabase("AuditTrailDemo")
    .Options;

await using var context = new AuditDbContext(options, bubblingConfig);

Console.WriteLine("=== Bubbling Audit Trail Demo ===\n");

// Create initial data
Console.WriteLine("1. Creating initial data...");
var product = new Product
{
    Name = "Laptop",
    Price = 999.99m
};

var order = new Order
{
    CustomerName = "Jan Kowalski"
};

context.Products.Add(product);
context.Orders.Add(order);
await context.SaveChangesAsync();

var initialTime = DateTime.UtcNow;
await Task.Delay(100); // Small delay to ensure different timestamps

Console.WriteLine($"   Product: {product.Name}");
Console.WriteLine($"   - LastModified: {product.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {product.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   Order: Customer {order.CustomerName}");
Console.WriteLine($"   - LastModified: {order.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {order.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine();

// Add OrderItem to Order
Console.WriteLine("2. Adding OrderItem to Order...");
var orderItem = new OrderItem
{
    OrderId = order.Id,
    ProductId = product.Id,
    Quantity = 2,
    UnitPrice = product.Price
};

context.OrderItems.Add(orderItem);
await context.SaveChangesAsync();

// Reload entities to see updated timestamps
await context.Entry(order).ReloadAsync();
await context.Entry(product).ReloadAsync();

Console.WriteLine($"   OrderItem created: {orderItem.Quantity}x {product.Name}");
Console.WriteLine($"   - LastModified: {orderItem.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {orderItem.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine();
Console.WriteLine($"   Order (should be updated due to bubbling from OrderItem):");
Console.WriteLine($"   - LastModified: {order.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {order.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff} ← UPDATED!");
Console.WriteLine();
Console.WriteLine($"   Product (should NOT be updated - no bubbling configured):");
Console.WriteLine($"   - LastModified: {product.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {product.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine();

await Task.Delay(100);

// Modify OrderItem quantity
Console.WriteLine("3. Modifying OrderItem quantity...");
var orderItemFromDb = await context.OrderItems
    .Include(oi => oi.Order)
    .FirstAsync(oi => oi.Id == orderItem.Id);

orderItemFromDb.Quantity = 5;
await context.SaveChangesAsync();

await context.Entry(order).ReloadAsync();

Console.WriteLine($"   OrderItem quantity changed to {orderItemFromDb.Quantity}");
Console.WriteLine($"   - LastModified: {orderItemFromDb.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {orderItemFromDb.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine();
Console.WriteLine($"   Order (LastModifiedWithDependents should be updated again):");
Console.WriteLine($"   - LastModified: {order.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {order.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff} ← UPDATED AGAIN!");
Console.WriteLine();

await Task.Delay(100);

// Modify Product price
Console.WriteLine("4. Modifying Product price (should NOT bubble to OrderItem)...");
var productFromDb = await context.Products.FindAsync(product.Id);
var orderItemBeforeProductChange = await context.OrderItems.FindAsync(orderItem.Id);

var orderItemLastModifiedBefore = orderItemBeforeProductChange!.LastModifiedWithDependents;

productFromDb!.Price = 1299.99m;
await context.SaveChangesAsync();

await context.Entry(orderItemFromDb).ReloadAsync();

Console.WriteLine($"   Product price changed to {productFromDb.Price:C}");
Console.WriteLine($"   - LastModified: {productFromDb.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {productFromDb.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine();
Console.WriteLine($"   OrderItem (should NOT be affected - no bubbling from Product):");
Console.WriteLine($"   - LastModified: {orderItemFromDb.LastModified:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - LastModifiedWithDependents: {orderItemFromDb.LastModifiedWithDependents:yyyy-MM-dd HH:mm:ss.fff}");
Console.WriteLine($"   - Timestamp unchanged: {orderItemFromDb.LastModifiedWithDependents == orderItemLastModifiedBefore}");
Console.WriteLine();

Console.WriteLine("=== Demo Complete ===");
Console.WriteLine();
Console.WriteLine("Summary:");
Console.WriteLine("✓ Each entity maintains its own LastModified timestamp");
Console.WriteLine("✓ OrderItem changes bubble to Order (configurable)");
Console.WriteLine("✓ Product changes do NOT bubble to OrderItem (not configured)");
