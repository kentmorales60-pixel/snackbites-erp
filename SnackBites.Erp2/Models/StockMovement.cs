using System;

namespace SnackBites.Erp2.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public string Type { get; set; }
        public int? RequisitionId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string UserId { get; set; }
    }
}