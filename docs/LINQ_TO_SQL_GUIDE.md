# LINQ to Db2 / SQL Developer Guide (`Db2Spufi.Core.Linq`)

This guide explains how to import and use **`Db2Spufi.Core`** as a lightweight LINQ to SQL data access library for IBM Db2 (z/OS and LUW) in both **VB.NET** and **C#**.

`Db2Spufi.Core.Linq` provides strongly-typed object materialization, `IEnumerable<T>` / `IEnumerable(Of T)`, `IQueryable<T>` / `IQueryable(Of T)`, and `Db2DataContext` query execution over IBM Db2 without requiring heavy external O/RM frameworks.

---

## 0. Importing `Db2Spufi.Core` into External Applications

Before writing LINQ code, import `Db2Spufi.Core` into your external application using either **Option A (NuGet Package)** or **Option B (Manual DLL Assembly Reference)**.

### First, Generate Library Assets
Run the publish script for your operating system to build library assets without sample GUI applications:

- **Windows**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts\Publish-Db2Spufi-Library-Win-x64.ps1
  ```
- **Linux**:
  ```bash
  chmod +x scripts/publish-db2spufi-library-linux-x64.sh
  ./scripts/publish-db2spufi-library-linux-x64.sh
  ```

---

### Option A: Importing via Local NuGet Package (Recommended)

Publishing outputs `.nupkg` packages to `publish/library/nupkg/Db2Spufi.Core.1.0.0.nupkg`.

#### 1. Via .NET CLI
In your external project folder, add the package specifying the local source path:
```bash
dotnet add package Db2Spufi.Core --version 1.0.0 --source ./path/to/DB2-VBNET/publish/library/nupkg
```

#### 2. Via `nuget.config`
Add a local package source in a `nuget.config` file placed in your target solution root:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Db2SpufiLocal" value="./publish/library/nupkg" />
  </packageSources>
</configuration>
```
Then reference the package in your target `.csproj` or `.vbproj`:
```xml
<ItemGroup>
  <PackageReference Include="Db2Spufi.Core" Version="1.0.0" />
</ItemGroup>
```

---

### Option B: Importing Assemblies / DLLs Manually

If your project does not use NuGet feeds, you can reference the compiled assemblies directly from `publish/library/win-x64/` (Windows) or `publish/library/linux-x64/` (Linux).

#### 1. Project Reference Snippet (`.csproj` or `.vbproj`)

##### C# Project (`.csproj`)
```xml
<ItemGroup>
  <!-- Direct DLL Reference -->
  <Reference Include="Db2Spufi.Core">
    <HintPath>..\path\to\publish\library\win-x64\Db2Spufi.Core.dll</HintPath>
  </Reference>
  
  <!-- IBM Db2 ADO.NET Provider Dependency -->
  <PackageReference Include="Net.IBM.Data.Db2" Version="10.0.0.300" Condition="$([MSBuild]::IsOSPlatform('Windows'))" />
  <PackageReference Include="Net.IBM.Data.Db2-lnx" Version="10.0.0.300" Condition="$([MSBuild]::IsOSPlatform('Linux'))" />
</ItemGroup>
```

##### VB.NET Project (`.vbproj`)
```xml
<ItemGroup>
  <!-- Direct DLL Reference -->
  <Reference Include="Db2Spufi.Core">
    <HintPath>..\path\to\publish\library\win-x64\Db2Spufi.Core.dll</HintPath>
  </Reference>
  
  <!-- IBM Db2 ADO.NET Provider Dependency -->
  <PackageReference Include="Net.IBM.Data.Db2" Version="10.0.0.300" Condition="$([MSBuild]::IsOSPlatform('Windows'))" />
  <PackageReference Include="Net.IBM.Data.Db2-lnx" Version="10.0.0.300" Condition="$([MSBuild]::IsOSPlatform('Linux'))" />
</ItemGroup>
```

#### 2. Required Binaries in Output Folder

