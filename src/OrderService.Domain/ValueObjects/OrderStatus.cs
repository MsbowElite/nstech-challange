namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Rich enumeration representing the possible states of an Order.
/// This is a value object that encapsulates order status behavior.
/// </summary>
public sealed class OrderStatus : IEquatable<OrderStatus>, IComparable<OrderStatus>
{
    public static readonly OrderStatus Draft = new(0, nameof(Draft), "Order is in draft state");
    public static readonly OrderStatus Placed = new(1, nameof(Placed), "Order has been placed");
    public static readonly OrderStatus Confirmed = new(2, nameof(Confirmed), "Order has been confirmed");
    public static readonly OrderStatus Canceled = new(3, nameof(Canceled), "Order has been canceled");

    public int Value { get; }
    public string Name { get; }
    public string Description { get; }

    private OrderStatus(int value, string name, string description)
    {
        Value = value;
        Name = name;
        Description = description;
    }

    public static OrderStatus FromValue(int value) => value switch
    {
        0 => Draft,
        1 => Placed,
        2 => Confirmed,
        3 => Canceled,
        _ => throw new ArgumentException($"Invalid order status value: {value}")
    };

    public static OrderStatus FromName(string name) => name switch
    {
        nameof(Draft) => Draft,
        nameof(Placed) => Placed,
        nameof(Confirmed) => Confirmed,
        nameof(Canceled) => Canceled,
        _ => throw new ArgumentException($"Invalid order status name: {name}")
    };

    public static IEnumerable<OrderStatus> GetAll() => new[] { Draft, Placed, Confirmed, Canceled };

    /// <summary>
    /// Determines if an order can transition to the Confirmed status.
    /// Only Placed orders can be confirmed.
    /// </summary>
    public bool CanTransitionToConfirmed() => this == Placed;

    /// <summary>
    /// Determines if an order can transition to the Canceled status.
    /// Only Placed or Confirmed orders can be canceled.
    /// </summary>
    public bool CanTransitionToCanceled() => this == Placed || this == Confirmed;

    /// <summary>
    /// Determines if the order is in a terminal state (cannot be modified).
    /// </summary>
    public bool IsTerminal() => this == Canceled;

    public override bool Equals(object? obj) => Equals(obj as OrderStatus);

    public bool Equals(OrderStatus? other) => other is not null && Value == other.Value;

    public int CompareTo(OrderStatus? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Name;
}
