using System;

namespace SnackBites.Erp2.Models
{
    public class PurchaseRequisition
    {
        public int Id { get; set; }
        public int RequisitionId { get; set; }
        public decimal EstimatedTotal { get; set; }
        public string Status { get; set; }
        public string DecidedById { get; set; }
        public DateTime? DecidedOn { get; set; }
        public string Remarks { get; set; }
        public virtual Requisition Requisition { get; set; }
    }
}