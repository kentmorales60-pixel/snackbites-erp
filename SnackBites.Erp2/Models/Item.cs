using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnackBites.Erp2.Models
{
    public class Item
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; }
        [NotMapped] public decimal QtyAvailable => QtyOnHand - QtyReserved;
    }
}