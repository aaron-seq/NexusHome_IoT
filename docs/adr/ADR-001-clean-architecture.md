# ADR-001: Clean Architecture Pattern Selection

## Status
Accepted

## Date
2025-11-08

## Context
The NexusHome IoT Platform requires a software architecture that supports:
- Long-term maintainability for a complex IoT system
- Testability for business logic and integrations
- Flexibility to swap external dependencies (databases, MQTT brokers, cloud providers)
- Team scalability as the project grows

## Decision
We adopt **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) with the following layer structure:

1. **Core Layer** (`Core/`): Domain entities and business logic interfaces
2. **Application Layer** (`Application/`): DTOs, Hubs, Validators
3. **Infrastructure Layer** (`Infrastructure/`): External integrations, data access
4. **API Layer** (`Controllers/`): HTTP endpoints and SignalR hubs

### Dependency Rule
Dependencies flow inward only: API → Application → Core ← Infrastructure

## Rationale

### Advantages
- **Testability**: Core business logic has no external dependencies, enabling unit testing without mocks for infrastructure
- **Flexibility**: External systems (SQL Server, Redis, MQTT) can be swapped by implementing new infrastructure adapters
- **Separation of Concerns**: Clear boundaries between business logic and infrastructure
- **Framework Independence**: Core domain is not coupled to ASP.NET Core or Entity Framework

### Trade-offs
- **Initial Complexity**: More boilerplate code for interfaces and dependency injection
- **Learning Curve**: Team members need to understand layer boundaries

## Consequences

### Positive
- Services like `EnergyOptimizationService` can be unit tested without database or MQTT connections
- Infrastructure changes (e.g., switching from SQL Server to PostgreSQL) are isolated
- New team members can understand boundaries quickly

### Negative
- Additional interface definitions for each service
- Mapping between layers requires AutoMapper configuration

## References
- Robert C. Martin, "Clean Architecture" (2017)
- Microsoft .NET Application Architecture Guidance
