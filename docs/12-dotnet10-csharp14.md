# C# 14 - Nové features

[<< Zpět na index](12-dotnet10-index.md)

---

## 1. Field keyword (semi-auto properties)

Nové kontextové klíčové slovo `field` pro přístup k backing field bez nutnosti ho deklarovat.

```csharp
// ========== STARÉ (C# 13 a dříve) ==========
private string _name;
public string Name
{
    get => _name;
    set => _name = value ?? throw new ArgumentNullException(nameof(value));
}

// ========== NOVÉ (C# 14) ==========
public string Name
{
    get;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

**Více příkladů:**

```csharp
// Validace v setteru
public int Age
{
    get;
    set => field = value >= 0
        ? value
        : throw new ArgumentOutOfRangeException(nameof(value), "Age must be non-negative");
}

// Lazy initialization v getteru
public string FullName
{
    get => field ??= $"{FirstName} {LastName}";
}

// Transformace hodnoty
public string Email
{
    get;
    set => field = value?.ToLowerInvariant()
        ?? throw new ArgumentNullException(nameof(value));
}

// Logging při změně
public decimal Price
{
    get;
    set
    {
        if (field != value)
        {
            _logger.LogInformation("Price changed from {Old} to {New}", field, value);
            field = value;
        }
    }
}

// Notifikace změny (INotifyPropertyChanged pattern)
public string Title
{
    get;
    set
    {
        if (field != value)
        {
            field = value;
            OnPropertyChanged();
        }
    }
}

// Kombinace get a set s různou logikou
public DateTime UpdatedAt
{
    get => field;
    set => field = value > field ? value : field; // Pouze novější datum
}
```

**Konflikty s existující proměnnou `field`:**

```csharp
public class Example
{
    private int field; // Existující proměnná

    public int Value
    {
        get => @field;           // Použij @ pro odlišení
        set => this.field = value; // Nebo this.
    }
}
```

---

## 2. Extension members (nová syntaxe)

C# 14 přidává novou syntaxi pro extension members - nejen metody, ale i properties.

```csharp
// ========== STARÉ (extension methods) ==========
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string value)
        => string.IsNullOrEmpty(value);

    public static string Truncate(this string value, int maxLength)
        => value?.Length > maxLength ? value[..maxLength] + "..." : value;
}

// ========== NOVÉ (C# 14 extension blocks) ==========
public static class StringExtensions
{
    extension(string value)
    {
        // Extension property
        public bool IsNullOrEmpty => string.IsNullOrEmpty(value);

        // Extension method
        public string Truncate(int maxLength)
            => value?.Length > maxLength ? value[..maxLength] + "..." : value;

        // Extension helper method
        public char? SafeCharAt(int index)
            => index >= 0 && index < value?.Length ? value[index] : null;
    }
}

// Použití
string text = "Hello World";
bool empty = text.IsNullOrEmpty;      // Property!
string truncated = text.Truncate(5);  // "Hello..."
char? c = text.SafeCharAt(0);         // 'H'
```

**Poznámky:**
- `extension { ... }` bloky lze deklarovat pouze v top-level, nongeneric `static class` (ne v interface / nested type).
- V jedné `static class` musí mít všechny extension members unikátní signatury (napříč všemi `extension` bloky).

**Static extension members:**

```csharp
public readonly record struct MyId(Guid Value);

public static class MyIdExtensions
{
    extension(MyId)
    {
        // Static extension - volá se na typu, ne na instanci
        public static MyId New() => new(Guid.NewGuid());

        public static bool TryParse(string s, out MyId id)
        {
            if (Guid.TryParse(s, out var guid))
            {
                id = new MyId(guid);
                return true;
            }

            id = default;
            return false;
        }
    }
}

// Použití
var id = MyId.New();
```

**Extension members pro interfaces (správně přes `static class`):**

```csharp
public enum LogLevel { Info, Warning, Error }

public interface ILogger
{
    void Log(LogLevel level, string message);
}

public static class LoggerExtensions
{
    extension(ILogger logger)
    {
        public void LogInfo(string msg) => logger.Log(LogLevel.Info, msg);
        public void LogWarning(string msg) => logger.Log(LogLevel.Warning, msg);
        public void LogError(string msg) => logger.Log(LogLevel.Error, msg);
    }
}

