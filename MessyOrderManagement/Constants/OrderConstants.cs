namespace MessyOrderManagement.Constants;

public static class OrderConstants
{
    // Order Status Values
    public const string StatusPending = "Pending";
    public const string StatusActive = "Active";
    public const string StatusCompleted = "Completed";
    public const string StatusShipped = "Shipped";

    // Business Rules
    public const int MaxDaysForActivation = 30;
    public const int BusinessHoursStart = 8;
    public const int BusinessHoursEnd = 18;

    // Default Values
    public const int DefaultCustomerId = 1;
    public const int DefaultProductId = 1;
    public const int DefaultQuantity = 1;
    public const decimal DefaultPrice = 1;
    public const int ZeroValue = 0;
}
