using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Paybyrd.Models;

public record PaymentInfoModel : BaseNopModel
{
    public string DescriptionText { get; set; }
}