// Použití - každá implementace automaticky získá tyto metody
public sealed class ConsoleLogger : ILogger
{
    public void Log(LogLevel level, string message)
        => Console.WriteLine($"[{level}] {message}");
}

var logger = new ConsoleLogger();
logger.LogInfo("Application started");  // Funguje automaticky!
logger.LogError("Something went wrong");
```

---

## 3. Null-conditional assignment

```csharp
// ========== STARÉ ==========
if (customer != null)
{
    customer.LastVisit = DateTime.Now;
}

if (order?.Customer != null)
{
    order.Customer.LastOrder = order;
}

// ========== NOVÉ (C# 14) ==========
customer?.LastVisit = DateTime.Now;
order?.Customer?.LastOrder = order;

// Compound assignment
customer?.Points += 100;
order?.Total *= 1.1m;  // 10% přirážka

// S indexery
customers?[0]?.Name = "Updated";
dictionary?["key"] = newValue;
```

---

## 4. Unbound generic types v nameof

```csharp
// ========== STARÉ ==========
var name1 = nameof(List<object>);     // "List" - muselo se uvést nějaký typ
var name2 = nameof(Dictionary<int, string>); // "Dictionary"

// ========== NOVÉ (C# 14) ==========
var name1 = nameof(List<>);           // "List"
var name2 = nameof(Dictionary<,>);    // "Dictionary"

// Užitečné pro reflection, logging, atributy
[Obsolete($"Use {nameof(List<>)} instead")]
public class OldCollection { }
```

---

## 5. Partial constructors a events

```csharp
// ========== Partial constructor ==========
public partial class Customer
{
    // Deklarace v jedné části
    public partial Customer(string name, string email);
}

public partial class Customer
{
    // Implementace v jiné části
    public partial Customer(string name, string email)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAt = DateTime.UtcNow;
    }
}

// ========== Partial event ==========
public partial class OrderService
{
    public partial event EventHandler<OrderEventArgs> OrderCreated;
}

public partial class OrderService
{
    public partial event EventHandler<OrderEventArgs> OrderCreated
    {
        add => _orderCreated += value;
        remove => _orderCreated -= value;
    }
    private EventHandler<OrderEventArgs> _orderCreated;
}
```

---

## 6. First-class Span support

```csharp
// Implicitní konverze z pole na Span
void ProcessData(ReadOnlySpan<int> data) { }

int[] array = [1, 2, 3, 4, 5];
ProcessData(array);  // Implicitní konverze

// Implicitní konverze ze stringu na ReadOnlySpan<char>
void ProcessText(ReadOnlySpan<char> text) { }

string str = "Hello";
ProcessText(str);  // Implicitní konverze

// V collection expressions
Span<int> span = [1, 2, 3];
ReadOnlySpan<char> chars = ['a', 'b', 'c'];
```

---

## 7. User-defined compound assignment operators

```csharp
public readonly struct BigNumber
{
    private readonly long _value;

    public BigNumber(long value) => _value = value;

    // Standardní operátor
    public static BigNumber operator +(BigNumber a, BigNumber b)
        => new(a._value + b._value);

    // NOVÉ: Compound assignment - může modifikovat in-place
    public static BigNumber operator +=(ref BigNumber a, BigNumber b)
    {
        a = new BigNumber(a._value + b._value);
        return a;
    }
}

// Použití
BigNumber num = new(100);
num += new BigNumber(50);  // Efektivnější, bez alokace nového objektu
```

---

## 8. Simple lambda parametry s modifikátory

V C# 14 lze u lambda parametrů bez explicitního typu použít modifikátory jako `scoped`, `ref`, `in`, `out`, `ref readonly`:

```csharp
delegate bool TryParse<T>(string text, out T result);

// ========== NOVÉ (C# 14) ==========
TryParse<int> parse1 = (text, out result) => int.TryParse(text, out result);

// ========== STARÉ (C# 13 a dříve) ==========
TryParse<int> parse2 = (string text, out int result) => int.TryParse(text, out result);
```

Poznámka: modifikátor `params` stále vyžaduje explicitně typovaný seznam parametrů.
