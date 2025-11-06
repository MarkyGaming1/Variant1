using System.ComponentModel.DataAnnotations;

namespace UMFST.MIP.Variant1
{
    public class Payment
    {
        [Key]
        public string Id { get; set; }

        public string OrderId { get; set; }
        public virtual Order Order { get; set; }

        public string Method { get; set; }
        public decimal Amount { get; set; }
        public bool Captured { get; set; }
    }
}
