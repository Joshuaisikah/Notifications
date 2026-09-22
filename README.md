# Notifications

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      API Layer                             │
│  (Controllers/Endpoints, DTOs, Services, Validators)       │
│  Composition root: wires up Infrastructure's DI here      │
│         ↓                                                 │
│                   Application Layer                        │
│  (Commands, Queries, Handlers, Application Services)      │
│         ↓                                                 │
│                    Domain Layer                            │
│  (Entities, Value Objects, Enums — zero dependencies)     │
│         ↑                                                 │
│               Infrastructure Layer                         │
│  (DbContext, External Services — injected into Api        │
│   Services directly, no repository indirection)           │
└─────────────────────────────────────────────────────────────┘
```

## Layers
- **Api**
- **Application**
- **Infrastructure**
- **Tests**

## Dependency Flow
- API → Application → Domain
- API → Infrastructure (composition root only — Program.cs wiring, no business logic calls this directly)
- Infrastructure → Domain, Application (implements their interfaces)
- Tests → All layers

## Setup
```bash
dotnet restore
dotnet build
cd Notifications.Api
dotnet run
```

## Tests
```bash
dotnet test
```
