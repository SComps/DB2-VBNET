# SPUFI for Db2 — SQL Processing Facility & .NET Core Data Access Library

A high-performance, cross-platform **SQL Processing Facility (SPUFI)** for IBM Db2 (z/OS and LUW) built on **.NET 10**, featuring **Avalonia UI**, **WinForms**, and a shared **`Db2Spufi.Core`** library supporting direct ADO.NET and **LINQ to Db2 (`IQueryable` / `IEnumerable`)**.

---

## 🌟 Solution Architecture

- **`Db2Spufi.Core`**: Shared .NET 10 core library containing IBM Db2 connection management, SPUFI script execution engine, CSV export, CLI package binding, and **`Db2Spufi.Core.Linq`** (`Db2DataContext`, `IQueryable<T>`, `IEnumerable<T>`).
- **`Db2Spufi.Avalonia`**: Cross-platform desktop application (Windows, Linux, macOS) built with Avalonia UI 11.
- **`Db2Spufi.WinForms`**: Classic Windows desktop GUI application.
- **`Db2Spufi.Tests`**: Automated verification test suite.

---

## 🚀 LINQ to Db2 / SQL Documentation

For detailed usage guidelines and code samples in **both VB.NET and C#**, see the [LINQ Developer Guide](docs/LINQ_TO_SQL_GUIDE.md).

### Quick Example (C#)

```csharp
using Db2Spufi.Core.Models;
using Db2Spufi.Core.Linq;

using (var ctx = profile.CreateDataContext())
{
    var query = ctx.AsQueryable<SysTableEntity>(
        "SELECT CREATOR, NAME, DBNAME FROM SYSIBM.SYSTABLES WHERE DBNAME = @p0", 
        "PRICE3D"
    );

    var tables = query.Where(t => t.Creator == "SYSIBM").ToList();
}
```

### Quick Example (VB.NET)

```vb
Imports Db2Spufi.Core.Models
Imports Db2Spufi.Core.Linq

Using ctx = profile.CreateDataContext()
    Dim query = ctx.AsQueryable(Of SysTableEntity)(
        "SELECT CREATOR, NAME, DBNAME FROM SYSIBM.SYSTABLES WHERE DBNAME = @p0", 
        "PRICE3D"
    )

    Dim tables = query.Where(Function(t) t.Creator = "SYSIBM").ToList()
End Using
```

---

## 🛠️ Building & Running

### Build Solution
```bash
dotnet build
```

### Run Avalonia Cross-Platform Desktop App
```bash
dotnet run --project Db2Spufi.Avalonia/Db2Spufi.Avalonia.csproj
```

### Run Verification Test Suite
```bash
dotnet test
```

---

## 📄 License
MIT License. See [LICENSE](LICENSE) for details.
