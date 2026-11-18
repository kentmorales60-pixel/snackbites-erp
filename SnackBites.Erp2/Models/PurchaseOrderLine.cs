namespace SnackBites.Erp2.Models
{
    public class PurchaseOrderLine
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ItemId { get; set; }
        public decimal QtyOrdered { get; set; }
        public decimal QtyReceived { get; set; }
        public decimal UnitCost { get; set; }
        public string InspectionRemarks { get; set; }
    }
}