# Price3D WinForms End-User Application — Plan

## Top-Level Overview

Create a new **VB.NET WinForms** project (`Price3DApp`) targeting **Windows / .NET 10** that provides a polished, end-user-facing GUI for the PRICE3D database on the z/OS mainframe at `10.10.13.2:8103`. This is a **Windows-only** project by design — WinForms does not run on Linux or macOS.

The application reuses the existing `Db` repository layer (`CustomerRepo`, `FilamentRepo`, `ProjectRepo`, `Database`) from `Price3DManager` by referencing it as a shared project, avoiding code duplication. The UI presents a main window with a tabbed or sidebar navigation structure, a DataGridView-based browse experience for each table, and modal dialogs for add/edit operations.

**Scope:**
- New WinForms project `Price3DApp` added to the solution
- Shared `Db` code extracted to a class library (`Price3DData`) referenced by both `Price3DManager` (TUI) and `Price3DApp` (WinForms)
- Main window with navigation to three table views
- Full CRUD (add, edit, delete) for `FILMNT`, `P3DCUST`, and `PROJECTS`
- Status bar showing connection state
- Publish script for Windows x64

**Non-goals:**
- No reporting, charting, or printing
- No authentication UI (credentials remain hardcoded for now)
- No Linux/macOS support

---

## Architecture

```
DB2-VBNET.slnx
├── Db2ConnTest/          (existing — connectivity test console app)
├── Price3DManager/       (existing — Terminal.Gui TUI app)
│   └── Db/ ──────────── (to be moved → Price3DData)
├── Price3DData/          (NEW — class library, shared DB layer)
│   └── Db/
│       ├── Database.vb
│       ├── CustomerRepo.vb
│       ├── FilamentRepo.vb
│       └── ProjectRepo.vb
└── Price3DApp/           (NEW — WinForms end-user app, Windows only)
    ├── Forms/
    │   ├── MainForm.vb
    │   ├── CustomerForm.vb
    │   ├── FilamentForm.vb
    │   └── ProjectForm.vb
    ├── Dialogs/
    │   ├── CustomerDialog.vb
    │   ├── FilamentDialog.vb
    │   └── ProjectDialog.vb
    └── Program.vb
```

---

## Sub-Tasks

---

### Sub-Task 1 — Extract shared Db layer into `Price3DData` class library

**Intent**
Move the four `Db/` files currently in `Price3DManager` into a new shared class library so both `Price3DManager` (TUI) and `Price3DApp` (WinForms) reference the same repository code without duplication.

**Expected Outcomes**
- New project `Price3DData/Price3DData.vbproj` exists, targeting `net10.0`, OutputType `Library`
- Contains `Db/Database.vb`, `Db/CustomerRepo.vb`, `Db/FilamentRepo.vb`, `Db/ProjectRepo.vb`
- `Price3DData.vbproj` has platform-conditional IBM NuGet references (same as current projects)
- `Price3DManager.vbproj` removes its own `Db/` files and adds a `<ProjectReference>` to `Price3DData`
- `Db2ConnTest.vbproj` is unaffected
- Both projects build successfully after the refactor
- `DB2-VBNET.slnx` updated to include `Price3DData`

**Todo List**
1. Create `Price3DData/Price3DData.vbproj` (SDK-style, `net10.0`, `Library` output, platform-conditional IBM NuGet refs)
2. Copy (not move yet) `Price3DManager/Db/*.vb` into `Price3DData/Db/`
3. Update namespace in copied files from `Price3DManager.Db` to `Price3DData.Db`
4. Add `<ProjectReference>` to `Price3DData` in `Price3DManager.vbproj`
5. Update `Imports` in all `Price3DManager` source files from `Price3DManager.Db` to `Price3DData.Db`
6. Remove `Price3DManager/Db/*.vb` files (now provided by the library)
7. Add `Price3DData` to `DB2-VBNET.slnx`
8. Build both projects; confirm zero errors

**Relevant Context**
- Current `Db/` files: `Database.vb`, `CustomerRepo.vb`, `FilamentRepo.vb`, `ProjectRepo.vb`
- Current namespace: `Price3DManager.Db` (used in all `Views/*.vb` via `Imports Price3DManager.Db`)
- IBM NuGet package must be present in `Price3DData` since the library compiles against `IBM.Data.Db2`

**Status:** `[ ] pending`

---

### Sub-Task 2 — Scaffold the `Price3DApp` WinForms project

