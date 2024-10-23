using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Paybyrd.Models
{
    internal class TransactionModel
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("transactionId")]
        public Guid TransactionId { get; set; }
    }
}