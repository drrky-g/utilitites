# Utilities

A `netstandard2.0` utility library providing cross-cutting concerns for .NET applications.
Built on `Microsoft.Extensions.Logging` abstractions — no logging backend is forced on consumers.

![CI](https://github.com/your-org/utilities/actions/workflows/ci.yml/badge.svg)

---

## Contents

| Namespace | What's in it |
|---|---|
| `Utilities.Logging` | `LoggingExtensions` — structured, high-performance logging helpers via `LoggerMessage.Define` |

---

## Getting Started

```bash
git clone https://github.com/your-org/utilities.git
cd utilities
dotnet restore
dotnet build
dotnet test
```

### Requirements

- .NET SDK 8.0 or 9.0

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
        catch (Exception ex)
        {
            _logger.OperationFailed("ProcessOrder", sw.Elapsed.TotalMilliseconds, ex);
            throw;
        }
    }
}
```

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
│       ├── Utilities.Tests.csproj    # net8.0 xUnit project
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