**Intent**
Create the new Windows-only WinForms project and wire it into the solution, referencing `Price3DData` for data access.

**Expected Outcomes**
- `Price3DApp/Price3DApp.vbproj` exists, targeting `net10.0-windows`, `WinExe` output
- References `Price3DData` via `<ProjectReference>`
- `DB2-VBNET.slnx` updated to include `Price3DApp`
- `dotnet build Price3DApp/Price3DApp.vbproj` succeeds on a baseline scaffold

**Todo List**
1. Run `dotnet new winforms -lang VB -n Price3DApp -f net10.0-windows`
2. Edit `Price3DApp.vbproj`: add `<ProjectReference>` to `Price3DData`
3. Add `Price3DApp` to `DB2-VBNET.slnx`
4. Add `scripts/Publish-Price3DApp-Win-x64.ps1` publish script (self-contained, `-p:PublishSingleFile=false`)
5. Run baseline build; confirm zero errors

**Relevant Context**
- WinForms requires TFM `net10.0-windows` (not `net10.0`) for `System.Windows.Forms` access
- `PublishSingleFile=false` required because `IBM.Data.Db2` uses `Assembly.CodeBase`
- No Linux build script needed — WinForms is Windows-only

**Status:** `[ ] pending`

---

### Sub-Task 3 — Build `MainForm` with navigation and status bar

**Intent**
Create the application's main window with a menu bar for navigating between tables, and a status bar showing the database connection state (server, version).

**Expected Outcomes**
- `MainForm.vb` has a `MenuStrip` with `File` (Exit) and `Tables` (Filament Inventory, Customers, Projects) menus
- A `StatusStrip` at the bottom shows `Connected to: 10.10.13.2:8103 (DBD1LOC)  |  Server: 13.01.0002` after startup
- The MDI or panel area is ready to host the three table forms
- Connection test runs on startup; status bar reflects success or error

**Todo List**
1. Open `MainForm.vb` (auto-generated by scaffold)
2. Add `MenuStrip` with `File > Exit` and `Tables > Filament Inventory / Customers / Projects`
3. Add `StatusStrip` with two `ToolStripStatusLabel` items (connection info, server version)
4. On `Form.Load`, call `Database.OpenConnection()`, populate status labels, handle errors gracefully
5. Wire menu items to open the respective child forms (Sub-Tasks 4–6)
6. Build and confirm zero errors

**Relevant Context**
- `Database.OpenConnection()` is in `Price3DData.Db.Database`
- `conn.ServerVersion` returns the string shown in prior test runs (`13.01.0002`)
- WinForms pattern: open child forms inside a `Panel` that fills the client area (single-document style) or use MDI

**Status:** `[ ] pending`

---

### Sub-Task 4 — Build Filament Inventory form (`FilamentForm` + `FilamentDialog`)

**Intent**
Provide a DataGridView-based browse of `SCOTT.FILMNT` with Add, Edit (double-click or button), and Delete actions, backed by a modal dialog for data entry.

**Expected Outcomes**
- `FilamentForm.vb` fills its host area with a `DataGridView` showing all `FILMNT` columns
- Toolbar or buttons: `Add`, `Edit`, `Delete`, `Refresh`
- Double-clicking a row or pressing Enter opens `FilamentDialog` in edit mode
- `FilamentDialog.vb` has labelled `TextBox` controls for all 11 fields; `Item ID` is read-only in edit mode
- Save button calls `FilamentRepo.Upsert`; errors shown via `MessageBox`
- Delete prompts for confirmation before calling `FilamentRepo.Delete`
- Grid refreshes after any CUD operation

**Todo List**
1. Create `Forms/FilamentForm.vb` — `UserControl` or `Form` with `DataGridView`, `ToolStrip` buttons
2. On load, call `FilamentRepo.GetAll()` and bind to a `BindingSource` / `DataGridView`
3. Wire double-click and Edit button to open `FilamentDialog(item, isNew:=False)`
4. Wire Add button to open `FilamentDialog(New FilamentItem(), isNew:=True)`
5. Wire Delete button with confirmation `MessageBox` then `FilamentRepo.Delete`
6. Create `Dialogs/FilamentDialog.vb` — `Form` with 11 labelled `TextBox` controls, Save/Cancel buttons
7. Build; confirm zero errors

