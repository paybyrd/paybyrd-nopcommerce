using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Paybyrd.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(PaybyrdPaymentDefaults.ConfigurationRouteName,
            "Admin/PaymentPaybyrd/Configure",
            new { controller = "PaymentPaybyrd", action = "Configure" });

        endpointRouteBuilder.MapControllerRoute(PaybyrdPaymentDefaults.HandleOrderPostProcessingRouteName,
            "Admin/PaymentPaybyrd/HandleOrderPostProcessing",
            new { controller = "PaymentPaybyrd", action = "HandleOrderPostProcessing", area = "Admin" });

        endpointRouteBuilder.MapControllerRoute(PaybyrdPaymentDefaults.ValidatePaymentRouteName,
            "Admin/PaymentPaybyrd/ValidatePayment",
            new { controller = "PaymentPaybyrd", action = "ValidatePayment", area = "Admin" });

        endpointRouteBuilder.MapControllerRoute(PaybyrdPaymentDefaults.WebhookRouteName,
            "Plugins/PaymentPaybyrd/Webhook",
            new { controller = "Webhook", action = "ReceiveWebhook" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}