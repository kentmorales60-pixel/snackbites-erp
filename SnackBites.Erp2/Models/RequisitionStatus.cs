namespace SnackBites.Erp2.Models
{
    // Keep new values at the END so the numbers already saved in the database don't shift.
    public enum RequisitionStatus
    {
        Submitted, ReturnedForRevision, Approved, Reserved, AwaitingPurchase,
        PendingBudget, OnHold, PurchaseOrdered, Received, Issued, Closed, Cancelled,
        BudgetApproved
    }
}