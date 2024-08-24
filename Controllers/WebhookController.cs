using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.Paybyrd.Controllers
{
    [Route("Plugins/PaymentPaybyrd/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ISettingService settingService, IOrderService orderService, IStoreContext storeContext, ILogger<WebhookController> logger)
        {
            _settingService = settingService;
            _orderService = orderService;
            _storeContext = storeContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            // Retrieve webhookId from settings
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var paybyrdPaymentSettings = await _settingService.LoadSettingAsync<PaybyrdPaymentSettings>(storeScope);
            var webhookId = paybyrdPaymentSettings.WebhookId;
            var testModeEnabled = paybyrdPaymentSettings.EnableTestMode;
            var postPaymentOrderStatus = paybyrdPaymentSettings.PostPaymentOrderStatus;

            // Validate x-api-key header
            if (!Request.Headers.TryGetValue("x-api-key", out var apiKey) || apiKey != webhookId)
            {
                _logger.LogWarning("Invalid or missing x-api-key");
                return Unauthorized(new { message = "Invalid or missing x-api-key" });
            }

            // Read and parse the request body
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var body = await reader.ReadToEndAsync();
                var jsonBody = JObject.Parse(body);

                var content = jsonBody["content"];
                var status = content["status"]?.ToString();
                var orderRef = content["orderRef"]?.ToString();
                var statusMessage = "pending";

                if (string.IsNullOrEmpty(orderRef) || string.IsNullOrEmpty(status))
                {
                    _logger.LogWarning("Invalid webhook payload");
                    return BadRequest(new { message = "Invalid webhook payload" });
                }

                // Extract and parse order ID from orderRef
                var strNopOrderId = orderRef.Replace("npc_", string.Empty);
                if (!int.TryParse(strNopOrderId, out int nopOrderId))
                {
                    _logger.LogWarning("Invalid orderRef format");
                    return BadRequest(new { message = "Invalid orderRef format" });
                }

                // Retrieve order from NopCommerce
                var order = await _orderService.GetOrderByIdAsync(nopOrderId);
                if (order == null)
                {
                    _logger.LogWarning($"Order not found: {nopOrderId}");
                    return NotFound(new { message = "Order not found" });
                }

                // Update order status based on the webhook status
                if (status == "paid" || status == "acquirersuccess" || status == "success")
                {
                    if (testModeEnabled)
                    {
                        _logger.LogInformation($"Order {nopOrderId} is a test and the payment was validated with success.");
                        return Ok(new { message = "Test webhook processed successfully" });
                    }
                    else
                    {
                        order.PaymentStatus = PaymentStatus.Paid;
                        order.OrderStatus = postPaymentOrderStatus == PostPaymentOrderStatus.Processing ? OrderStatus.Processing : OrderStatus.Complete;
                        await _orderService.UpdateOrderAsync(order);
                    }

                    statusMessage = "paid";
                    _logger.LogInformation($"Order {nopOrderId} marked as paid.");
                }
                else if (status == "canceled")
                {
                    order.PaymentStatus = PaymentStatus.Voided;
                    order.OrderStatus = OrderStatus.Cancelled;
                    await _orderService.UpdateOrderAsync(order);

                    statusMessage = "canceled";
                    _logger.LogInformation($"Order {nopOrderId} canceled.");
                }
                else if (status == "refunded")
                {
                    order.PaymentStatus = PaymentStatus.Refunded;
                    order.OrderStatus = OrderStatus.Complete;
                    await _orderService.UpdateOrderAsync(order);

                    statusMessage = "refunded";
                    _logger.LogInformation($"Order {nopOrderId} was refunded.");
                }
                else if (status == "error")
                {
                    order.PaymentStatus = PaymentStatus.Voided;
                    order.OrderStatus = OrderStatus.Cancelled;
                    await _orderService.UpdateOrderAsync(order);

                    statusMessage = "error";
                    _logger.LogWarning($"Order {nopOrderId} had an error and it was canceled.");
                }
                else
                {
                    _logger.LogWarning($"Unsupported payment status: {status}");
                    return BadRequest(new { message = "Unsupported payment status" });
                }

                return Ok(new { message = $"Webhook processed successfully. Order status: {statusMessage}" });
            }
        }
    }
}
