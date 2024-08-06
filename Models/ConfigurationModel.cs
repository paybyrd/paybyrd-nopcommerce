using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Paybyrd.Models;

public record ConfigurationModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.EnableTestMode")]
    public bool EnableTestMode { get; set; }
    public bool EnableTestMode_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.HFBackgroundColor")]
    public string HFBackgroundColor { get; set; }
    public bool HFBackgroundColor_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.HFPaymentBackgroundColor")]
    public string HFPaymentBackgroundColor { get; set; }
    public bool HFPaymentBackgroundColor_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.HFPrimaryColor")]
    public string HFPrimaryColor { get; set; }
    public bool HFPrimaryColor_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.HFTextColor")]
    public string HFTextColor { get; set; }
    public bool HFTextColor_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.LiveApiKey")]
    [DataType(DataType.Password)]
    public string LiveApiKey { get; set; }
    public bool LiveApiKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.TestApiKey")]
    [DataType(DataType.Password)]
    public string TestApiKey { get; set; }
    public bool TestApiKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Paybyrd.WebhookId")]
    public string WebhookId { get; set; }
    public bool WebhookId_OverrideForStore { get; set; }

    public int PostPaymentOrderStatusId { get; set; }
    [NopResourceDisplayName("Plugins.Payments.Manual.Fields.PostPaymentOrderStatus")]
    public SelectList PostPaymentOrderStatusValues { get; set; }
    public bool PostPaymentOrderStatusId_OverrideForStore { get; set; }
}