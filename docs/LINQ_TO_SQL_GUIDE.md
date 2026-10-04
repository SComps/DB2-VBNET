# LINQ to Db2 / SQL Developer Guide (`Db2Spufi.Core.Linq`)

This guide explains how to use **`Db2Spufi.Core`** as a lightweight LINQ to SQL data access library for IBM Db2 (z/OS and LUW) in both **VB.NET** and **C#**.

`Db2Spufi.Core.Linq` provides strongly-typed object materialization, `IEnumerable<T>` / `IEnumerable(Of T)`, `IQueryable<T>` / `IQueryable(Of T)`, and `Db2DataContext` query execution over IBM Db2 without requiring heavy external O/RM frameworks.

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
