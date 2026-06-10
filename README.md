# Utilities

A `netstandard2.0` utility library providing cross-cutting concerns for .NET applications:
structured logging helpers, dependency call tracing, and validation failure reporting.

![CI](https://github.com/drrky-g/utilitites/actions/workflows/ci.yml/badge.svg)

---

## Contents

| Namespace | What's in it |
|---|---|
| `Utilities.Logging` | `LoggingExtensions` — operation timing (`OperationCompleted`, `OperationFailed`), dependency call tracing (`DependencyCallStarted/Completed/Failed`), validation failures (`ValidationFailed`), and structured scopes (`BeginOperationScope`) |

---

## Getting Started

```bash
git clone https://github.com/drrky-g/utilitites.git
cd utilitites
dotnet restore
dotnet build
dotnet test
```

### Requirements

- .NET SDK 8.0, 9.0, or 10.0

---

## Usage

```csharp
using Utilities.Logging;

public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public async Task ProcessAsync(Order order)
    {
        using var scope = _logger.BeginOperationScope("ProcessOrder", order.CorrelationId);

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.DependencyCallStarted("PaymentGateway", "https://pay.example.com");
            await _paymentClient.ChargeAsync(order);
            _logger.DependencyCallCompleted("PaymentGateway", 200, sw.Elapsed.TotalMilliseconds);

            _logger.OperationCompleted("ProcessOrder", sw.Elapsed.TotalMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            _logger.DependencyCallFailed("PaymentGateway", sw.Elapsed.TotalMilliseconds, ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("ProcessOrder", sw.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }

    public bool ValidateOrder(Order order)
    {
        if (string.IsNullOrEmpty(order.Email))
        {
            _logger.ValidationFailed("Email", "must not be empty");
            return false;
        }
        return true;
    }
}
```

---

## API Reference

All extension methods live on `ILogger` in the `Utilities.Logging` namespace.

### Operation timing

| Method | Log level | Event ID |
|---|---|---|
| `OperationCompleted(operationName, elapsedMs)` | Information | 1000 |
| `OperationFailed(operationName, elapsedMs, exception?)` | Error | 1001 |

### Dependency / external call tracing

| Method | Log level | Event ID |
|---|---|---|
| `DependencyCallStarted(dependencyName, targetAddress)` | Debug | 2000 |
| `DependencyCallCompleted(dependencyName, statusCode, elapsedMs)` | Debug | 2001 |
| `DependencyCallFailed(dependencyName, elapsedMs, exception?)` | Error | 2002 |

### Validation

| Method | Log level | Event ID |
|---|---|---|
| `ValidationFailed(fieldName, reason)` | Warning | 3000 |

### Scope helpers

| Method | Returns |
|---|---|
| `BeginOperationScope(operationName, correlationId?)` | `IDisposable?` — attaches `OperationName` and `CorrelationId` as structured properties to all log entries within the scope |

---

## Project Structure

```
.
├── src/
│   └── Utilities/
│       ├── Utilities.csproj          # netstandard2.0 library
│       └── Logging/
│           └── LoggingExtensions.cs
├── tests/
│   └── Utilities.Tests/
│       ├── Utilities.Tests.csproj    # net10.0 xUnit project
│       ├── Helpers/
│       │   └── CapturingLogger.cs    # test-only ILogger
│       └── Logging/
│           └── LoggingExtensionsTests.cs
├── .github/
│   └── workflows/
│       └── ci.yml                    # build + test on .NET 8 & 9
├── Directory.Build.props             # centralised versions
└── Utilities.sln
```

---

## CI/CD

GitHub Actions runs on every push and pull request to `main` / `develop`:

- Builds in **Release** configuration
- Runs all xUnit tests with **code coverage** (Coverlet / Cobertura)
- Uploads `.trx` test results and `coverage.cobertura.xml` as artifacts
- Matrix tests against **.NET 8 and .NET 9**
- A single `ci-passed` gate job is provided — add that to your branch protection rules

---

## Adding a Backend (Optional)

This library only depends on `Microsoft.Extensions.Logging.Abstractions`.
Wire in any backend in your application host — it all just works:

```csharp
// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// OpenTelemetry
builder.Logging.AddOpenTelemetry(otel => otel.AddOtlpExporter());
```

---

## Contributing

1. Branch from `develop`
2. All tests must pass — `dotnet test`
3. No warnings (warnings are treated as errors in the build)
4. Open a PR targeting `develop`
