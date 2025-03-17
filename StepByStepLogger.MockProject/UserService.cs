using System.Globalization;

namespace MethodTracker.MockProject;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class OrderRequest
{
    public int UserId { get; set; }
    public List<int> ProductIds { get; set; } = [];
    public decimal TotalAmount { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public User Customer { get; set; } = new();
    public List<int> ProductIds { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public Type Type { get; set; }
    public CultureInfo CultureInfo { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = "";
}

public class OrderService
{
    public async Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        Console.WriteLine("OrderService: ProcessOrderAsync started.");
        ValidateOrder(request);
        var userService = new UserService();
        var customer = userService.GetUser(request.UserId);
        var order = BuildOrder(request, customer);
        var inventoryService = new InventoryService();
        var inventoryReserved = await inventoryService.ReserveInventoryAsync(order);
        var paymentService = new PaymentService();
        var paymentResult = await paymentService.ChargeUserAsync(customer, order);
        SaveOrder(order);
        var notificationService = new NotificationService();
        notificationService.SendOrderConfirmation(order);

        Console.WriteLine("OrderService: ProcessOrderAsync completed.");

        throw new Exception("Test exception");
        return order;
    }

    private void ValidateOrder(OrderRequest request)
    {
        Console.WriteLine("OrderService: Validating order request.");
    }

    private Order BuildOrder(OrderRequest request, User customer)
    {
        Console.WriteLine("OrderService: Building order.");
        return new Order
        {
            OrderId = new Random().Next(1000, 9999),
            Customer = customer,
            ProductIds = request.ProductIds,
            TotalAmount = request.TotalAmount,
            OrderDate = DateTime.Now,
            Type = GetType(),
            CultureInfo = CultureInfo.CurrentCulture
        };
    }

    private void SaveOrder(Order order)
    {
        Console.WriteLine("OrderService: Saving order.");
        LogOrder(order);
    }

    private void LogOrder(Order order)
    {
        Console.WriteLine($"OrderService: Order {order.OrderId} for {order.Customer.Name} logged.");
    }
}

public class PaymentService
{
    public async Task<PaymentResult> ChargeUserAsync(User user, Order order)
    {
        Console.WriteLine("PaymentService: Charging user.");
        await Task.Delay(300);
        var success = order.TotalAmount < 1000;
        var transactionId = success ? Guid.NewGuid().ToString() : "";
        Console.WriteLine("PaymentService: Charge completed.");
        return new PaymentResult { Success = success, TransactionId = transactionId };
    }
}

public class InventoryService
{
    public async Task<bool> ReserveInventoryAsync(Order order)
    {
        Console.WriteLine("InventoryService: Reserving inventory.");
        await Task.Delay(200);
        var success = order.ProductIds.Count <= 3;
        Console.WriteLine("InventoryService: Reservation " + (success ? "successful." : "failed."));
        return success;
    }
}

public class NotificationService
{
    public void SendOrderConfirmation(Order order)
    {
        Console.WriteLine($"NotificationService: Sending confirmation for Order {order.OrderId} to {order.Customer.Name}.");
        DoNotificationWork();
    }

    private void DoNotificationWork()
    {
        Console.WriteLine("NotificationService: Doing additional notification work.");
    }

    public void Throw()
    {
        throw new Exception("test exc");
    }
}

public class UserService
{
    public User GetUser(int id)
    {
        Console.WriteLine("UserService: Getting user.");
        var name = GenerateUserName(id);
        var age = CalculateUserAge(id);
        return new User { Id = id, Name = name, Age = age };
    }

    private string GenerateUserName(int id)
    {
        Console.WriteLine("UserService: Generating user name.");
        return $"User-{id}";
    }

    private int CalculateUserAge(int id)
    {
        Console.WriteLine("UserService: Calculating user age.");
        return 20 + id % 10;
    }
}