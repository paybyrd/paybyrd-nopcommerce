using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Paybyrd;

/// <summary>
/// Represents settings of "Check money order" payment plugin
/// </summary>
public class PaybyrdPaymentSettings : ISettings
{
    /// <summary>
    /// Gets or sets which API Key should be used
    /// </summary>
    public bool EnableTestMode { get; set; }

    /// <summary>
    /// Gets or sets the hosted form background color
    /// </summary>
    public string HFBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the hosted form payment box background color
    /// </summary>
    public string HFPaymentBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the hosted form primary color
    /// </summary>
    public string HFPrimaryColor { get; set; }

    /// <summary>
    /// Gets or sets the hosted form payment box text color
    /// </summary>
    public string HFTextColor { get; set; }

    /// <summary>
    /// Gets or sets user's Live Api Key
    /// </summary>
    public string LiveApiKey { get; set; }

    /// <summary>
    /// Gets or sets the order status to be set after a successful payment
    /// </summary>
    public PostPaymentOrderStatus PostPaymentOrderStatus { get; set; }

    /// <summary>
    /// Gets or sets user's Test Api Key
    /// </summary>
    public string TestApiKey { get; set; }

    /// <summary>
    /// <summary>
    /// Gets or sets webhook id
    /// </summary>
    public string WebhookId { get; set; }
}