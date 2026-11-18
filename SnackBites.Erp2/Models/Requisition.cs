using System;
using System.Collections.Generic;

namespace SnackBites.Erp2.Models
{
    public class Requisition
    {
        public int Id { get; set; }
        public string RequisitionNo { get; set; }
        public Department Department { get; set; }
        public DateTime DateRequested { get; set; } = DateTime.Now;
        public RequisitionStatus Status { get; set; }
        public string RequestedById { get; set; }
        public string Co1ReviewedById { get; set; }
        public DateTime? Co1ReviewedOn { get; set; }
        public string ReviewRemarks { get; set; }
        public string IssuedById { get; set; }
        public DateTime? IssuedOn { get; set; }
        public string AcknowledgedBy { get; set; }
        public DateTime? ClosedOn { get; set; }
        public virtual ICollection<RequisitionLine> Lines { get; set; }
    }
}