**Relevant Context**
- `FilamentItem` fields: `ItemId`, `ItType`, `ItColor`, `ItWeigh`, `ItUsed`, `ItRemain`, `ItPurch`, `ItVendor`, `ItVendorId`, `ItVReorder`, `ItLastDt`
- Numeric fields (`ItWeigh`, `ItUsed`, `ItRemain`, `ItPurch`) should be right-aligned in the grid
- `ItLastDt` is stored as a VARCHAR on z/OS ADCD; display as plain string

**Status:** `[ ] pending`

---

### Sub-Task 5 — Build Customers form (`CustomerForm` + `CustomerDialog`)

**Intent**
Provide a DataGridView browse of `SCOTT.P3DCUST` with full CRUD via a modal dialog.

**Expected Outcomes**
- `CustomerForm.vb` shows all 9 customer fields in a grid
- Toolbar buttons: `Add`, `Edit`, `Delete`, `Refresh`
- Double-click or Enter opens `CustomerDialog` in edit mode
- `CustomerDialog.vb` has 9 labelled `TextBox` controls; `Customer ID` is read-only in edit mode
- `Addr2`, `Phone`, `Email` are optional (nullable); saved as `DBNull.Value` when blank
- Grid refreshes after CUD

**Todo List**
1. Create `Forms/CustomerForm.vb` with `DataGridView` and `ToolStrip`
2. Bind `CustomerRepo.GetAll()` to grid via `BindingSource`
3. Wire Add, Edit (double-click), Delete with same patterns as `FilamentForm`
4. Create `Dialogs/CustomerDialog.vb` with 9 field controls, Save/Cancel
5. Build; confirm zero errors

**Relevant Context**
- `Customer` fields: `CustId`, `CName`, `Addr1`, `Addr2`, `City`, `State`, `Zip`, `Phone`, `Email`
- Nullable columns (`Addr2`, `Phone`, `Email`) already handled in `CustomerRepo.Upsert` via `DBNull.Value`

**Status:** `[ ] pending`

---

### Sub-Task 6 — Build Projects form (`ProjectForm` + `ProjectDialog`)

**Intent**
Provide a DataGridView browse of `SCOTT.PROJECTS` with full CRUD via a modal dialog.

**Expected Outcomes**
- `ProjectForm.vb` shows all 7 project fields in a grid
- Toolbar buttons: `Add`, `Edit`, `Delete`, `Refresh`
- Double-click or Enter opens `ProjectDialog` in edit mode
- `ProjectDialog.vb` has 7 labelled controls; `Project Name` is read-only in edit mode
- Numeric fields use validated input (parse on Save, show error if not numeric)
- Grid refreshes after CUD

**Todo List**
1. Create `Forms/ProjectForm.vb` with `DataGridView` and `ToolStrip`
2. Bind `ProjectRepo.GetAll()` to grid via `BindingSource`
3. Wire Add, Edit (double-click), Delete
4. Create `Dialogs/ProjectDialog.vb` with 7 field controls, Save/Cancel
5. Build; confirm zero errors

**Relevant Context**
- `Project` fields: `PName`, `PFile`, `PRuntime` (Integer), `PCostMin` (Decimal), `PMarkup` (Decimal), `PFil`, `PGrams` (Decimal)
- `PRuntime` is minutes (Integer); `PCostMin`, `PMarkup`, `PGrams` are Decimal

**Status:** `[ ] pending`

---

### Sub-Task 7 — Publish script and final validation

**Intent**
Add a Windows publish script and verify the complete solution builds cleanly.

**Expected Outcomes**
- `scripts/Publish-Price3DApp-Win-x64.ps1` exists and runs successfully
- Published output in `Price3DApp/bin/publish/win-x64/` contains `Price3DApp.exe` and `clidriver/`
- `dotnet build DB2-VBNET.slnx` builds all four projects (Db2ConnTest, Price3DData, Price3DManager, Price3DApp) with zero errors

**Todo List**
1. Create `scripts/Publish-Price3DApp-Win-x64.ps1` (same pattern as other publish scripts, `--runtime win-x64 --self-contained true -p:PublishSingleFile=false`)
2. Run `dotnet build DB2-VBNET.slnx -c Release` and confirm all four projects build
3. Run `powershell -ExecutionPolicy Bypass -File scripts/Publish-Price3DApp-Win-x64.ps1` and confirm success
4. Commit and push all new files

**Relevant Context**
- `PublishSingleFile=false` is mandatory (IBM driver incompatibility)
- WinForms publish produces an `.exe` that requires Windows; no Linux script needed

**Status:** `[ ] pending`
