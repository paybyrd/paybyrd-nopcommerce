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

namespace Nop.Plugin.Payments.Paybyrd;

/// <summary>
/// Paybyrd payment processor
/// </summary>
public class PaybyrdPaymentProcessor : BasePlugin, IPaymentMethod
{
    #region Fields

    protected readonly PaybyrdPaymentSettings _paybyrdPaymentSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly ISettingService _settingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IWebHelper _webHelper;
    protected readonly IUrlHelperFactory _urlHelperFactory;
    protected readonly IActionContextAccessor _actionContextAccessor;
    protected readonly IHttpContextAccessor _httpContextAccessor;

    #endregion

    #region Ctor

    public PaybyrdPaymentProcessor(PaybyrdPaymentSettings paybyrdPaymentSettings,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        IHttpContextAccessor httpContextAccessor)
    {
        _paybyrdPaymentSettings = paybyrdPaymentSettings;
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _shoppingCartService = shoppingCartService;
        _webHelper = webHelper;
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

    public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
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
    public bool SupportPartiallyRefund => false;
    public bool SupportRefund => false;
    public bool SupportVoid => false;
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
    public bool SkipPaymentInfo => false;

    #endregion
}
