namespace OrderService.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string message) : base(message)
    {
    }
}

public class InvalidOrderStatusException : DomainException
{
    public InvalidOrderStatusException(string message) : base(message)
    {
    }
}
