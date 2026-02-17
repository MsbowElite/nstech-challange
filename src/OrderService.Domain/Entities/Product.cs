namespace OrderService.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int AvailableQuantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { }

    public Product(string name, decimal unitPrice, int availableQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required", nameof(name));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        if (availableQuantity < 0)
            throw new ArgumentException("Available quantity cannot be negative", nameof(availableQuantity));

        Id = Guid.NewGuid();
        Name = name;
        UnitPrice = unitPrice;
        AvailableQuantity = availableQuantity;
        CreatedAt = DateTime.UtcNow;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (AvailableQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}");

        AvailableQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        AvailableQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(newPrice));

        UnitPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
