using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Paybyrd.Localization;
using Nop.Plugin.Payments.Paybyrd.Models;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Paybyrd.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public class PaymentPaybyrdController : BasePaymentController
{
    #region Fields

    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWebHelper _webHelper;
    protected readonly IOrderService _orderService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public PaymentPaybyrdController(ILanguageService languageService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWebHelper webHelper,
        IOrderService orderService,
        IWorkContext workContext)
    {
        _languageService = languageService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _webHelper = webHelper;
        _orderService = orderService;
        _workContext = workContext;
    }

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        await UpdateLocaleResources();

        // Load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var paybyrdPaymentSettings = await _settingService.LoadSettingAsync<PaybyrdPaymentSettings>(storeScope);

        var model = new ConfigurationModel
        {
            PostPaymentOrderStatusId = Convert.ToInt32(paybyrdPaymentSettings.PostPaymentOrderStatus),
            PostPaymentOrderStatusValues = await paybyrdPaymentSettings.PostPaymentOrderStatus.ToSelectListAsync(),
            LiveApiKey = paybyrdPaymentSettings.LiveApiKey,
            TestApiKey = paybyrdPaymentSettings.TestApiKey,
            EnableTestMode = paybyrdPaymentSettings.EnableTestMode,
            WebhookId = paybyrdPaymentSettings.WebhookId,
            HFBackgroundColor = paybyrdPaymentSettings.HFBackgroundColor,
            HFPaymentBackgroundColor = paybyrdPaymentSettings.HFPaymentBackgroundColor,
            HFPrimaryColor = paybyrdPaymentSettings.HFPrimaryColor,
            ActiveStoreScopeConfiguration = storeScope
        };

        model.LiveApiKey = paybyrdPaymentSettings.LiveApiKey;
        model.PostPaymentOrderStatusId = Convert.ToInt32(paybyrdPaymentSettings.PostPaymentOrderStatus);
        model.TestApiKey = paybyrdPaymentSettings.TestApiKey;
        model.EnableTestMode = paybyrdPaymentSettings.EnableTestMode;
        model.WebhookId = paybyrdPaymentSettings.WebhookId;
        model.HFBackgroundColor = paybyrdPaymentSettings.HFBackgroundColor;
        model.HFPaymentBackgroundColor = paybyrdPaymentSettings.HFPaymentBackgroundColor;
        model.HFPrimaryColor = paybyrdPaymentSettings.HFPrimaryColor;
        model.HFTextColor = paybyrdPaymentSettings.HFTextColor;
        model.ActiveStoreScopeConfiguration = storeScope;

        if (storeScope > 0)
        {
            model.LiveApiKey_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.LiveApiKey, storeScope);
            model.PostPaymentOrderStatusId_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.PostPaymentOrderStatus, storeScope);
            model.TestApiKey_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.TestApiKey, storeScope);
            model.EnableTestMode_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.EnableTestMode, storeScope);
            model.WebhookId_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.WebhookId, storeScope);
            model.HFBackgroundColor_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.HFBackgroundColor, storeScope);
            model.HFPaymentBackgroundColor_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.HFPaymentBackgroundColor, storeScope);
            model.HFPrimaryColor_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.HFPrimaryColor, storeScope);
            model.HFTextColor_OverrideForStore = await _settingService.SettingExistsAsync(paybyrdPaymentSettings, x => x.HFTextColor, storeScope);
        }

        return View("~/Plugins/Payments.Paybyrd/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var paybyrdPaymentSettings = await _settingService.LoadSettingAsync<PaybyrdPaymentSettings>(storeScope);
        var webhookId = await FetchWebhookIdAsync(model.LiveApiKey, paybyrdPaymentSettings.WebhookId);

        // Save admin configurations
        paybyrdPaymentSettings.LiveApiKey = model.LiveApiKey;
        paybyrdPaymentSettings.TestApiKey = model.TestApiKey;
        paybyrdPaymentSettings.EnableTestMode = model.EnableTestMode;
        paybyrdPaymentSettings.PostPaymentOrderStatus = (PostPaymentOrderStatus)model.PostPaymentOrderStatusId;
        paybyrdPaymentSettings.WebhookId = webhookId;
        paybyrdPaymentSettings.HFBackgroundColor = model.HFBackgroundColor;
        paybyrdPaymentSettings.HFPaymentBackgroundColor = model.HFPaymentBackgroundColor;
        paybyrdPaymentSettings.HFPrimaryColor = model.HFPrimaryColor;
        paybyrdPaymentSettings.HFTextColor = model.HFTextColor;

        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.LiveApiKey, model.LiveApiKey_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.TestApiKey, model.TestApiKey_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.PostPaymentOrderStatus, model.PostPaymentOrderStatusId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.EnableTestMode, model.EnableTestMode_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.WebhookId, model.WebhookId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.HFBackgroundColor, model.HFBackgroundColor_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.HFPaymentBackgroundColor, model.HFPaymentBackgroundColor_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.HFPrimaryColor, model.HFPrimaryColor_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(paybyrdPaymentSettings, x => x.HFTextColor, model.HFTextColor_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    public async Task<IActionResult> HandleOrderPostProcessing(int orderId)
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<PaybyrdPaymentSettings>(storeScope);

        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            var orderNotFoundMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderNotFound");
            _notificationService.ErrorNotification(orderNotFoundMessage);
            return Redirect(_webHelper.GetStoreLocation());
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        var currency = order.CustomerCurrencyCode;
        var culture = (await _languageService.GetLanguageByIdAsync(order.CustomerLanguageId))?.LanguageCulture;
        var testModeEnabled = settings.EnableTestMode;
        var apiKey = testModeEnabled ? settings.TestApiKey : settings.LiveApiKey;
        var postPaymentRedirectUrl = $"{_webHelper.GetStoreLocation()}Admin/PaymentPaybyrd/ValidatePayment";

        var requestBody = new
        {
            amount = order.OrderTotal,
            currency,
            orderRef = $"npc_{order.Id}",
            shopper = new
            {
                email = customer.Email,
                firstName = customer.FirstName,
                lastName = customer.LastName
            },
            orderOptions = new
            {
                culture,
                redirectUrl = postPaymentRedirectUrl
            },
            paymentOptions = new
            {
                useSimulated = testModeEnabled,
                tokenOptions = new
                {
                    customReference = customer.Email
                }
            }
        };

        var jsonContent = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            var response = await client.PostAsync($"{PaybyrdPaymentDefaults.PaybyrdAPIBasePath}/orders", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                var orderCreationErrorMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderCreationError");

                _notificationService.ErrorNotification($"{orderCreationErrorMessage} {errorMessage}");
                return Redirect(_webHelper.GetStoreLocation());
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
            var hostedFormCheckoutKey = responseObject.checkoutKey;
            var hostedFormOrderId = responseObject.orderId;

            var hostedFormConfigs = new HostedFormConfigurationsModel
            {
                RedirectUrl = postPaymentRedirectUrl,
                Locale = culture,
                OrderId = hostedFormOrderId,
                CheckoutKey = hostedFormCheckoutKey,
                Theme = new HostedFormThemeConfigurationsModel
                {
                    BackgroundColor = settings.HFBackgroundColor,
                    FormBackgroundColor = settings.HFPaymentBackgroundColor,
                    PrimaryColor = settings.HFPrimaryColor,
                    TextColor = settings.HFTextColor
                },
                AutoRedirect = true,
                ShowCancelButton = false,
                SkipATMSuccessPage = true
            };

            // Convert configs to base64 that is supported by the FE to customize theme
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var hostedFormConfigsJsonString = JsonConvert.SerializeObject(hostedFormConfigs, jsonSerializerSettings);
            var hostedFormConfigsJsonBytes = Encoding.UTF8.GetBytes(hostedFormConfigsJsonString);
            var hostedFormConfigsBase64String = Convert.ToBase64String(hostedFormConfigsJsonBytes);

            return Redirect($"{PaybyrdPaymentDefaults.PaybyrdHostedFormBasePath}?checkoutKey={hostedFormCheckoutKey}&orderId={hostedFormOrderId}&configs={hostedFormConfigsBase64String}");
        }
    }
    public async Task<IActionResult> ValidatePayment(string orderId)
    {
        var orderNotFoundMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderNotFound");

        if (orderId == null)
        {
            _notificationService.ErrorNotification(orderNotFoundMessage);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        using (var client = new HttpClient())
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
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
            var strOrderRef = (string)responseObject.orderRef;
            var strNopOrderId = strOrderRef.Replace("npc_", string.Empty);
            var postPaymentOrderStatus = settings.PostPaymentOrderStatus;

            if (!int.TryParse(strNopOrderId, out int nopOrderId))
            {
                _notificationService.ErrorNotification(orderNotFoundMessage);
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var order = await _orderService.GetOrderByIdAsync(nopOrderId);
            if (order == null)
            {
                _notificationService.ErrorNotification(orderNotFoundMessage);
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (responseObject.status == "paid" || responseObject.status == "acquirersuccess" || responseObject.status == "success")
            {
                if (testModeEnabled)
                {
                    var orderTestPaidMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderTestPaid");
                    _notificationService.ErrorNotification(orderTestPaidMessage);
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.OrderStatus = postPaymentOrderStatus == PostPaymentOrderStatus.Processing ? OrderStatus.Processing : OrderStatus.Complete;
                    await _orderService.UpdateOrderAsync(order);
                }

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else if (responseObject.status == "canceled")
            {
                order.PaymentStatus = PaymentStatus.Voided;
                order.OrderStatus = OrderStatus.Cancelled;
                await _orderService.UpdateOrderAsync(order);

                var orderCanceledMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderCanceled");
                _notificationService.ErrorNotification(orderCanceledMessage);
            }
            else if (responseObject.status == "refunded")
            {
                order.PaymentStatus = PaymentStatus.Refunded;
                order.OrderStatus = OrderStatus.Complete;
                await _orderService.UpdateOrderAsync(order);

                var orderRefundedMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderRefunded");
                _notificationService.SuccessNotification(orderRefundedMessage);
            }
            else if (responseObject.status == "error")
            {
                order.PaymentStatus = PaymentStatus.Voided;
                order.OrderStatus = OrderStatus.Cancelled;
                await _orderService.UpdateOrderAsync(order);

                var orderErrorMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderPaymentError");
                _notificationService.ErrorNotification(orderErrorMessage);
            }
            else
            {
                var orderNotPaidMessage = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.OrderNotPaid");
                _notificationService.WarningNotification(orderNotPaidMessage);
            }
        }

        return RedirectToRoute("ShoppingCart");
    }

    public async Task UpdateLocaleResources()
    {
        var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
        await _localizationService.AddOrUpdateLocaleResourceAsync(LocalizationResources.GetLocaleResources());
    }

    private async Task<string> FetchWebhookIdAsync(string liveApiKey, string currentWebhookId)
    {
        if (!string.IsNullOrEmpty(currentWebhookId))
        {
            return currentWebhookId;
        }

        var storeUrlBasePath = _webHelper.GetStoreLocation();
        var webhookUrlPath = $"{storeUrlBasePath}Plugins/PaymentPaybyrd/Webhook";

        var requestBody = new
        {
            url = webhookUrlPath,
            credentialType = "api-key",
            events = PaybyrdPaymentDefaults.WebhookEventNames,
            paymentMethods = new string[] { }
        };

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-API-Key", liveApiKey);

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{PaybyrdPaymentDefaults.PaybyrdAPIWebhookBasePath}/settings", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);

                if (responseObject != null && responseObject.data != null && responseObject.data.credential != null && responseObject.data.credential.apiKey != null)
                {
                    return responseObject.data.credential.apiKey.ToString();
                }
            }

            _notificationService.WarningNotification(await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.WebhookCreationFailed"));

            return null;
        }
    }

    #endregion
}
