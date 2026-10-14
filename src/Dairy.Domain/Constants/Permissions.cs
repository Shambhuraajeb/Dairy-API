namespace Dairy.Domain.Constants;

/// <summary>
/// One const per permission code. Naming rule: PascalCase each dot segment
/// (farmer.view -> FarmerView, collection.quality.record -> CollectionQualityRecord).
/// </summary>
public static class Permissions
{
    public const string FarmerView = "farmer.view";
    public const string FarmerCreate = "farmer.create";
    public const string FarmerUpdate = "farmer.update";

    public const string LoanView = "loan.view";
    public const string LoanCreate = "loan.create";
    public const string LoanUpdate = "loan.update";

    public const string AnimalView = "animal.view";
    public const string AnimalCreate = "animal.create";
    public const string AnimalUpdate = "animal.update";

    public const string CollectionView = "collection.view";
    public const string CollectionCreate = "collection.create";
    public const string CollectionUpdate = "collection.update";
    public const string CollectionApprove = "collection.approve";
    public const string CollectionReject = "collection.reject";
    public const string CollectionQualityRecord = "collection.quality.record";

    public const string RatechartView = "ratechart.view";
    public const string RatechartManage = "ratechart.manage";
    public const string RateChartView = RatechartView;       // alias
    public const string RateChartManage = RatechartManage;   // alias

    public const string PaymentView = "payment.view";
    public const string PaymentCreate = "payment.create";
    public const string PaymentApprove = "payment.approve";

    public const string InventoryView = "inventory.view";
    public const string InventoryCreate = "inventory.create";
    public const string InventoryUpdate = "inventory.update";
    public const string InventoryAdjust = "inventory.adjust";

    public const string SupplierView = "supplier.view";
    public const string SupplierManage = "supplier.manage";

    public const string PurchaseView = "purchase.view";
    public const string PurchaseCreate = "purchase.create";
    public const string PurchaseApprove = "purchase.approve";
    public const string PurchaseReceive = "purchase.receive";

    public const string CustomerView = "customer.view";
    public const string CustomerCreate = "customer.create";
    public const string CustomerUpdate = "customer.update";

    public const string SubscriptionView = "subscription.view";
    public const string SubscriptionCreate = "subscription.create";
    public const string SubscriptionUpdate = "subscription.update";

    public const string ComplaintView = "complaint.view";
    public const string ComplaintCreate = "complaint.create";
    public const string ComplaintUpdate = "complaint.update";

    public const string ProductView = "product.view";
    public const string ProductCreate = "product.create";
    public const string ProductUpdate = "product.update";

    public const string OrderView = "order.view";
    public const string OrderCreate = "order.create";
    public const string OrderUpdate = "order.update";

    public const string RouteView = "route.view";
    public const string RouteManage = "route.manage";

    public const string DeliveryView = "delivery.view";
    public const string DeliveryUpdate = "delivery.update";
    public const string DeliveryConfirm = "delivery.confirm";

    public const string InvoiceView = "invoice.view";
    public const string InvoiceCreate = "invoice.create";

    public const string CustpaymentView = "custpayment.view";
    public const string CustpaymentCreate = "custpayment.create";
    public const string CustPaymentView = CustpaymentView;     // alias
    public const string CustPaymentCreate = CustpaymentCreate; // alias

    public const string ExpenseView = "expense.view";
    public const string ExpenseCreate = "expense.create";
    public const string ExpenseApprove = "expense.approve";

    public const string CashView = "cash.view";

    public const string EmployeeView = "employee.view";
    public const string EmployeeCreate = "employee.create";
    public const string EmployeeUpdate = "employee.update";

    public const string AttendanceView = "attendance.view";
    public const string AttendanceManage = "attendance.manage";

    public const string LeaveView = "leave.view";
    public const string LeaveCreate = "leave.create";
    public const string LeaveApprove = "leave.approve";

    public const string SalaryView = "salary.view";
    public const string SalaryManage = "salary.manage";

    public const string MaintenanceView = "maintenance.view";
    public const string MaintenanceCreate = "maintenance.create";
    public const string MaintenanceUpdate = "maintenance.update";

    public const string ReportView = "report.view";
    public const string NotificationView = "notification.view";
    public const string DashboardView = "dashboard.view";

    public const string UserManage = "user.manage";
    public const string RoleManage = "role.manage";
    public const string SettingsManage = "settings.manage";
    public const string AuditView = "audit.view";

    /// <summary>Every canonical permission code (aliases excluded).</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        FarmerView, FarmerCreate, FarmerUpdate,
        LoanView, LoanCreate, LoanUpdate,
        AnimalView, AnimalCreate, AnimalUpdate,
        CollectionView, CollectionCreate, CollectionUpdate, CollectionApprove, CollectionReject, CollectionQualityRecord,
        RatechartView, RatechartManage,
        PaymentView, PaymentCreate, PaymentApprove,
        InventoryView, InventoryCreate, InventoryUpdate, InventoryAdjust,
        SupplierView, SupplierManage,
        PurchaseView, PurchaseCreate, PurchaseApprove, PurchaseReceive,
        CustomerView, CustomerCreate, CustomerUpdate,
        SubscriptionView, SubscriptionCreate, SubscriptionUpdate,
        ComplaintView, ComplaintCreate, ComplaintUpdate,
        ProductView, ProductCreate, ProductUpdate,
        OrderView, OrderCreate, OrderUpdate,
        RouteView, RouteManage,
        DeliveryView, DeliveryUpdate, DeliveryConfirm,
        InvoiceView, InvoiceCreate,
        CustpaymentView, CustpaymentCreate,
        ExpenseView, ExpenseCreate, ExpenseApprove,
        CashView,
        EmployeeView, EmployeeCreate, EmployeeUpdate,
        AttendanceView, AttendanceManage,
        LeaveView, LeaveCreate, LeaveApprove,
        SalaryView, SalaryManage,
        MaintenanceView, MaintenanceCreate, MaintenanceUpdate,
        ReportView, NotificationView, DashboardView,
        UserManage, RoleManage, SettingsManage, AuditView
    };
}
