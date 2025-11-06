using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UMFST.MIP.Variant1
{
    public class Book
    {
        [Key]
        public string Isbn { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        public string AuthorId { get; set; }
        public virtual Author Author { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
