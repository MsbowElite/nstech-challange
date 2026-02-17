namespace OrderService.Application.Common;

public class ValidationFailure
{
    public string PropertyName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationFailure> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(List<ValidationFailure> errors) =>
        new() { IsValid = false, Errors = errors };
}
