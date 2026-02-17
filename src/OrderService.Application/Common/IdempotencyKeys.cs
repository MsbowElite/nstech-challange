namespace OrderService.Application.Common;

public static class IdempotencyKeys
{
    private const string ConfirmPrefix = "confirm";
    private const string CancelPrefix = "cancel";

    public static string ForConfirm(Guid orderId, string requestKey)
    {
        return $"{ConfirmPrefix}-{orderId}-{requestKey}";
    }

    public static string ForCancel(Guid orderId, string requestKey)
    {
        return $"{CancelPrefix}-{orderId}-{requestKey}";
    }
}
