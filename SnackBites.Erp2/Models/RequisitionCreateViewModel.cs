using System.Collections.Generic;

namespace SnackBites.Erp2.Models
{
    public class RequisitionLineInput
    {
        public int? ItemId { get; set; }
        public decimal? Qty { get; set; }
    }

    public class RequisitionCreateViewModel
    {
        public Department Department { get; set; }
        public List<RequisitionLineInput> Lines { get; set; } = new List<RequisitionLineInput>();
    }
}