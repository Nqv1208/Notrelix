# Testing Rules

## Test types per use case

| Test type | Location | Purpose |
|-----------|----------|---------|
| Domain tests | `Notrelix.Domain.Tests/` | State transitions, invariants, domain events |
| Handler tests | `Notrelix.Application.Tests/` | Handler logic, validation, authorization wiring |
| Architecture tests | `Notrelix.Architecture.Tests/` | Classification, context boundary, system context |
| API tests | `Notrelix.API.Tests/` | Endpoint authorization, problem details |

## Rule: Domain tests first

```csharp
// ✅ Test valid state transition
[Fact]
public void Rename_WithValidName_UpdatesName()
{
    var item = CreateBoardItem();
    item.Rename("New Name", userId, clock.UtcNow);
    item.Name.Should().Be("New Name");
}

// ✅ Test invalid transition
[Fact]
public void Rename_WithEmptyName_ThrowsDomainException()
{
    var item = CreateBoardItem();
    Action act = () => item.Rename("", userId, clock.UtcNow);
    act.Should().Throw<DomainException>();
}
```

## Rule: Pipeline behavior tests

Test authorization, validation, and cache behavior independently from handlers.

```csharp
[Fact]
public async Task UnauthorizedRequest_ThrowsUnauthorizedException()
{
    var behavior = new AuthorizationBehavior<TestRequest, Result>(
        logger, tenantContext);

    var act = () => behavior.Handle(
        new TestRequest(), () => Task.FromResult(Result.Success()), CancellationToken.None);

    await act.Should().ThrowAsync<UnauthorizedException>();
}
```

## Rule: Architecture tests

Enforce conventions with file-scanning architecture tests. Do not use `NetArchTest` or similar libraries — scan `*.cs` files directly.

## Rule: Build before commit

Run these before every PR:

```bash
dotnet build --no-restore
dotnet test tests/Notrelix.Architecture.Tests  # ~126 tests
dotnet test tests/Notrelix.Application.Tests    # ~52 tests
dotnet test tests/Notrelix.API.Tests            # ~26 tests
```
