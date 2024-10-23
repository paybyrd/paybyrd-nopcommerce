using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Paybyrd.Components;
using Nop.Plugin.Payments.Paybyrd.Localization;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Plugin.Payments.Paybyrd.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Nop.Core.Domain.Payments;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Linq;
using System.Text;
using Nop.Services.Messages;

namespace Nop.Plugin.Payments.Paybyrd;

/// <summary>
/// Paybyrd payment processor
/// </summary>
public class PaybyrdPaymentProcessor : BasePlugin, IPaymentMethod
{
    #region Fields

    protected readonly PaybyrdPaymentSettings _paybyrdPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IWebHelper _webHelper;
    protected readonly IOrderService _orderService;
    protected readonly IUrlHelperFactory _urlHelperFactory;
    protected readonly IActionContextAccessor _actionContextAccessor;
    protected readonly IHttpContextAccessor _httpContextAccessor;

    #endregion

    #region Ctor

    public PaybyrdPaymentProcessor(PaybyrdPaymentSettings paybyrdPaymentSettings,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper,
        IOrderService orderService,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        IHttpContextAccessor httpContextAccessor)
    {
        _paybyrdPaymentSettings = paybyrdPaymentSettings;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _shoppingCartService = shoppingCartService;
        _webHelper = webHelper;
        _orderService = orderService;
        _urlHelperFactory = urlHelperFactory;
        _actionContextAccessor = actionContextAccessor;
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Process a payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the process payment result
    /// </returns>
    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult());
    }

    /// <summary>
    /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        var redirectUrl = urlHelper.Action("HandleOrderPostProcessing", "PaymentPaybyrd", new { area = "Admin", orderId = postProcessPaymentRequest.Order.Id }, _webHelper.GetCurrentRequestProtocol());

        // Redirects the user to hosted form handler
        _httpContextAccessor.HttpContext.Response.Redirect(redirectUrl);

        return Task.CompletedTask;
    }


    /// <summary>
    /// Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains true - hide; false - display.
    /// </returns>
    public async Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        // You can put any logic here
        // for example, hide this payment method if all products in the cart are downloadable
        // or hide this payment method if current customer is from certain country

        if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
            return true;

        return false;
    }

    /// <summary>
    /// Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the additional handling fee
    /// </returns>
    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart, 0, false);
    }

    /// <summary>
    /// Captures payment
    /// </summary>
    /// <param name="capturePaymentRequest">Capture payment request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the capture payment result
    /// </returns>
    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();

        try
        {
            var client = new HttpClient();
            var orderId = refundPaymentRequest.Order.OrderGuid;

            using (client)
            {
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var settings = await _settingService.LoadSettingAsync<PaybyrdPaymentSettings>(storeScope);
                var testModeEnabled = settings.EnableTestMode;
                var apiKey = testModeEnabled ? settings.TestApiKey : settings.LiveApiKey;

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await client.GetAsync($"{PaybyrdPaymentDefaults.PaybyrdAPIBasePath}/orders/{orderId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    var orderStatusErrorMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderStatusError");

                    _notificationService.ErrorNotification($"{orderStatusErrorMessage} {errorMessage}");

                    return result;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                var transactions = ((JArray)responseObject.transactions).ToObject<TransactionModel[]>();
                Guid refundTransactionId;

                var firstSuccessTransaction = transactions.ToList().Find(t => t.Status == "Success");

                // Check if a successful transaction was found
                if (firstSuccessTransaction != null)
                {
                    refundTransactionId = firstSuccessTransaction.TransactionId;
                }
                else
                {
                    var refundErrorMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderRefundError");

                    _notificationService.ErrorNotification(refundErrorMessage);
                    return result;
                }

                // Set up the refund request payload
                var refundPayload = new
                {
                    isoAmount = (int)(refundPaymentRequest.AmountToRefund * 100)
                };

                var jsonPayload = JsonConvert.SerializeObject(refundPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // POST the Refund Request
                var refundResponse = await client.PostAsync($"{PaybyrdPaymentDefaults.PaybyrdAPIBasePath}/refund/{refundTransactionId}", content);

                // Check if the refund was successful
                if (refundResponse.IsSuccessStatusCode)
                {
                    var order = await _orderService.GetOrderByIdAsync(refundPaymentRequest.Order.Id);

                    if (order == null)
                    {
                        var orderNotFoundMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderNotFound");
                        _notificationService.ErrorNotification(orderNotFoundMessage);
                        return result;
                    }

                    if (refundPaymentRequest.IsPartialRefund)
                    {
                        order.PaymentStatus = PaymentStatus.PartiallyRefunded;
                        result.NewPaymentStatus = PaymentStatus.PartiallyRefunded;
                    }

                    else
                    {
                        order.PaymentStatus = PaymentStatus.Refunded;
                        result.NewPaymentStatus = PaymentStatus.Refunded;
                    }

                    // Update order payment status
                    await _orderService.UpdateOrderAsync(order);

                    var orderRefundedMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderRefunded");
                    _notificationService.SuccessNotification(orderRefundedMessage);

                    return result;
                }
                else
                {
                    result.Errors.Add($"Refund request failed with status code: {refundResponse.StatusCode}");
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception and set errors
            result.Errors.Add($"An error occurred while processing the refund: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Voids a payment
    /// </summary>
    /// <param name="voidPaymentRequest">Request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
    }

    /// <summary>
    /// Process recurring payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the process payment result
    /// </returns>
    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Cancels a recurring payment
    /// </summary>
    /// <param name="cancelPaymentRequest">Request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        // It's a redirection payment method, so we always return true
        return Task.FromResult(true);
    }

    /// <summary>
    /// Validate payment form
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of validating errors
    /// </returns>
    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    /// <summary>
    /// Get payment information
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the payment info holder
    /// </returns>
    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {
        return Task.FromResult(new ProcessPaymentRequest());
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/PaymentPaybyrd/Configure";
    }

    /// <summary>
    /// Gets a type of a view component for displaying plugin in public store ("payment info" checkout step)
    /// </summary>
    /// <returns>View component type</returns>
    public Type GetPublicViewComponent()
    {
        return typeof(PaybyrdViewComponent);
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        // Set initial settings
        var settings = new PaybyrdPaymentSettings
        {
            HFBackgroundColor = "#dbdbdb",
            HFPaymentBackgroundColor = "#ffffff",
            HFPrimaryColor = "#dbdbdb",
            HFTextColor = "#2c2c2c",
            PostPaymentOrderStatus = PostPaymentOrderStatus.Processing
        };

        await _settingService.SaveSettingAsync(settings);

        // Load localization resources
        await _localizationService.AddOrUpdateLocaleResourceAsync(LocalizationResources.GetLocaleResources());

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        // Delete all settings before uninstalling the plugin
        await _settingService.DeleteSettingAsync<PaybyrdPaymentSettings>();

        // Delete all localization resources before uninstalling the plugin
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payment.Paybyrd");

        await base.UninstallAsync();
    }

    /// <summary>
    /// Gets a payment method description that will be displayed on checkout pages in the public store
    /// </summary>
    /// <remarks>
    /// return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
    /// for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.PaymentMethodDescription");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether capture is supported
    /// </summary>
    public bool SupportCapture => false;

    /// <summary>
    /// Gets a value indicating whether partial refund is supported
    /// </summary>
    public bool SupportPartiallyRefund => true;

    /// <summary>
    /// Gets a value indicating whether refund is supported
    /// </summary>
    public bool SupportRefund => true;

    /// <summary>
    /// Gets a value indicating whether void is supported
    /// </summary>
    public bool SupportVoid => false;

    /// <summary>
    /// Gets a recurring payment type of payment method
    /// </summary>
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

    /// <summary>
    /// Gets a payment method type
    /// </summary>
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

    /// <summary>
    /// Gets a value indicating whether we should display a payment information page for this plugin
    /// </summary>
    public bool SkipPaymentInfo => false;

    #endregion
}