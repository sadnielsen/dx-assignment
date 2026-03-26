# Oprdracht

## Status

Deze repository is gearchiveerd.
De assignment is gepresenteerd en zeer positief beoordeeld.

## 1  Context & Doelstelling

Bij The Company werken we aan een applicatie waarmee data migraties georchestreerd worden. In ons systeem moeten we verschillende soorten databronnen ondersteunen: SQL databases, REST API's, bestanden, en meer. Elke databron heeft gemeenschappelijke functionaliteit maar ook specifieke gedragingen.

Deze opdracht toetst jouw begrip van **overerving**, **polymorfisme**, **async programming**, **error handling**, en **testbaarheid** in een scenario dat dicht bij onze dagelijkse praktijk ligt.

---

## 2  Basis Types

### 2.1  Result Types

Implementeer een result type hiërarchie gebaseerd op het **Result pattern**. Gebruik sealed records om een gesloten type hiërarchie af te dwingen.

```csharp
public abstract record DataSourceResult(bool IsSuccess, string? ErrorMessage = null);

public sealed record SuccessResult(object Data) : DataSourceResult(true);
public sealed record ErrorResult(string ErrorMessage) : DataSourceResult(false, ErrorMessage);
public sealed record RetryableError(string ErrorMessage, TimeSpan RetryAfter)
    : DataSourceResult(false, ErrorMessage);
```

### 2.2  Custom Exceptions

Definieer een exception hiërarchie voor databron-specifieke fouten. Gebruik deze in je implementaties in plaats van generieke exceptions.

```csharp
public class DataSourceException : Exception
{
    public string DataSourceName { get; }
    public DataSourceException(string dataSourceName, string message, Exception? inner = null)
        : base(message, inner) => DataSourceName = dataSourceName;
}

public class ConnectionFailedException : DataSourceException { /* ... */ }
public class QueryExecutionException : DataSourceException { /* ... */ }
public class InvalidQueryException : DataSourceException { /* ... */ }
```

### 2.3  Interfaces

Implementeer het volgende interface. Let op: de interface erft van `IAsyncDisposable` — je implementaties moeten resources correct opruimen.

```csharp
public interface IDataSource : IAsyncDisposable
{
    string Name { get; }
    DataSourceState State { get; }
    Task<DataSourceResult> ConnectAsync(CancellationToken ct = default);
    Task<DataSourceResult> GetDataAsync(string query, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}

public enum DataSourceState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
```

### 2.4  Abstract Base Class

Implementeer een generieke abstract base class met een configuratie type parameter. De base class moet gemeenschappelijke logica bevatten: state management, automatische (her)verbinding, en template method patterns.

```csharp
public abstract class DataSourceBase<TConfig> : IDataSource
    where TConfig : class
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    protected DataSourceBase(string name, TConfig configuration)
    {
        Name = name;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; }
    public DateTime CreatedAt { get; }
    public DataSourceState State { get; protected set; }
    protected TConfig Configuration { get; }

    // Template method: subclasses implementeren deze
    protected abstract Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct);
    protected abstract Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct);
    protected abstract Task DisconnectCoreAsync(CancellationToken ct);

    public async Task<DataSourceResult> ConnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (State == DataSourceState.Connected)
                return new SuccessResult($"Already connected to {Name}");

            State = DataSourceState.Connecting;
            var result = await ConnectCoreAsync(ct);
            State = result.IsSuccess ? DataSourceState.Connected : DataSourceState.Error;
            return result;
        }
        catch (Exception ex)
        {
            State = DataSourceState.Error;
            throw new ConnectionFailedException(Name, $"Connection failed: {ex.Message}", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    // TODO: Implementeer GetDataAsync
    //   - Gebruik _lock voor thread-safety
    //   - Auto-connect als State != Connected
    //   - Valideer dat query niet null/empty is
    //   - Delegeer naar GetDataCoreAsync
    //   - Wrap exceptions in QueryExecutionException

    // TODO: Implementeer DisconnectAsync
    //   - Gebruik _lock voor thread-safety
    //   - Alleen disconnecten als State == Connected
    //   - Delegeer naar DisconnectCoreAsync
    //   - Zet State naar Disconnected

    // TODO: Implementeer DisposeAsync
    //   - Disconnect als verbonden
    //   - Dispose _lock
}
```

