# DateTime Helper - Pakistan Standard Time Usage Guide

## Overview
This application has been configured to use **Pakistan Standard Time (PKT - UTC+5)** throughout the application, regardless of the server's timezone location.

## DateTimeHelper Class
A utility class has been created at `CommonUtilities/DateTimeHelper.cs` that provides methods to work with Pakistan Standard Time.

## How to Use

### Replace DateTime.Now
Instead of using `DateTime.Now`, use `DateTimeHelper.Now`:

**Before:**
```csharp
existingStock.ModifiedDate = DateTime.Now;
```

**After:**
```csharp
using IMS.CommonUtilities;

existingStock.ModifiedDate = DateTimeHelper.Now;
```

### Replace DateTime.Today
Instead of using `DateTime.Today`, use `DateTimeHelper.Today`:

**Before:**
```csharp
var today = DateTime.Today;
```

**After:**
```csharp
using IMS.CommonUtilities;

var today = DateTimeHelper.Today;
```

### Convert Existing DateTime to Pakistan Time
If you have a DateTime value that needs to be converted to Pakistan time:

```csharp
using IMS.CommonUtilities;

// Convert UTC DateTime to Pakistan time
DateTime utcDateTime = DateTime.UtcNow;
DateTime pakistanTime = DateTimeHelper.GetPakistanTime(utcDateTime);

// Convert any DateTime to Pakistan time
DateTime anyDateTime = DateTime.Now;
DateTime pakistanTime = DateTimeHelper.ToPakistanTime(anyDateTime);
```

### Convert Pakistan Time to UTC (for database storage)
If you need to store in UTC format in the database:

```csharp
using IMS.CommonUtilities;

DateTime pakistanTime = DateTimeHelper.Now;
DateTime utcTime = DateTimeHelper.GetUtcTime(pakistanTime);
```

## Configuration

### Program.cs
The application has been configured in `Program.cs` to:
- Set default culture to `en-PK` (English - Pakistan)
- Configure request localization for Pakistan

### appsettings.json
Timezone settings have been added to `appsettings.json`:
```json
"ApplicationSettings": {
  "DefaultTimeZone": "Pakistan Standard Time",
  "TimeZoneOffset": "+05:00",
  "Culture": "en-PK"
}
```

## Migration Steps

To migrate existing code:

1. **Add using statement:**
   ```csharp
   using IMS.CommonUtilities;
   ```

2. **Replace all instances:**
   - `DateTime.Now` → `DateTimeHelper.Now`
   - `DateTime.Today` → `DateTimeHelper.Today`
   - `DateTime.UtcNow` → Use `DateTimeHelper.Now` (if you want Pakistan time) or keep `DateTime.UtcNow` if you specifically need UTC

3. **For database operations:**
   - If your database stores UTC, convert before saving: `DateTimeHelper.GetUtcTime(DateTimeHelper.Now)`
   - If your database stores local time, use: `DateTimeHelper.Now`

## Important Notes

- **Server Timezone:** The server may be in US timezone, but the application will always use Pakistan Standard Time
- **Database Storage:** Consider whether your database should store UTC or Pakistan time based on your requirements
- **Display:** All dates/times displayed to users will be in Pakistan Standard Time
- **Compatibility:** The DateTimeHelper works on both Windows and Linux servers

## Example Controller Update

```csharp
using IMS.CommonUtilities;

public class StockController : Controller
{
    public IActionResult UpdateStock(Stock stock)
    {
        // Instead of DateTime.Now
        stock.ModifiedDate = DateTimeHelper.Now;
        stock.CreatedDate = DateTimeHelper.Now;
        
        // Your code here...
    }
}
```

## Testing

To verify the timezone is working correctly:
1. Check that `DateTimeHelper.Now` returns time in PKT (UTC+5)
2. Compare with server time - there should be a 5-hour difference (or more depending on server timezone)
3. All date displays in the UI should show Pakistan time

