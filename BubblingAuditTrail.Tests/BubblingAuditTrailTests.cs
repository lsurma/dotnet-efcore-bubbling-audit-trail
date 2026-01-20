using BubblingAuditTrail.Core;
using BubblingAuditTrail.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BubblingAuditTrail.Tests;

public class BubblingAuditTrailTests
{
    private AuditDbContext CreateContext(BubblingConfiguration? config = null)
    {
        var bubblingConfig = config ?? new BubblingConfiguration();
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use unique DB for each test
            .Options;

        return new AuditDbContext(options, bubblingConfig);
    }

    [Fact]
    public async Task EntityCreation_ShouldSetBothTimestamps()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var product = new Product { Name = "Test Product", Price = 100 };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, product.LastModified);
        Assert.NotEqual(default, product.LastModifiedWithDependents);
        Assert.Equal(product.LastModified, product.LastModifiedWithDependents);
    }

    [Fact]
    public async Task EntityUpdate_ShouldUpdateBothTimestamps()
    {
        // Arrange
        await using var context = CreateContext();
        var product = new Product { Name = "Test Product", Price = 100 };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var originalLastModified = product.LastModified;
        await Task.Delay(10);

        // Act
        product.Price = 150;
        await context.SaveChangesAsync();

        // Assert
        Assert.True(product.LastModified > originalLastModified);
        Assert.Equal(product.LastModified, product.LastModifiedWithDependents);
    }

    [Fact]
    public async Task BubblingEnabled_OrderItemChange_ShouldUpdateOrderWithDependents()
    {
        // Arrange
        var config = new BubblingConfiguration();
        config.ConfigureBubbling<OrderItem, Order>();

        await using var context = CreateContext(config);

        var order = new Order { CustomerName = "Test Customer" };
        var product = new Product { Name = "Test Product", Price = 100 };
        context.Orders.Add(order);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 100
        };
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var orderLastModifiedBefore = order.LastModified;
        var orderLastModifiedWithDependentsBefore = order.LastModifiedWithDependents;
        await Task.Delay(10);

        // Act
        var orderItemFromDb = await context.OrderItems
            .Include(oi => oi.Order)
            .FirstAsync(oi => oi.Id == orderItem.Id);
        orderItemFromDb.Quantity = 5;
        await context.SaveChangesAsync();

        // Reload order to get updated values
        await context.Entry(order).ReloadAsync();

        // Assert
        Assert.Equal(orderLastModifiedBefore, order.LastModified); // LastModified should not change
        Assert.True(order.LastModifiedWithDependents > orderLastModifiedWithDependentsBefore); // Should bubble
    }

    [Fact]
    public async Task BubblingNotConfigured_ProductChange_ShouldNotUpdateOrderItem()
    {
        // Arrange
        var config = new BubblingConfiguration();
        // Note: We don't configure Product -> OrderItem bubbling

        await using var context = CreateContext(config);

        var order = new Order { CustomerName = "Test Customer" };
        var product = new Product { Name = "Test Product", Price = 100 };
        context.Orders.Add(order);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 100
        };
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var orderItemLastModifiedWithDependentsBefore = orderItem.LastModifiedWithDependents;
        await Task.Delay(10);

        // Act
        var productFromDb = await context.Products.FindAsync(product.Id);
        productFromDb!.Price = 150;
        await context.SaveChangesAsync();

        // Reload orderItem
        await context.Entry(orderItem).ReloadAsync();

        // Assert
        Assert.Equal(orderItemLastModifiedWithDependentsBefore, orderItem.LastModifiedWithDependents); // Should not bubble
    }

    [Fact]
    public async Task LastModified_ShouldOnlyChangeWhenEntityIsDirectlyModified()
    {
        // Arrange
        var config = new BubblingConfiguration();
        config.ConfigureBubbling<OrderItem, Order>();

        await using var context = CreateContext(config);

        var order = new Order { CustomerName = "Test Customer" };
        var product = new Product { Name = "Test Product", Price = 100 };
        context.Orders.Add(order);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 100
        };
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var orderLastModifiedBefore = order.LastModified;
        await Task.Delay(10);

        // Act - Modify OrderItem (should bubble to Order.LastModifiedWithDependents but not LastModified)
        var orderItemFromDb = await context.OrderItems
            .Include(oi => oi.Order)
            .FirstAsync(oi => oi.Id == orderItem.Id);
        orderItemFromDb.Quantity = 5;
        await context.SaveChangesAsync();

        await context.Entry(order).ReloadAsync();

        // Assert
        Assert.Equal(orderLastModifiedBefore, order.LastModified); // Should NOT change
        Assert.True(order.LastModifiedWithDependents > orderLastModifiedBefore); // Should change
    }

    [Fact]
    public async Task AddingNewOrderItem_ShouldBubbleToOrder()
    {
        // Arrange
        var config = new BubblingConfiguration();
        config.ConfigureBubbling<OrderItem, Order>();

        await using var context = CreateContext(config);

        var order = new Order { CustomerName = "Test Customer" };
        var product = new Product { Name = "Test Product", Price = 100 };
        context.Orders.Add(order);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var orderLastModifiedWithDependentsBefore = order.LastModifiedWithDependents;
        await Task.Delay(10);

        // Act
        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 100
        };
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        await context.Entry(order).ReloadAsync();

        // Assert
        Assert.True(order.LastModifiedWithDependents > orderLastModifiedWithDependentsBefore);
    }
}
