# Datadog MAUI SDK Documentation

## Table of Contents

- [Installation & Setup](#installation--setup)
- [Datadog Class](#datadog-class)
- [RUM (Real User Monitoring)](#rum-real-user-monitoring)
- [Logs](#logs)

---

## Installation & Setup

### MauiProgram.cs (recommended)

Use `UseDatadog` in your `MauiProgram.cs` to initialize the SDK when the app starts:

```csharp
using Datadog.Maui.Extensions;
using Datadog.Maui;
using Datadog.Maui.Configuration;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseDatadog(config =>
            {
                config.ClientToken = "YOUR_CLIENT_TOKEN";
                config.Environment = "production";
                config.ServiceName = "my-maui-app";
                config.Site = DatadogSite.US1;
                config.TrackingConsent = TrackingConsent.Pending;

                config.EnableRum(rum =>
                {
                    rum.SetApplicationId("YOUR_RUM_APP_ID")
                       .SetSessionSampleRate(100)
                       .TrackViewsAutomatically(true)
                       .TrackUserInteractions(true)
                       .TrackResources(true)
                       .TrackErrors(true);
                });

                config.EnableLogs(logs =>
                {
                    logs.SetSampleRate(100)
                        .EnableNetworkInfo(true)
                        .BundleWithRum(true);
                });
            });

        return builder.Build();
    }
}
```

### Manual initialization

You can also initialize using `DatadogConfiguration.Builder` directly:

```csharp
using Datadog.Maui;
using Datadog.Maui.Configuration;

var config = new DatadogConfiguration.Builder("YOUR_CLIENT_TOKEN")
    .SetEnvironment("production")
    .SetServiceName("my-maui-app")
    .SetSite(DatadogSite.EU1)
    .SetTrackingConsent(TrackingConsent.Pending)
    .AddGlobalTag("version", "2.1.0")
    .SetFirstPartyHosts("api.myapp.com", "cdn.myapp.com")
    .EnableRum(rum =>
    {
        rum.SetApplicationId("YOUR_RUM_APP_ID");
    })
    .EnableLogs(logs =>
    {
        logs.SetSampleRate(80);
    })
    .Build();

Datadog.Initialize(config);
```

### DatadogSite values

| Value | Region |
|-------|--------|
| `DatadogSite.US1` | United States (datadoghq.com) — default |
| `DatadogSite.US3` | United States (us3.datadoghq.com) |
| `DatadogSite.US5` | United States (us5.datadoghq.com) |
| `DatadogSite.EU1` | Europe (datadoghq.eu) |
| `DatadogSite.US1_FED` | US Government (ddog-gov.com) |
| `DatadogSite.AP1` | Asia Pacific (ap1.datadoghq.com) |

### TrackingConsent values

| Value | Behavior |
|-------|----------|
| `TrackingConsent.Granted` | Tracking starts immediately |
| `TrackingConsent.NotGranted` | No tracking occurs |
| `TrackingConsent.Pending` | Events are buffered locally until consent is granted or denied |

---

## Datadog Class

`Datadog.Maui.Datadog` is the main entry point for SDK-level operations.

### Check initialization status

```csharp
if (Datadog.IsInitialized)
{
    // Safe to use Datadog APIs
}
```

### Set user information

Set user info after a successful login to correlate all RUM and log events with the authenticated user:

```csharp
Datadog.SetUser(new UserInfo
{
    Id = "usr-12345",
    Name = "Jane Smith",
    Email = "jane.smith@example.com",
    ExtraInfo = new Dictionary<string, object>
    {
        { "plan", "premium" },
        { "account_age_days", 365 }
    }
});
```

### Clear user information

Call this on logout to stop associating events with the previous user:

```csharp
Datadog.ClearUser();
```

### Update tracking consent

Update consent after showing a consent dialog to the user:

```csharp
// User accepted — start sending buffered events
Datadog.SetTrackingConsent(TrackingConsent.Granted);

// User declined — discard buffered events and stop tracking
Datadog.SetTrackingConsent(TrackingConsent.NotGranted);
```

### Set global tags

Attach tags to all events (RUM, Logs, Traces):

```csharp
Datadog.SetTags(new Dictionary<string, string>
{
    { "build_number", "1042" },
    { "feature_flags", "checkout_v2" }
});
```

### Add and remove global attributes

Global attributes are added to all RUM events. Use `AddAttribute` or `SetAttribute` (they are equivalent — `SetAttribute` overwrites any existing value for the key):

```csharp
// Add a new attribute
Datadog.AddAttribute("user_tier", "gold");

// Overwrite an existing attribute
Datadog.SetAttribute("user_tier", "platinum");

// Remove an attribute
Datadog.RemoveAttribute("user_tier");
```

---

## RUM (Real User Monitoring)

`Datadog.Maui.Rum.Rum` provides the static API for tracking views, user actions, resources, errors, and custom timings.

RUM must be enabled in configuration before these methods have any effect. See [Installation & Setup](#installation--setup).

### Track views manually

Use this when `TrackViewsAutomatically` is `false` or when you need precise control over view lifecycle:

```csharp
using Datadog.Maui.Rum;

// In your page's OnAppearing
protected override void OnAppearing()
{
    base.OnAppearing();
    Rum.StartView("ProductDetailPage", "Product Detail", new Dictionary<string, object>
    {
        { "product_id", "sku-9876" },
        { "category", "electronics" }
    });
}

// In your page's OnDisappearing
protected override void OnDisappearing()
{
    base.OnDisappearing();
    Rum.StopView("ProductDetailPage");
}
```

### Track user actions

Record discrete user interactions that do not have a duration:

```csharp
// Button tap
Rum.AddAction(RumActionType.Tap, "Add to Cart Button", new Dictionary<string, object>
{
    { "product_id", "sku-9876" },
    { "quantity", 2 }
});

// Custom action (e.g., a business event)
Rum.AddAction(RumActionType.Custom, "Checkout Completed", new Dictionary<string, object>
{
    { "order_total", 49.99 },
    { "item_count", 3 }
});
```

Available `RumActionType` values: `Tap`, `Scroll`, `Swipe`, `Click`, `Custom`.

### Track network resources

Manually instrument HTTP calls when the automatic resource tracking is insufficient:

```csharp
var resourceKey = Guid.NewGuid().ToString();

Rum.StartResource(resourceKey, "POST", "https://api.myapp.com/orders");

try
{
    var response = await httpClient.PostAsync("/orders", content);
    Rum.StopResource(
        key: resourceKey,
        statusCode: (int)response.StatusCode,
        size: response.Content.Headers.ContentLength,
        kind: RumResourceKind.Native
    );
}
catch (Exception ex)
{
    Rum.StopResourceWithError(resourceKey, ex);
}
```

Available `RumResourceKind` values: `Image`, `Xhr`, `Beacon`, `Css`, `Document`, `Font`, `Js`, `Media`, `Native`, `Other`.

### Add errors

Report non-fatal errors from source code:

```csharp
try
{
    ProcessPayment();
}
catch (PaymentException ex)
{
    // From an exception object
    Rum.AddError(ex, RumErrorSource.Source, new Dictionary<string, object>
    {
        { "payment_method", "credit_card" }
    });
}

// Or report by message without an exception
Rum.AddError(
    message: "Payment gateway timeout",
    source: RumErrorSource.Network,
    attributes: new Dictionary<string, object> { { "gateway", "stripe" } }
);
```

Available `RumErrorSource` values: `Source`, `Network`, `WebView`, `Custom`.

### Add custom timings

Mark a point in time relative to the start of the current view — useful for measuring when key content becomes ready:

```csharp
Rum.StartView("HomePageKey", "Home");

await LoadFeaturedProducts();
Rum.AddTiming("featured_products_loaded");

await LoadRecommendations();
Rum.AddTiming("recommendations_loaded");
```

### Manage RUM sessions

```csharp
// Force-start a new session (e.g., after logout/login)
Rum.StartSession();

// Stop the current session
Rum.StopSession();
```

### RUM-scoped attributes

These attributes are added only to RUM events (not logs):

```csharp
Rum.AddAttribute("ab_test_group", "variant_b");

// Later, when the experiment ends
Rum.RemoveAttribute("ab_test_group");
```

---

## Logs

The Logs API uses named loggers created from `Datadog.Maui.Logs.Logs`. Logs must be enabled in configuration. See [Installation & Setup](#installation--setup).

### Configure Logs

```csharp
config.EnableLogs(logs =>
{
    logs.SetSampleRate(100)      // 0-100, percentage of logs to send
        .EnableNetworkInfo(true) // attach network info to each log
        .BundleWithRum(true);    // correlate logs with the active RUM session
});
```

### Create a logger

Loggers are singletons keyed by name — calling `CreateLogger` with the same name returns the same instance:

```csharp
using Datadog.Maui.Logs;

private static readonly ILogger _logger = Logs.CreateLogger("CheckoutService");
```

### Log at different levels

```csharp
// Debug — detailed diagnostic information
_logger.Debug("Cart loaded", attributes: new Dictionary<string, object>
{
    { "item_count", 4 }
});

// Info — general operational events
_logger.Info("User navigated to checkout");

// Notice — normal but significant events
_logger.Notice("Promotional code applied", attributes: new Dictionary<string, object>
{
    { "code", "SAVE10" },
    { "discount", 10.00 }
});

// Warn — unexpected situations that are not errors
_logger.Warn("Payment retry attempt", attributes: new Dictionary<string, object>
{
    { "attempt", 2 },
    { "max_attempts", 3 }
});

// Error — errors that need attention
_logger.Error("Payment failed", error: ex, attributes: new Dictionary<string, object>
{
    { "order_id", "ord-55123" }
});

// Critical — severe failures requiring immediate action
_logger.Critical("Payment service unreachable", error: serviceException);
```

### Log using the LogLevel enum

```csharp
using Datadog.Maui.Logs;

_logger.Log(LogLevel.Info, "Feature flag evaluated", attributes: new Dictionary<string, object>
{
    { "flag", "new_checkout_flow" },
    { "value", true }
});
```

### Per-logger attributes and tags

Attributes and tags added to a logger are included on every message that logger sends:

```csharp
// Add context for the lifetime of a request
_logger.AddAttribute("request_id", requestId);
_logger.AddTag("region", "us-east-1");

try
{
    await ProcessOrder(order);
    _logger.Info("Order processed successfully");
}
finally
{
    // Clean up when done
    _logger.RemoveAttribute("request_id");
    _logger.RemoveTag("region");
}
```

### Global log attributes and tags

These apply to all loggers:

```csharp
// Attach build metadata to every log
Logs.AddAttribute("app_version", AppInfo.VersionString);
Logs.AddTag("build_type", "release");

// Remove when no longer relevant
Logs.RemoveAttribute("app_version");
Logs.RemoveTag("build_type");
```

### Complete logger example

```csharp
using Datadog.Maui.Logs;

public class OrderService
{
    private static readonly ILogger _logger = Logs.CreateLogger("OrderService");

    public async Task<Order> PlaceOrderAsync(Cart cart, PaymentInfo payment)
    {
        _logger.Info("Placing order", attributes: new Dictionary<string, object>
        {
            { "item_count", cart.Items.Count },
            { "cart_total", cart.Total }
        });

        try
        {
            var order = await _api.CreateOrderAsync(cart, payment);

            _logger.Info("Order placed successfully", attributes: new Dictionary<string, object>
            {
                { "order_id", order.Id },
                { "total", order.Total }
            });

            return order;
        }
        catch (PaymentDeclinedException ex)
        {
            _logger.Warn("Payment declined", error: ex, attributes: new Dictionary<string, object>
            {
                { "reason", ex.DeclineCode }
            });
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error("Unexpected error placing order", error: ex);
            throw;
        }
    }
}
```
