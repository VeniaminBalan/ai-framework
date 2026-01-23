# DDD Reference

Detailed rules and conventions for Domain-Driven Design tactical patterns.

## Aggregates

### Definition
An aggregate is a cluster of domain objects (entities and value objects) that are treated as a single unit for data changes. Each aggregate has a root entity (aggregate root) that controls access to the aggregate.

### Rules
- Aggregates are **consistency boundaries** - all invariants within an aggregate are enforced on every change
- Only the **aggregate root** can be referenced from outside
- Other aggregates are referenced by **ID only**, never by direct reference
- Aggregates should be **small** - prefer smaller aggregates over larger ones
- Changes to multiple aggregates require **eventual consistency** (domain events)

### Aggregate Root Responsibilities
- Protect invariants of the entire aggregate
- Control all access to child entities
- Publish domain events for significant state changes
- Validate all operations before applying them

## Entities

### Definition
Objects that have a distinct identity that runs through time and different states.

### Rules
- Identity is what matters, not attributes
- Two entities with same attributes but different IDs are different
- Entities are mutable but must protect their invariants
- Entities should encapsulate behavior, not just data

## Value Objects

### Definition
Objects that are defined by their attributes rather than identity. Two value objects with the same attributes are considered equal.

### Rules
- **Immutable** - once created, cannot be changed
- **No identity** - equality based on attributes
- **Self-validating** - invalid state cannot exist
- **Side-effect free** - methods return new instances
- Replace rather than modify

### Common Value Objects
- Money (amount + currency)
- Address
- DateRange
- Email
- PhoneNumber
- Quantity

## Domain Events

### Definition
A record of something significant that happened in the domain. Events are named in past tense.

### Rules
- Named in **past tense** (OrderPlaced, UserRegistered)
- **Immutable** - events are facts that happened
- Contain all data needed by handlers
- Published by aggregate roots after state changes
- Enable eventual consistency across aggregates

### Naming Convention
`{Entity}{Action}Event` or `{Entity}{Action}`
- `OrderPlacedEvent`
- `PaymentReceivedEvent`
- `UserEmailChangedEvent`

## Domain Services

### Definition
Stateless operations that don't naturally fit within an entity or value object.

### When to Use
- Operation involves multiple aggregates
- Operation requires external services (payment processing)
- Complex calculations that don't belong to a single entity
- Operations that need repository access

### Rules
- Stateless
- Named after domain operations (not technical concerns)
- Can coordinate multiple aggregates
- Should not replace entity behavior

## Bounded Contexts

### Definition
A boundary within which a particular domain model applies. The same concept may have different meanings in different contexts.

### Rules
- Each context has its own ubiquitous language
- Models don't leak between contexts
- Communication between contexts via events or APIs
- Anti-corruption layers translate between contexts

## File Organization

```
Domain/
├── Aggregates/
│   ├── Order/
│   │   ├── Order.cs              (Aggregate Root)
│   │   ├── OrderLine.cs          (Entity)
│   │   └── OrderStatus.cs        (Value Object)
│   └── Customer/
│       ├── Customer.cs           (Aggregate Root)
│       └── CustomerAddress.cs    (Value Object)
├── ValueObjects/
│   ├── Money.cs
│   ├── Email.cs
│   └── DateRange.cs
├── DomainEvents/
│   ├── OrderPlacedEvent.cs
│   └── PaymentReceivedEvent.cs
├── DomainServices/
│   └── PricingService.cs
└── Exceptions/
    └── DomainException.cs
```

## Anti-Patterns to Avoid

### ❌ Anemic Domain Model
Entities with only getters/setters and all logic in services.

### ❌ Large Aggregates
Aggregates that try to encompass too much, causing concurrency issues.

### ❌ Direct Aggregate References
Referencing other aggregates by object reference instead of ID.

### ❌ Exposing Internal State
Allowing external code to modify aggregate internals directly.

### ❌ Domain Logic in Application Layer
Business rules implemented in services instead of entities.
