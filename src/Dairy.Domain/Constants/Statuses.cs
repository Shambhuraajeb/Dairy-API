namespace Dairy.Domain.Constants;

// Canonical status strings shared by C#, stored procedures and the frontend.
// Statuses are stored as VARCHAR; use these exact values (they mirror the column COMMENTs in database/01_schema.sql).

/// <summary>Farmer.Status: Pending | Active | Inactive | Blocked.</summary>
public static class FarmerStatus
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Blocked = "Blocked";

    // Farmer.VerificationStatus: Pending | Verified | Rejected
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";
}

public static class MilkCollectionStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

/// <summary>MilkCollection.SettlementStatus.</summary>
public static class SettlementStatus
{
    public const string Unsettled = "Unsettled";
    public const string Settled = "Settled";
}

public static class ShiftStatus
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}

/// <summary>FarmerPayment.Status (farmer settlement): Draft | Approved | Paid | Cancelled.</summary>
public static class PaymentStatus
{
    public const string Draft = "Draft";
    public const string Approved = "Approved";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
}

/// <summary>FarmerAdvance.Status.</summary>
public static class AdvanceStatus
{
    public const string Open = "Open";
    public const string Recovered = "Recovered";
    public const string Cancelled = "Cancelled";
}

public static class LoanStatus
{
    public const string Active = "Active";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}

/// <summary>FarmerLoanInstallment.Status: Pending | Partial | Paid (overdue is derived from DueDate, never stored).</summary>
public static class InstallmentStatus
{
    public const string Pending = "Pending";
    public const string Partial = "Partial";
    public const string Paid = "Paid";
}

/// <summary>Purchase.Status: Draft | Approved | Received | Cancelled (partial receipts stay Approved).</summary>
public static class PurchaseStatus
{
    public const string Draft = "Draft";
    public const string Approved = "Approved";
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}

/// <summary>Purchase.PaymentStatus.</summary>
public static class PurchasePaymentStatus
{
    public const string Unpaid = "Unpaid";
    public const string Partial = "Partial";
    public const string Paid = "Paid";
}

/// <summary>StockTransaction.TransactionType: Purchase | Consumption | Adjustment | Return | Sale | Opening (Quantity is signed).</summary>
public static class StockTransactionType
{
    public const string Purchase = "Purchase";
    public const string Consumption = "Consumption";
    public const string Adjustment = "Adjustment";
    public const string Return = "Return";
    public const string Sale = "Sale";
    public const string Opening = "Opening";

    // Legacy names kept so existing references still compile; they map to the real stored values.
    public const string Receipt = Purchase;
    public const string AdjustmentIn = Adjustment;
    public const string AdjustmentOut = Adjustment;
    public const string Wastage = Adjustment;
}

/// <summary>InventoryBatch.Status.</summary>
public static class BatchStatus
{
    public const string Available = "Available";
    public const string Depleted = "Depleted";
    public const string Expired = "Expired";
    public const string Quarantined = "Quarantined";
}

public static class SubscriptionStatus
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}

public static class ComplaintStatus
{
    public const string Open = "Open";
    public const string Assigned = "Assigned";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
}

/// <summary>SalesOrder.Status.</summary>
public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Scheduled = "Scheduled";
    public const string Dispatched = "Dispatched";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
}

/// <summary>SalesOrder.PaymentStatus.</summary>
public static class OrderPaymentStatus
{
    public const string Unpaid = "Unpaid";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
}

/// <summary>Delivery.Status: Pending | Dispatched | Delivered | Failed.</summary>
public static class DeliveryStatus
{
    public const string Pending = "Pending";
    public const string Dispatched = "Dispatched";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";
}

/// <summary>Invoice.Status: Draft | Issued | PartiallyPaid | Paid | Cancelled (overdue is derived from DueDate, never stored).</summary>
public static class InvoiceStatus
{
    public const string Draft = "Draft";
    public const string Issued = "Issued";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";

    /// <summary>Legacy name for an issued invoice with no payment yet.</summary>
    public const string Unpaid = Issued;
}

/// <summary>CustomerPayment.Status.</summary>
public static class CustomerPaymentStatus
{
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}

public static class ExpenseStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

/// <summary>LeaveRequest.Status: Pending | Approved | Rejected.</summary>
public static class LeaveStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class AttendanceStatus
{
    public const string Present = "Present";
    public const string Absent = "Absent";
    public const string HalfDay = "HalfDay";
    public const string Leave = "Leave";
}

/// <summary>Salary.Status: Generated | Paid.</summary>
public static class SalaryStatus
{
    public const string Generated = "Generated";
    public const string Paid = "Paid";
}

/// <summary>MaintenanceRecord.Status: Completed | InProgress.</summary>
public static class MaintenanceStatus
{
    public const string Completed = "Completed";
    public const string InProgress = "InProgress";
}

/// <summary>MaintenanceSchedule.Status: Active | Paused.</summary>
public static class MaintenanceScheduleStatus
{
    public const string Active = "Active";
    public const string Paused = "Paused";
}

/// <summary>Equipment.Status: Operational | UnderMaintenance | Retired.</summary>
public static class EquipmentStatus
{
    public const string Operational = "Operational";
    public const string UnderMaintenance = "UnderMaintenance";
    public const string Retired = "Retired";
}

/// <summary>Notification.Severity values.</summary>
public static class NotificationSeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Danger = "danger";
    public const string Success = "success";
}
