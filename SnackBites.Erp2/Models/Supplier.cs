using System.ComponentModel.DataAnnotations;

namespace SnackBites.Erp2.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        public string Contact { get; set; }
    }
}