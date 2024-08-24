using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Paybyrd.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Paybyrd.Components;

[ViewComponent(Name = "PaybyrdPayment")]
public class PaybyrdViewComponent : NopViewComponent
{

    protected readonly ILocalizationService _localizationService;

    public PaybyrdViewComponent(ILocalizationService localizationService)
    {
        _localizationService = localizationService;;
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new PaymentInfoModel
        {
            DescriptionText = await _localizationService.GetResourceAsync("Plugins.Payments.Paybyrd.DescriptionText")
        };

        return View("~/Plugins/Payments.Paybyrd/Views/PaymentInfo.cshtml", model);
    }
}