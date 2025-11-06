using System.Collections.Generic;

namespace UMFST.MIP.Variant1
{
    public class Store
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public string AddressLine1 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Zip { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }
}
