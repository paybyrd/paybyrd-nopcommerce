namespace Nop.Plugin.Payments.Paybyrd;

/// <summary>
/// Defines what order status to be set after a successful payment
/// </summary>
public enum PostPaymentOrderStatus
{
    /// <summary>
    /// Processing
    /// </summary>
    Processing = 0,

    /// <summary>
    /// Completed
    /// </summary>
    Completed = 1,
}