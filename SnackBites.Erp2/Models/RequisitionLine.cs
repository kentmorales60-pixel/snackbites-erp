using System.ComponentModel.DataAnnotations.Schema;

namespace SnackBites.Erp2.Models
{
    public class RequisitionLine
    {
        public int Id { get; set; }
        public int RequisitionId { get; set; }
        public int ItemId { get; set; }
        public decimal QtyRequested { get; set; }
        public decimal QtyReserved { get; set; }
        public virtual Requisition Requisition { get; set; }
        public virtual Item Item { get; set; }
        [NotMapped] public decimal QtyShort => QtyRequested - QtyReserved;
    }
}