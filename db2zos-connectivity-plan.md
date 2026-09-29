# DB2 z/OS Connectivity Test — VB.NET Console App Plan

## Top-Level Overview

Create a minimal VB.NET console application targeting **.NET 10** that opens a connection to a **Db2 for z/OS** database server and reports success or failure to the console. The app uses IBM's official `Net.IBM.Data.Db2` NuGet package (Windows x64 variant), which implements the ADO.NET `DB2Connection` API and ships with the Db2 Connect runtime required for z/OS DRDA connections.

**Connection details (hardcoded):**

| Parameter     | Value         |
|---------------|---------------|
| Host          | 10.10.13.2    |
| Port          | 8103          |
| Location name | DBD1LOC       |
| User ID       | SCOTT         |
| Password      | mlkhbu        |

**Scope:**
- Scaffold a new VB.NET console project
- Add the correct IBM Db2 NuGet package for Windows
- Write minimal connection-open / connection-close code with console output
- Verify the build succeeds

**Non-goals:**
- No query execution
- No configuration file / appsettings
- No error-handling beyond what is needed to print a useful message on failure

---

## Sub-Tasks

---

### Sub-Task 1 — Verify .NET 10 SDK and Db2 Connect license

**Intent**
Confirm that the .NET 10 SDK is installed and accessible, and determine which Db2 Connect licensing mechanism is in effect so the app can connect at runtime without a license error.

**Expected Outcomes**
- `dotnet --version` returns a `10.x.x` version string.
- One of the following is confirmed true:
  - The z/OS ADCD has a server-side Db2 Connect Unlimited Edition license activated (no client file needed), **or**
  - A `db2consv_zs.lic` (or equivalent) file is located on the Windows machine and its path is documented for use in Sub-Task 3.

**Todo List**
1. Run `dotnet --version` and confirm the output starts with `10.`.
2. Run `dotnet --list-sdks` to see all installed SDKs.
3. Search for any existing Db2 Connect license files on the machine:
   - Check `C:\Program Files\IBM\` recursively for any `*.lic` files.
   - Check the IBM Db2 for z/OS VS Code extension folder (typically under `%USERPROFILE%\.vscode\extensions\ibm*`) for a `clidriver\license\` subdirectory containing a `.lic` file.
4. Document the result:
   - If a `.lic` file is found, note its path — it will need to be copied into the NuGet package's `clidriver\license\` output folder (Sub-Task 3).
   - If no `.lic` file is found, the z/OS server-side license is assumed active (consistent with ADCD environments); no further action is needed.

**Relevant Context**
- ADCD environments commonly use the server-activated Db2 Connect Unlimited Edition for System z license (`db2connectactivate` run on z/OS), which removes any client-side license requirement.
- The VS Code extension connecting successfully is evidence the license situation is already resolved — this step confirms *how*.
- For the `Net.IBM.Data.Db2` NuGet package, the client-side license file (if needed) must reside in `clidriver\license\` within the application's build output directory.

**Status:** `[x] done`

---

### Sub-Task 2 — Scaffold the VB.NET console project

**Intent**  
Create the project structure using the `dotnet new` CLI so the project targets .NET 10 and uses VB.NET.

**Expected Outcomes**
- A folder `Db2ConnTest/` exists containing `Db2ConnTest.vbproj` and `Program.vb`.
- The project file targets `net10.0` and specifies `<RootNamespace>Db2ConnTest</RootNamespace>`.
- `dotnet build` succeeds on the bare project before any library is added.

**Todo List**
1. Run `dotnet new console -lang VB -n Db2ConnTest -f net10.0` in the workspace root.
2. Confirm the generated `Db2ConnTest.vbproj` targets `net10.0`.
3. Run `dotnet build Db2ConnTest/Db2ConnTest.vbproj` to confirm a clean baseline build.

**Relevant Context**
- Template name for VB console: `console` with `-lang VB`.
- Framework moniker for .NET 10: `net10.0`.

**Status:** `[x] done`

---

### Sub-Task 3 — Add the IBM Db2 NuGet package

**Intent**  
Add the `Net.IBM.Data.Db2` NuGet package (Windows x64) to the project. This package bundles the Db2 Connect runtime driver and the `IBM.Data.Db2` ADO.NET provider assembly — no separate client installation is needed.

**Expected Outcomes**
- `Db2ConnTest.vbproj` contains a `<PackageReference>` for `Net.IBM.Data.Db2`.
- `dotnet restore` completes without errors.
- `dotnet build` still succeeds after the package is added.

**Todo List**
1. Run `dotnet add Db2ConnTest/Db2ConnTest.vbproj package Net.IBM.Data.Db2` to add the latest stable version.
2. Open `Db2ConnTest.vbproj` and confirm the `<PackageReference>` entry is present.
3. Run `dotnet build Db2ConnTest/Db2ConnTest.vbproj` to verify no build errors.

**Relevant Context**
- IBM NuGet publisher profile: `IBMDB2EF` on nuget.org.
- Package `Net.IBM.Data.Db2` targets Windows x64; this is correct for the development machine.
- Namespace is `IBM.Data.Db2`; the main connection class is `IBM.Data.Db2.DB2Connection`.
- A Db2 Connect license is required for z/OS connections — the user has confirmed this is available.

**Status:** `[x] done`

---

### Sub-Task 4 — Write the connectivity test code

**Intent**  
Replace the scaffolded `Program.vb` with code that builds a connection string from the known parameters, attempts to open a `DB2Connection`, prints a success message, then closes the connection. Any exception is caught and printed so the failure reason is visible.

**Expected Outcomes**
- `Program.vb` contains a single `Main` sub that:
  1. Builds the connection string: `Server=10.10.13.2:8103;Database=DBD1LOC;UID=SCOTT;PWD=mlkhbu;`
  2. Creates a `DB2Connection` with that string.
  3. Calls `conn.Open()`.
  4. Prints `"Connection successful. Server version: <version>"` using `conn.ServerVersion`.
  5. Calls `conn.Close()`.
  6. Catches any `Exception` and prints `"Connection failed: <message>"`.
- `dotnet build` succeeds.

**Todo List**
1. Open `Db2ConnTest/Program.vb`.
2. Replace its contents with the connectivity test implementation described above.
3. Run `dotnet build Db2ConnTest/Db2ConnTest.vbproj` and confirm zero errors.

**Relevant Context**
- IBM documentation connection string format: `Server=<host>:<port>;Database=<location>;UID=<user>;PWD=<password>;`
- `DB2Connection` class lives in namespace `IBM.Data.Db2`.
- `conn.ServerVersion` is a string property available after a successful `Open()`.
- `DB2Exception` is the typed exception class but catching the base `Exception` is sufficient for a connectivity test.

**Status:** `[x] done`

---

### Sub-Task 5 — Final build validation

**Intent**  
Perform a final clean build to confirm the complete project compiles without warnings or errors before handing off for runtime testing.

**Expected Outcomes**
- `dotnet build Db2ConnTest/Db2ConnTest.vbproj -c Release` exits with code 0.
- No warnings or errors in build output.

**Todo List**
1. Run `dotnet build Db2ConnTest/Db2ConnTest.vbproj -c Release`.
2. Confirm exit code 0 and no unexpected warnings.

**Relevant Context**
- Runtime execution (`dotnet run`) will require network access to `10.10.13.2:8103` and a valid Db2 Connect license file in the environment.

**Status:** `[x] done`