---

## 3  Concrete Implementaties

Implementeer de onderstaande drie databron types. Elk type heeft een eigen configuratie record en specifiek gedrag.

### 3.1  SqlDataSource

```csharp
public record SqlConfiguration(string ConnectionString, int CommandTimeoutSeconds = 30);

public class SqlDataSource : DataSourceBase<SqlConfiguration>
{
    public SqlDataSource(string name, SqlConfiguration config) : base(name, config) { }

    // TODO: Implementeer ConnectCoreAsync
    //   - Simuleer verbinding (Task.Delay)
    //   - Valideer ConnectionString is niet leeg
    //   - Return SuccessResult met connectie-info

    // TODO: Implementeer GetDataCoreAsync
    //   - Valideer query: moet beginnen met SELECT, INSERT, UPDATE, DELETE of EXEC
    //   - Gooi InvalidQueryException voor ongeldige queries
    //   - Return mock data als SuccessResult
    //   - Simuleer timeout als query "slow" bevat (return RetryableError)

    // TODO: Implementeer DisconnectCoreAsync
    //   - Log SQL-specifieke cleanup info
}
```

### 3.2  ApiDataSource

```csharp
public record ApiConfiguration(string BaseUrl, Dictionary<string, string>? DefaultHeaders = null);

public class ApiDataSource : DataSourceBase<ApiConfiguration>
{
    public ApiDataSource(string name, ApiConfiguration config) : base(name, config) { }

    public void AddHeader(string key, string value) { /* ... */ }

    // TODO: Implementeer ConnectCoreAsync
    //   - Valideer BaseUrl formaat (moet geldig URI zijn)
    //   - Simuleer health check
    //   - Return resultaat

    // TODO: Implementeer GetDataCoreAsync
    //   - Behandel query als endpoint path (bv. "/users", "/orders/123")
    //   - Valideer dat path begint met "/"
    //   - Return mock JSON data als string in SuccessResult
    //   - Simuleer 404 als path "/notfound" bevat (return ErrorResult)
    //   - Simuleer rate limit als path "/ratelimit" bevat (return RetryableError)
}
```

### 3.3  FileDataSource

```csharp
public record FileConfiguration(string FilePath, bool ReadOnly = true);

public class FileDataSource : DataSourceBase<FileConfiguration>
{
    public FileDataSource(string name, FileConfiguration config) : base(name, config) { }

    // TODO: Implementeer ConnectCoreAsync
    //   - Controleer of FilePath een geldig pad is
    //   - Simuleer bestandstoegang check
    //   - Return resultaat

    // TODO: Implementeer GetDataCoreAsync
    //   - Behandel query als sectie/filter (bv. "header", "line:5", "search:keyword")
    //   - Parseer het query formaat en return mock data
    //   - Return ErrorResult voor onbekend query formaat

    // TODO: Implementeer DisconnectCoreAsync
    //   - Log bestand-specifieke cleanup
    //   - Simuleer file handle release
}
```

---

## 4  DataSourceManager

Creëer een manager klasse die polymorfisme demonstreert. De manager beheert een collectie van databronnen en kan operaties op alle bronnen tegelijk uitvoeren. De manager moet zelf ook `IAsyncDisposable` implementeren.

```csharp
public class DataSourceManager : IAsyncDisposable
{
    private readonly List<IDataSource> _dataSources = [];

    public void Register(IDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        if (_dataSources.Any(ds => ds.Name == dataSource.Name))
            throw new InvalidOperationException($"Data source '{dataSource.Name}' already registered");
        _dataSources.Add(dataSource);
    }

    // TODO: Implementeer ExecuteOnAllAsync
    //   - Voer query parallel uit op alle bronnen (Task.WhenAll)
    //   - Gebruik CancellationToken
    //   - Return Dictionary<string, DataSourceResult> (name -> result)
    //   - Vang per-bron exceptions op en map naar ErrorResult

    // TODO: Implementeer GetByType<T>() where T : IDataSource
    //   - Return IReadOnlyList<T> van bronnen van type T
    //   - Gebruik OfType<T>()

    // TODO: Implementeer GenerateReportAsync
    //   - Genereer een rapport string met info over alle bronnen
    //   - Gebruik pattern matching (switch expression) op het concrete type
    //   - Toon type-specifieke informatie per bron
    //   - Gebruik StringBuilder voor performance

    // TODO: Implementeer DisposeAsync
    //   - Dispose alle geregistreerde bronnen
    //   - Vang en log individuele disposal fouten
    //   - Clear de lijst
}
```

