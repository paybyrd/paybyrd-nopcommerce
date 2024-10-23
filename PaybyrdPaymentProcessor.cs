using System.Collections.Generic;
using System.Threading.Tasks;
using System;
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

    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult());
    }

    public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        var redirectUrl = urlHelper.Action("HandleOrderPostProcessing", "PaymentPaybyrd", new { area = "Admin", orderId = postProcessPaymentRequest.Order.Id }, _webHelper.GetCurrentRequestProtocol());

        _httpContextAccessor.HttpContext.Response.Redirect(redirectUrl);

        return Task.CompletedTask;
    }

    public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        if (!_shoppingCartService.ShoppingCartRequiresShippingAsync(cart).Result)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart, 0, false);
    }

    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
    }

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

    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
    }

    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    public string GetPublicViewComponentName()
    {
        return "PaybyrdPayment";
    }

    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return Task.FromResult(true);
    }

    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {
        return Task.FromResult(new ProcessPaymentRequest());
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/PaymentPaybyrd/Configure";
    }

    public Type GetPublicViewComponent()
    {
        return typeof(PaybyrdViewComponent);
    }

    public override async Task InstallAsync()
    {
        var settings = new PaybyrdPaymentSettings
        {
            HFBackgroundColor = "#dbdbdb",
            HFPaymentBackgroundColor = "#ffffff",
            HFPrimaryColor = "#dbdbdb",
            HFTextColor = "#2c2c2c",
            PostPaymentOrderStatus = PostPaymentOrderStatus.Processing
        };

        await _settingService.SaveSettingAsync(settings);

        await _localizationService.AddOrUpdateLocaleResourceAsync(LocalizationResources.GetLocaleResources());

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<PaybyrdPaymentSettings>();

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payment.Paybyrd");

        await base.UninstallAsync();
    }

    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.PaymentMethodDescription");
    }

    #endregion

    #region Properties

    public bool SupportCapture => false;
    public bool SupportPartiallyRefund => true;
    public bool SupportRefund => true;
    public bool SupportVoid => false;
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
    public bool SkipPaymentInfo => false;

    #endregion
}
