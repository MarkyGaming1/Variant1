using System.ComponentModel.DataAnnotations;

namespace UMFST.MIP.Variant1
{
    public class OrderItem
    {
        [Key]
        public string Id { get; set; }

        public string OrderId { get; set; }
        public virtual Order Order { get; set; }

        public string BookId { get; set; }
        public virtual Book Book { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
    }
}