Ensure the following files from `publish/library/<platform>/` are present in your target application runtime folder:
- **`Db2Spufi.Core.dll`**: Core engine & LINQ to Db2 provider.
- **`Db2Spufi.Core.xml`**: Intellisense tooltips and API documentation.
- **`Net.IBM.Data.Db2.dll`** (Windows) or **`Net.IBM.Data.Db2-lnx.dll`** (Linux): IBM ADO.NET data provider.
- **`clidriver/`**: Subdirectory containing IBM DB2 native driver libraries required for DRDA protocol database communication.

---

## 1. Defining Entity Models

Entity classes match table or query column names to public writable properties (case-insensitive). Supported data types include `String`, `Integer` / `int`, `Long` / `long`, `Decimal` / `decimal`, `Double` / `double`, `DateTime`, `Guid`, `Boolean` / `bool`, `Enum`, and `Nullable(Of T)` / `Nullable<T>`.

### VB.NET
```vb
Public Class SysTableEntity
    Public Property Creator As String
    Public Property Name As String
    Public Property DbName As String
    Public Property ColumnCount As Integer
End Class
```

### C#
```csharp
public class SysTableEntity
{
    public string Creator { get; set; }
    public string Name { get; set; }
    public string DbName { get; set; }
    public int ColumnCount { get; set; }
}
```

---

## 2. Using `Db2DataContext` with `IQueryable`

`Db2DataContext` manages connections and query execution against IBM Db2. Use `AsQueryable(Of T)` / `AsQueryable<T>` to construct queries that support standard LINQ query operators (`Where`, `Select`, `OrderBy`, `Take`, `FirstOrDefault`, `ToList`).

### VB.NET Example
```vb
Imports Db2Spufi.Core.Models
Imports Db2Spufi.Core.Linq

Public Sub QueryDb2TablesVb(profile As ConnectionProfile)
    ' Create Db2DataContext from a ConnectionProfile
    Using ctx As Db2DataContext = profile.CreateDataContext()
        
        ' Create IQueryable for SysTableEntity
        Dim query As IQueryable(Of SysTableEntity) = ctx.AsQueryable(Of SysTableEntity)(
            "SELECT CREATOR, NAME, DBNAME, COLCOUNT AS ColumnCount FROM SYSIBM.SYSTABLES WHERE DBNAME = @p0",
            "PRICE3D"
        )

        ' Apply LINQ query expressions
        Dim results = query.Where(Function(t) t.Creator = "SYSIBM" OrElse t.Creator = "SCOTT") _
                           .OrderBy(Function(t) t.Name) _
                           .ToList()

        For Each t In results
            Console.WriteLine($"[VB.NET] Table: {t.Creator}.{t.Name} (Db: {t.DbName})")
        Next
    End Using
End Sub
```

### C# Example
```csharp
using System;
using System.Linq;
using Db2Spufi.Core.Models;
using Db2Spufi.Core.Linq;

public void QueryDb2TablesCSharp(ConnectionProfile profile)
{
    // Create Db2DataContext from a ConnectionProfile
    using (Db2DataContext ctx = profile.CreateDataContext())
    {
        // Create IQueryable for SysTableEntity
        IQueryable<SysTableEntity> query = ctx.AsQueryable<SysTableEntity>(
            "SELECT CREATOR, NAME, DBNAME, COLCOUNT AS ColumnCount FROM SYSIBM.SYSTABLES WHERE DBNAME = @p0",
            "PRICE3D"
        );

        // Apply LINQ query expressions
        var results = query.Where(t => t.Creator == "SYSIBM" || t.Creator == "SCOTT")
                           .OrderBy(t => t.Name)
                           .ToList();

        foreach (var t in results)
        {
            Console.WriteLine($"[C#] Table: {t.Creator}.{t.Name} (Db: {t.DbName})");
        }
    }
}
```

---

## 3. Parameterized & Async Queries

Parameterized queries prevent SQL injection by substituting `@p0`, `@p1`, etc. Async methods support non-blocking I/O with optional `CancellationToken` cancellation.

