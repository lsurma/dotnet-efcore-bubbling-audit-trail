# dotnet-efcore-bubbling-audit-trail

A .NET 10 application with EF Core demonstrating a configurable bubbling audit trail system for tracking entity modifications.

## Overview

This application showcases an innovative approach to entity audit tracking where changes to dependent entities can "bubble up" to their parent entities. This is useful when you want to track when an aggregate root (like an Order) was last modified, including modifications to its child entities (like OrderItems).

## Key Features

### 1. Dual Timestamp Tracking
Each auditable entity maintains two timestamps:
- **`LastModified`**: Updated only when the entity itself is directly modified
- **`LastModifiedWithDependents`**: Updated when the entity OR its dependent entities are modified

### 2. Configurable Bubbling
The bubbling behavior is fully configurable. You can specify which parent-child relationships should trigger timestamp bubbling.

**Example:**
- `OrderItem` → `Order`: Bubbling enabled (changes to order items update the order)
- `Product` → `OrderItem`: Bubbling disabled (changes to products don't affect order items)

### 3. EF Core Integration
The audit trail is implemented using EF Core's `SaveChanges` override, making it transparent to your application code.

## Solution Structure

```
├── BubblingAuditTrail.Core       # Core library with entities and DbContext
├── BubblingAuditTrail.Demo       # Console application demonstrating the functionality
└── BubblingAuditTrail.Tests      # Unit tests
```

## Quick Start

### Running the Demo

```bash
dotnet run --project BubblingAuditTrail.Demo/BubblingAuditTrail.Demo.csproj
```

### Running Tests

```bash
dotnet test
```

## Usage Example

```csharp
// 1. Configure bubbling relationships
var bubblingConfig = new BubblingConfiguration();
bubblingConfig.ConfigureBubbling<OrderItem, Order>();  // Enable bubbling from OrderItem to Order
// Note: We don't configure Product -> OrderItem, so no bubbling occurs

// 2. Create DbContext with configuration
var options = new DbContextOptionsBuilder<AuditDbContext>()
    .UseInMemoryDatabase("MyDatabase")
    .Options;

using var context = new AuditDbContext(options, bubblingConfig);

// 3. Create and modify entities - timestamps are automatically managed
var order = new Order { CustomerName = "Jan Kowalski" };
context.Orders.Add(order);
await context.SaveChangesAsync();

var orderItem = new OrderItem 
{ 
    OrderId = order.Id,
    ProductId = productId,
    Quantity = 2 
};
context.OrderItems.Add(orderItem);
await context.SaveChangesAsync();

// Order.LastModifiedWithDependents is automatically updated!
```

## Domain Model

```
Order (1) ─────< OrderItem (N) >─────── (1) Product
          bubbling enabled      bubbling disabled
```

- **Order**: Represents a customer order
- **OrderItem**: Line items in an order (links Order to Product)
- **Product**: Product catalog

## Configuration Details

The `BubblingConfiguration` class allows you to define which relationships should trigger bubbling:

```csharp
var config = new BubblingConfiguration();

// Changes to OrderItem will update Order.LastModifiedWithDependents
config.ConfigureBubbling<OrderItem, Order>();

// Changes to Product will NOT update OrderItem
// (not configured, so no bubbling)
```

## Technical Implementation

The bubbling mechanism works by:
1. Intercepting `SaveChanges()` in the `AuditDbContext`
2. Detecting all modified entities
3. Updating `LastModified` and `LastModifiedWithDependents` for directly modified entities
4. Recursively traversing parent relationships based on configuration
5. Updating `LastModifiedWithDependents` for parent entities when configured

## Requirements

- .NET 10
- Entity Framework Core 10.0

## Benefits

1. **Accurate Aggregate Tracking**: Know when an aggregate root was last modified, including its children
2. **Flexible Configuration**: Choose which relationships should bubble
3. **Transparent**: No changes needed to your entity manipulation code
4. **Performance**: Only processes modified entities during SaveChanges

## License

This is a demonstration project.