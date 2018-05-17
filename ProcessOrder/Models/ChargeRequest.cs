using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessOrder.Models
{
    public class ChargeRequest
    {
        public string stripeToken { get; set; }
        public string stripeEmail { get; set; }
        public double stripeAmt { get; set; }
    }
}