### VB.NET Example
```vb
Public Async Function GetTableCountAsyncVb(profile As ConnectionProfile, creator As String) As Task(Of Integer)
    Using ctx As Db2DataContext = profile.CreateDataContext()
        Dim sql = "SELECT COUNT(*) FROM SYSIBM.SYSTABLES WHERE CREATOR = @p0"
        Return Await ctx.ExecuteScalarAsync(Of Integer)(sql, CancellationToken.None, creator)
    End Using
End Function
```

### C# Example
```csharp
public async Task<int> GetTableCountAsyncCSharp(ConnectionProfile profile, string creator)
{
    using (Db2DataContext ctx = profile.CreateDataContext())
    {
        string sql = "SELECT COUNT(*) FROM SYSIBM.SYSTABLES WHERE CREATOR = @p0";
        return await ctx.ExecuteScalarAsync<int>(sql, CancellationToken.None, creator);
    }
}
```

---

## 4. `ConnectionProfile` & `DataTable` Extension Methods

`Db2Spufi.Core.Linq` provides extension methods directly on `ConnectionProfile`, `DataTable`, and `SpufiStatementResult`.

### Converting `DataTable` to LINQ Entities

#### VB.NET
```vb
Dim dt As DataTable = getResultTable()

' Convert DataTable to List(Of T)
Dim entities As List(Of SysTableEntity) = dt.ToEntities(Of SysTableEntity)()

' Convert DataTable directly to IQueryable(Of T)
Dim queryable = dt.AsQueryable(Of SysTableEntity)()
```

#### C#
```csharp
DataTable dt = getResultTable();

// Convert DataTable to List<T>
List<SysTableEntity> entities = dt.ToEntities<SysTableEntity>();

// Convert DataTable directly to IQueryable<T>
IQueryable<SysTableEntity> queryable = dt.AsQueryable<SysTableEntity>();
```

### Direct `ConnectionProfile` Query Extension

#### VB.NET
```vb
Dim profile As ConnectionProfile = profileManager.GetDefaultProfile()

Dim list As List(Of SysTableEntity) = profile.ExecuteQuery(Of SysTableEntity)(
    "SELECT CREATOR, NAME, DBNAME FROM SYSIBM.SYSTABLES WHERE CREATOR = @p0", 
    "SYSIBM"
)
```

#### C#
```csharp
ConnectionProfile profile = profileManager.GetDefaultProfile();

List<SysTableEntity> list = profile.ExecuteQuery<SysTableEntity>(
    "SELECT CREATOR, NAME, DBNAME FROM SYSIBM.SYSTABLES WHERE CREATOR = @p0", 
    "SYSIBM"
);
```

---

## 5. Summary API Reference (`Db2Spufi.Core.Linq`)

| Class / Module | Method | Description |
| :--- | :--- | :--- |
| `Db2DataContext` | `New(profile)` / `New(connectionString)` | Initializes context for Db2 query execution. |
| `Db2DataContext` | `Query<T>(sql, params)` | Executes SQL and returns strongly-typed `List<T>`. |
| `Db2DataContext` | `QueryAsync<T>(sql, cancelToken, params)` | Async query execution returning `Task<List<T>>`. |
| `Db2DataContext` | `AsQueryable<T>(sql, params)` | Creates `IQueryable<T>` for LINQ expressions. |
| `Db2DataContext` | `ExecuteNonQueryAsync(sql, cancelToken, params)` | Executes INSERT/UPDATE/DELETE statement. |
| `Db2DataContext` | `ExecuteScalarAsync<T>(sql, cancelToken, params)` | Executes scalar query returning single value `T`. |
| `DataTableExtensions` | `table.ToEntities<T>()` | Maps `DataTable` rows to `List<T>`. |
| `DataTableExtensions` | `table.AsQueryable<T>()` | Converts `DataTable` to `IQueryable<T>`. |
| `DataTableExtensions` | `stmtResult.AsQueryable<T>()` | Converts `SpufiStatementResult.DataTable` to `IQueryable<T>`. |
| `Db2LinqExtensions` | `profile.CreateDataContext()` | Extension method creating `Db2DataContext`. |
| `Db2LinqExtensions` | `profile.ExecuteQuery<T>(sql, params)` | One-liner query execution on `ConnectionProfile`. |
