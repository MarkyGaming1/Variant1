using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UMFST.MIP.Variant1
{
    public class Customer
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