---

## 5  Retry Decorator (Bonus Architectuur)

Implementeer een decorator die retry-logica toevoegt aan elke `IDataSource`. Dit demonstreert het **Decorator pattern** in combinatie met polymorfisme.

```csharp
public class RetryDataSourceDecorator : IDataSource
{
    private readonly IDataSource _inner;
    private readonly int _maxRetries;

    public RetryDataSourceDecorator(IDataSource inner, int maxRetries = 3)
    {
        _inner = inner;
        _maxRetries = maxRetries;
    }

    // TODO: Implementeer alle IDataSource members
    //   - Delegeer naar _inner
    //   - Bij RetryableError: wacht RetryAfter, dan retry
    //   - Bij ErrorResult: geef direct terug (niet retryable)
    //   - Bij SuccessResult: geef direct terug
    //   - Na _maxRetries: return laatste error
    //   - Log elke retry poging naar Console
}
```

---

## 6  Unit Tests

Schrijf unit tests met een test framework en mock framework naar keuze. Wij gebruiken intern **TUnit** en **NSubstitute**, maar je mag xUnit/NUnit/MSTest en Moq/NSubstitute gebruiken.

### 6.1  Minimale Test Coverage

Schrijf minimaal tests voor de volgende scenario's:

- **ConnectAsync** — succesvol verbinden zet State naar Connected
- **ConnectAsync** — dubbele connect geeft SuccessResult zonder opnieuw te verbinden
- **GetDataAsync** — auto-connect bij niet-verbonden state
- **GetDataAsync** — ongeldige query geeft ErrorResult of gooit InvalidQueryException
- **GetDataAsync** — null/lege query wordt geweigerd
- **DisconnectAsync** — zet State naar Disconnected
- **DisposeAsync** — roept DisconnectAsync aan als verbonden
- **DataSourceManager.ExecuteOnAllAsync** — voert query uit op alle bronnen
- **DataSourceManager.ExecuteOnAllAsync** — één falende bron blokkeert anderen niet
- **RetryDataSourceDecorator** — retry bij RetryableError
- **RetryDataSourceDecorator** — stopt na max retries
- **Thread safety** — gelijktijdige ConnectAsync calls resulteren in één verbinding

> *Tip: gebruik CancellationTokenSource met timeout in je tests om hangende operaties te voorkomen.*

---

## 7  Console Applicatie

Creëer een interactieve console applicatie die het polymorfisme in actie toont. De applicatie moet `await using` gebruiken voor correcte resource cleanup.

### 7.1  Menu Structuur

```
╔════════════════════════════════════════╗
║    DXMS Data Source Demo               ║
╠════════════════════════════════════════╣
║  1. SQL Data Source toevoegen          ║
║  2. API Data Source toevoegen          ║
║  3. File Data Source toevoegen         ║
║  4. Verbinding testen                  ║
║  5. Query uitvoeren op alle bronnen    ║
║  6. Query met retry uitvoeren          ║
║  7. Rapport tonen                      ║
║  8. Afsluiten                          ║
╚════════════════════════════════════════╝
```

### 7.2  Voorbeeld Interactie

```
Selecteer optie: 1
Voer naam in: Production DB
Voer connection string in: Server=localhost;Database=DXMS
✓ SQL Data Source 'Production DB' toegevoegd

Selecteer optie: 5
Voer query in: SELECT * FROM Users
━━━ Resultaten ━━━━━━━━━━━━━━━━━━━━━━━━
[SQL]  Production DB    ✓ Succes    (23ms)
[API]  User Service     ✓ Succes    (142ms)
[File] Config.json      ✗ Error     Ongeldig query formaat
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Totaal: 2 succes, 1 fout
```

---

*Succes! Mocht je vragen hebben over de opdracht, neem dan contact op met je contactpersoon.*
