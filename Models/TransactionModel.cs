using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Paybyrd.Models
{
    internal class TransactionModel
    {
        public string status { get; set; }

        public Guid transactionId { get; set; }
    }
}
