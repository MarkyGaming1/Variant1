using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UMFST.MIP.Variant1
{
    public class Order
    {
        [Key]
        public string Id { get; set; }

        public string CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        public DateTime? OrderDate { get; set; }
        public bool IsPaid { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment Payment { get; set; }
    }
}
