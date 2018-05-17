using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessOrder.Models
{
    public enum Status
    {
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Other = 4
    }
    public class OrderDetails
    {
        public Status status { get; set; }
        public string orderId { get; set; }
    }
}
