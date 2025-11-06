using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UMFST.MIP.Variant1
{
    public class Author
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
