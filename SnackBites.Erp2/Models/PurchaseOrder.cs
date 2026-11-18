using System;
using System.Collections.Generic;

namespace SnackBites.Erp2.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public int PurchaseRequisitionId { get; set; }
        public int SupplierId { get; set; }
        public string PoNumber { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<PurchaseOrderLine> Lines { get; set; }
    }
}