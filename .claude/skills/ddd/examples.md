# DDD Examples

## Aggregate Root

```csharp
public class Order : AggregateRoot
{
    private readonly List<OrderLine> _orderLines = new();

    public int Id { get; private set; }
    public int CustomerId { get; private set; }  // Reference by ID only
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(int customerId)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Draft,
            TotalAmount = Money.Zero,
            CreatedAt = DateTime.UtcNow
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    public void AddLine(int productId, string productName, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify non-draft order");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        var existingLine = _orderLines.FirstOrDefault(l => l.ProductId == productId);
        if (existingLine != null)
        {
            existingLine.IncreaseQuantity(quantity);
        }
        else
        {
            var line = new OrderLine(productId, productName, quantity, unitPrice);
            _orderLines.Add(line);
        }

        RecalculateTotal();
    }

    public void RemoveLine(int productId)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify non-draft order");

        var line = _orderLines.FirstOrDefault(l => l.ProductId == productId);
        if (line == null)
            throw new DomainException($"Product {productId} not in order");

        _orderLines.Remove(line);
        RecalculateTotal();
    }

    public void Place()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Order already placed");

        if (!_orderLines.Any())
            throw new DomainException("Cannot place empty order");

        Status = OrderStatus.Placed;
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot cancel shipped order");

        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id, reason));
    }

    private void RecalculateTotal()
    {
        TotalAmount = _orderLines
            .Select(l => l.LineTotal)
            .Aggregate(Money.Zero, (sum, amount) => sum.Add(amount));
    }
}
```

## Entity (Child of Aggregate)

```csharp
public class OrderLine : Entity
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderLine() { } // EF Core

    internal OrderLine(int productId, string productName, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be positive");

        Quantity += amount;
    }

    internal void DecreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be positive");

        if (Quantity - amount < 1)
            throw new DomainException("Quantity cannot be less than 1");

        Quantity -= amount;
    }
}
```

## Value Object

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Zero => new(0, "USD");

    private Money() { } // EF Core

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required");

        if (currency.Length != 3)
            throw new DomainException("Currency must be 3-letter ISO code");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(int multiplier)
    {
        return new Money(Amount * multiplier, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot operate on different currencies: {Currency} vs {other.Currency}");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

## Value Object - Email

```csharp
public class Email : ValueObject
{
    public string Value { get; }

    private Email() { } // EF Core

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email is required");

        if (!IsValidEmail(value))
            throw new DomainException("Invalid email format");

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
}
```

## Value Object - DateRange

```csharp
public class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public int TotalDays => (End - Start).Days;

    private DateRange() { } // EF Core

    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
            throw new DomainException("End date must be after start date");

        Start = start.Date;
        End = end.Date;
    }

    public bool Contains(DateTime date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

## Base Classes

```csharp
public abstract class Entity
{
    public int Id { get; protected set; }

    public override bool Equals(object obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == default || other.Id == default)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(Entity a, Entity b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity a, Entity b) => !(a == b);
}

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject a, ValueObject b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(ValueObject a, ValueObject b) => !(a == b);
}
```

## Domain Events

```csharp
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class OrderPlacedEvent : DomainEvent
{
    public int OrderId { get; }
    public int CustomerId { get; }
    public Money TotalAmount { get; }

    public OrderPlacedEvent(int orderId, int customerId, Money totalAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}

public class OrderCancelledEvent : DomainEvent
{
    public int OrderId { get; }
    public string Reason { get; }

    public OrderCancelledEvent(int orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}
```

## Domain Service

```csharp
public interface IPricingService
{
    Money CalculateDiscount(Order order, Customer customer);
}

public class PricingService : IPricingService
{
    public Money CalculateDiscount(Order order, Customer customer)
    {
        var discount = Money.Zero;

        // Loyalty discount
        if (customer.IsLoyalCustomer)
        {
            discount = discount.Add(order.TotalAmount.Multiply(10).Divide(100));
        }

        // Volume discount
        if (order.OrderLines.Sum(l => l.Quantity) > 10)
        {
            discount = discount.Add(order.TotalAmount.Multiply(5).Divide(100));
        }

        return discount;
    }
}
```

## Repository Interface (Domain Layer)

```csharp
// Repository interface in Domain layer - one per aggregate root
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(int id);
    Task<Order> GetByIdWithLinesAsync(int id);
    Task AddAsync(Order order);
    void Update(Order order);
    void Remove(Order order);
}

// No repository for OrderLine - it's accessed through Order aggregate
```

## Domain Exception

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
```
