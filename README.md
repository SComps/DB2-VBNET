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

### 🔑 Database Connection & Credentials Setup

To establish **where** to connect and **what credentials** to use, configure a `ConnectionProfile` or standard Db2 ADO.NET connection string.

#### Key Connection Parameters:
- **Server**: Hostname or IP address (e.g. `10.10.13.2`)
- **Port**: DRDA listener port (e.g. `8103` for Db2 z/OS, `50000` for LUW)
- **Database**: Db2 Location / Database Name (e.g. `DBD1LOC`)
- **User / Password**: RACF ID / Db2 user ID (`UID`) and password (`PWD`)
- **CurrentSQLID**: *(Optional z/OS)* Primary schema owner (e.g. `SCOTT`)

Saved connection profiles are persisted automatically to `%APPDATA%\Db2Spufi\profiles.json`.

### Quick Example (C#)

```csharp
using Db2Spufi.Core.Models;
using Db2Spufi.Core.Linq;

var profile = new ConnectionProfile
{
    Name = "Db2 z/OS Production",
    Server = "10.10.13.2",
    Port = 8103,
    Database = "DBD1LOC",
    User = "SCOTT",
    Password = "your_password",
    CurrentSqlId = "SCOTT"
};

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

Dim profile As New ConnectionProfile With {
    .Name = "Db2 z/OS Production",
    .Server = "10.10.13.2",
    .Port = 8103,
    .Database = "DBD1LOC",
    .User = "SCOTT",
    .Password = "your_password",
    .CurrentSqlId = "SCOTT"
}

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

## 🤖 Acknowledgements & AI Assistance

AI assistance was utilized in the design, development, refactoring, and documentation of this project.

---

## 📄 License
MIT License. See [LICENSE](LICENSE) for details.
