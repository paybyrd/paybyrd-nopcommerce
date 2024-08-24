using System.Collections.Generic;

namespace Nop.Plugin.Payments.Paybyrd.Localization
{
    public static class LocalizationResources
    {
        public static Dictionary<string, string> GetLocaleResources()
        {
            return new Dictionary<string, string>
            {
                ["Plugins.Payments.Paybyrd.DescriptionText"] = "By selecting Paybyrd Payment Method, you can proceed with the checkout and after confirming the checkout, you will get redirected to Paybyrd's Hosted Form page to complete the payment. With Paybyrd you can use the different ways to pay for your order, like Credit Cards, MBWay, Multibanco, Paypal and many others!",
                ["Plugins.Payments.Paybyrd.EnableTestMode"] = "Enable Test Mode",
                ["Plugins.Payments.Paybyrd.EnableTestMode.Hint"] = "When enabled, all the API calls will use the Test API Key instead of the Live one. Use it to test payments before going live.",
                ["Plugins.Payments.Paybyrd.HFBackgroundColor"] = "Hosted Form Background Color",
                ["Plugins.Payments.Paybyrd.HFBackgroundColor.Hint"] = "Sets the main background color for the entire payment hosted form page.",
                ["Plugins.Payments.Paybyrd.HFPaymentBackgroundColor"] = "Hosted Form Payment Background Color",
                ["Plugins.Payments.Paybyrd.HFPaymentBackgroundColor.Hint"] = "Sets the background color for the payment box in the hosted form.",
                ["Plugins.Payments.Paybyrd.HFPrimaryColor"] = "Hosted Form Primary Color",
                ["Plugins.Payments.Paybyrd.HFPrimaryColor.Hint"] = "Sets the main colors of buttons and icons.",
                ["Plugins.Payments.Paybyrd.HFTextColor"] = "Hosted Form Text Color",
                ["Plugins.Payments.Paybyrd.HFTextColor.Hint"] = "Sets all font colors for the hosted form.",
                ["Plugins.Payments.Paybyrd.Instructions"] = "Configure the Paybyrd plugin by filling the below fields. Once the Live API Key is filled and saved, the plugin will be ready to be used by the customers.",
                ["Plugins.Payments.Paybyrd.LiveApiKey"] = "Live Private Key",
                ["Plugins.Payments.Paybyrd.LiveApiKey.Required"] = "Live Private Key is required",
                ["Plugins.Payments.Paybyrd.OrderCreationError"] = "Paybyrd order creation has failed:",
                ["Plugins.Payments.Paybyrd.OrderNotFound"] = "Order not found.",
                ["Plugins.Payments.Paybyrd.OrderCanceled"] = "The order was canceled.",
                ["Plugins.Payments.Paybyrd.OrderNotPaid"] = "The order is still being processed.",
                ["Plugins.Payments.Paybyrd.OrderPaymentError"] = "There was en error on processing this payment.",
                ["Plugins.Payments.Paybyrd.OrderRefunded"] = "The order was refunded.",
                ["Plugins.Payments.Paybyrd.OrderTestPaid"] = "The test payment was validated. The order status will not be changed to paid due to the \"Enable Test Mode\" option being set as true.",
                ["Plugins.Payments.Paybyrd.TestApiKey"] = "Test Private Key",
                ["Plugins.Payments.Paybyrd.TestMode"] = "Enable Test Mode",
                ["Plugins.Payments.Paybyrd.TestMode.Hint"] = "Switches the payment gateway to use your test API Keys. Turn this flag off when ready for production.",
                ["Plugins.Payments.Paybyrd.PaymentMethodDescription"] = "Pay using Paybyrd Hosted Form and select your preferred payment method to pay, like credit card and others.",
                ["Plugins.Payments.Manual.Fields.PostPaymentOrderStatus"] = "After payment mark order as",
                ["Plugins.Payments.Manual.Fields.PostPaymentOrderStatus.Hint"] = "Specify which order status should be applied after a successful payment. Usually Processing (default) is the recommended option and the Complete is for digital products.",
                ["Plugins.Payments.Paybyrd.WebhookId"] = "Webhook ID",
                ["Plugins.Payments.Paybyrd.WebhookId.Hint"] = "This field is automatically generated after filling Live Api Key and saving for the first time and it is required for processing async payments.",
                ["Plugins.Payments.Paybyrd.WebhookCreationFailed"] = "The Webhook creation has failed. Try saving again."
            };
        }
    }
}
