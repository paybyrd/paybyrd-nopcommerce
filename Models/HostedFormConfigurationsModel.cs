namespace Nop.Plugin.Payments.Paybyrd.Models
{
    public class HostedFormThemeConfigurationsModel
    {
        public string BackgroundColor { get; set; }
        public string FormBackgroundColor { get; set; }
        public string PrimaryColor { get; set; }
        public string TextColor { get; set; }
        public string EffectsBackgroundColor { get; set; }
    }

    public class HostedFormConfigurationsModel
    {
        public string RedirectUrl { get; set; }
        public string Locale { get; set; }
        public string OrderId { get; set; }
        public string CheckoutKey { get; set; }
        public HostedFormThemeConfigurationsModel Theme { get; set; }
        public bool AutoRedirect { get; set; }
        public bool ShowCancelButton { get; set; }
        public bool SkipATMSuccessPage { get; set; }
    }
}
