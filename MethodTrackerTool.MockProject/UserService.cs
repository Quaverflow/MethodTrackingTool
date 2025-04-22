using System.Globalization;

#pragma warning disable IDE0060
namespace MethodTracker.MockProject
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class OrderRequest
    {
        public int UserId { get; set; }
        public List<int> ProductIds { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public Func<string> X { get; set; } = () => string.Empty;
    }

    public class Order
    {
        public string OrderId { get; set; }
        public User Customer { get; set; }
        public List<int> ProductIds { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public CultureInfo Culture { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
    }

    public class OrderService
    {
        private readonly string _orderId;
        private readonly DateTime _now;
        private readonly IAuthService _auth;
        private readonly IValidationService _validation;
        private readonly IPricingService _pricing;
        private readonly IDiscountService _discount;
        private readonly ITaxService _tax;
        private readonly IInventoryService _inventory;
        private readonly IPaymentService _payment;
        private readonly IShippingService _shipping;
        private readonly IRecommendationService _recommend;
        private readonly IAnalyticsService _analytics;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;
        private readonly IBulkProcessingService _bulk;

        public OrderService(string orderId, DateTime now)
        {
            _orderId = orderId;
            _now = now;
            _auth = new AuthService();
            _validation = new ValidationService();
            _pricing = new PricingService();
            _discount = new DiscountService();
            _tax = new TaxService();
            _inventory = new InventoryService();
            _payment = new PaymentService();
            _shipping = new ShippingService();
            _recommend = new RecommendationService();
            _analytics = new AnalyticsService();
            _audit = new AuditService();
            _notify = new NotificationService();
            _bulk = new BulkProcessingService();
        }

        public async Task<Order> ProcessOrderAsync(OrderRequest req)
        {
            _auth.Authorize(req.UserId);
            _validation.Validate(req);
            var user = new UserService().GetUser(req.UserId);
            var subtotal = _pricing.Calculate(req.ProductIds);
            var discount = _discount.Apply(req.X(), subtotal);
            var taxed = _tax.Apply(subtotal - discount);
            var order = BuildOrder(req, user, subtotal, discount, taxed);
            var reserved = await _inventory.ReserveAsync(order.ProductIds);
            var payment = await _payment.ChargeAsync(user, order.Total);
            var shipped = await _shipping.ScheduleAsync(order);
            var recs = _recommend.GetRecommendations(user.Id);
            _analytics.TrackOrder(order, payment, shipped, recs);
            _audit.Record(order, user, payment, shipped);
            _notify.Confirm(order, user);
            _bulk.Process(order.ProductIds);
            return order;
        }

        private Order BuildOrder(OrderRequest req, User user, decimal subtotal, decimal discount, decimal taxed)
        {
            var fee = _shipping.Estimate(req.ProductIds);
            return new Order
            {
                OrderId = _orderId,
                Customer = user,
                ProductIds = req.ProductIds,
                Subtotal = subtotal,
                Discount = discount,
                Tax = taxed,
                ShippingFee = fee,
                Total = subtotal - discount + taxed + fee,
                OrderDate = _now,
                Culture = CultureInfo.InvariantCulture
            };
        }
    }

    public interface IAuthService { void Authorize(int userId); }
    public interface IValidationService { void Validate(OrderRequest req); }
    public interface IPricingService { decimal Calculate(List<int> products); }
    public interface IDiscountService { decimal Apply(string promo, decimal amount); }
    public interface ITaxService { decimal Apply(decimal amount); }
    public interface IInventoryService { Task<bool> ReserveAsync(List<int> products); }
    public interface IPaymentService { Task<PaymentResult> ChargeAsync(User user, decimal amount); }
    public interface IShippingService { decimal Estimate(List<int> products); Task<bool> ScheduleAsync(Order order); }
    public interface INotificationService { void Confirm(Order order, User user); }
    public interface IAuditService { void Record(Order order, User user, PaymentResult pr, bool shipped); }
    public interface IRecommendationService { List<string> GetRecommendations(int userId); }
    public interface IAnalyticsService { void TrackOrder(Order order, PaymentResult pr, bool shipped, List<string> recs); }
    public interface IBulkProcessingService { void Process(List<int> items); }

    internal class AuthService : IAuthService { public void Authorize(int userId) { } }
    internal class ValidationService : IValidationService
    {
        public void Validate(OrderRequest req) => new RuleService().Run(req);
    }
    internal class RuleService
    {
        public void Run(OrderRequest req) => new RuleStep1().Execute(req);
    }
    internal class RuleStep1
    {
        public void Execute(OrderRequest req) => new RuleStep2().Execute(req);
    }
    internal class RuleStep2 { public void Execute(OrderRequest req) { } }

    internal class PricingService : IPricingService { public decimal Calculate(List<int> products) { var sum = 0m; foreach (var p in products) { sum += p * 2.5m; new PricingHelper().Adjust(ref sum); } return sum; } }
    internal class PricingHelper { public void Adjust(ref decimal amount) { amount += 1.0m; new PricingDetail().Refine(amount); } }
    internal class PricingDetail { public void Refine(decimal amt) { } }

    internal class DiscountService : IDiscountService
    {
        public decimal Apply(string promo, decimal amount) => promo == "VIP" ? amount * 0.9m : 0m;
    }
    internal class TaxService : ITaxService
    {
        public decimal Apply(decimal amount) => amount * 0.07m;
    }
    internal class InventoryService : IInventoryService
    {
        public async Task<bool> ReserveAsync(List<int> products)
        {
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(10);
                new InventoryValidator().Check(products);
            }
            return true;
        }
    }

    internal class InventoryValidator
    {
        public void Check(List<int> products) { }
    }
    internal class PaymentService : IPaymentService { public async Task<PaymentResult> ChargeAsync(User user, decimal amount) { await Task.Delay(20); return new PaymentResult { Success = true, TransactionId = $"TX{user.Id:0000}" }; } }
    internal class ShippingService : IShippingService { public decimal Estimate(List<int> products) { var fee = products.Count * 1.99m; new ShippingEstimator().Estimate(ref fee); return fee; } public async Task<bool> ScheduleAsync(Order order) { await Task.Delay(10); return true; } }
    internal class ShippingEstimator
    {
        public void Estimate(ref decimal fee) => fee += 2.0m;
    }
    internal class RecommendationService : IRecommendationService
    {
        public List<string> GetRecommendations(int userId)
        {
            var recs = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                recs.Add($"Rec-{userId}-{i}");
            }
            return recs;
        }
    }

    internal class AnalyticsService : IAnalyticsService
    {
        public void TrackOrder(Order o, PaymentResult p, bool s, List<string> r) => new AnalyticsDetail().Log(o, p, s, r);
    }
    internal class AnalyticsDetail { public void Log(Order o, PaymentResult p, bool s, List<string> r) { int count = r.Count; } }
    internal class AuditService : IAuditService
    {
        public void Record(Order o, User u, PaymentResult pr, bool s) => new AuditDetailService().Log(o, u, pr, s);
    }
    internal class AuditDetailService { public void Log(Order o, User u, PaymentResult p, bool s) { } }
    internal class NotificationService : INotificationService { public void Confirm(Order o, User u) { new EmailService().Send(u, o.OrderId); new PushService().Send(u, o.OrderId); } }
    internal class EmailService
    {
        public void Send(User u, string oid) => new EmailFormatter().Format(oid);
    }
    internal class EmailFormatter { public void Format(string oid) { } }
    internal class PushService
    {
        public void Send(User u, string oid) => new PushQueue().Enqueue(u.Id, oid);
    }
    internal class PushQueue { public void Enqueue(int uid, string oid) { } }
    internal class BulkProcessingService : IBulkProcessingService
    {
        public void Process(List<int> items)
        {
            foreach (var item in items)
            {
                Level1(item);
            }
        }
        private void Level1(int item) => Level2(item);

        private void Level2(int item)
        {
            for (int i = 0; i < 10; i++)
            {
                Level3(item, i);
            }
        }
        private void Level3(int item, int i)
        {
            if (i % 2 == 0)
            {
                Level4(item, i);
            }
            else
            {
                Level5(item, i);
            }
        }
        private void Level4(int item, int i) => Level6(item, i);
        private void Level5(int item, int i) => Level6(item, i);

        private void Level6(int item, int i)
        {
            for (int j = 0; j < 10; j++)
            {
                Level7(item, i, j);
            }
        }
        private void Level7(int item, int i, int j) { }
    }
    public class UserService { public User GetUser(int id) => new() { Id = id, Name = $"User-{id}", Age = 20 + id % 10 }; }
}
