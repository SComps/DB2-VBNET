Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports Db2Spufi.Core.Config
Imports Db2Spufi.Core.Execution
Imports Db2Spufi.Core.Formatting
Imports Db2Spufi.Core.Models
Imports Db2Spufi.Core.Services

Namespace Forms

    Public Class MainForm
        Inherits Form

        Private ReadOnly _profileManager As ProfileManager
        Private _currentProfile As ConnectionProfile
        Private _options As SpufiOptions
        Private _currentFilePath As String = Nothing
        Private _cancellationTokenSource As CancellationTokenSource

        ' Menu controls
        Private menuStrip As MenuStrip
        Private toolStrip As ToolStrip
        Private statusStrip As StatusStrip

        ' ToolStrip items
        Private cboProfiles As ToolStripComboBox
        Private btnTestConn As ToolStripButton
        Private btnManageProfiles As ToolStripButton
        Private btnRunAll As ToolStripButton
        Private btnRunSelected As ToolStripButton
        Private btnCancelRun As ToolStripButton
        Private btnOptions As ToolStripButton
        Private btnOpenFile As ToolStripButton
        Private btnSaveFile As ToolStripButton

        ' Main layout
        Private splitContainer As SplitContainer
        Private txtSqlInput As RichTextBox
        Private lblEditorInfo As Label

        ' Results tabs
        Private tabResults As TabControl
        Private tabSpufiLog As TabPage
        Private tabGrids As TabPage
        Private tabMessages As TabPage

        Private txtSpufiOutput As RichTextBox
        Private tabDataGrids As TabControl
        Private txtMessages As TextBox

        ' Status bar items
        Private statusConnLabel As ToolStripStatusLabel
        Private statusExecutionLabel As ToolStripStatusLabel
        Private statusProgressBar As ToolStripProgressBar
        Private statusStatsLabel As ToolStripStatusLabel

        Public Sub New()
            _profileManager = New ProfileManager()
            _options = New SpufiOptions()

            InitializeComponents()
            LoadProfiles()
            UpdateEditorTitle()
        End Sub

        Private Sub InitializeComponents()
            Me.Text = "SPUFI for Db2 — SQL Processing Facility"
            Me.Size = New Size(1100, 750)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Font = New Font("Segoe UI", 9.0F)

            ' ── Menu Bar ────────────────────────────────────────────────────────────
            menuStrip = New MenuStrip()
            Dim mnuFile As New ToolStripMenuItem("&File")
            Dim mnuNew As New ToolStripMenuItem("&New Script", Nothing, AddressOf OnNewScript) With {.ShortcutKeys = Keys.Control Or Keys.N}
            Dim mnuOpen As New ToolStripMenuItem("&Open Script...", Nothing, AddressOf OnOpenScript) With {.ShortcutKeys = Keys.Control Or Keys.O}
            Dim mnuSave As New ToolStripMenuItem("&Save Script", Nothing, AddressOf OnSaveScript) With {.ShortcutKeys = Keys.Control Or Keys.S}
            Dim mnuSaveAs As New ToolStripMenuItem("Save Script &As...", Nothing, AddressOf OnSaveScriptAs)
            Dim mnuSaveLog As New ToolStripMenuItem("Save SPUFI &Output Log...", Nothing, AddressOf OnSaveSpufiLog)
            Dim mnuExportCsv As New ToolStripMenuItem("Export Active Grid to &CSV...", Nothing, AddressOf OnExportActiveGridCsv)
            Dim mnuExit As New ToolStripMenuItem("E&xit", Nothing, Sub() Me.Close()) With {.ShortcutKeys = Keys.Alt Or Keys.F4}
            mnuFile.DropDownItems.AddRange(New ToolStripItem() {mnuNew, mnuOpen, mnuSave, mnuSaveAs, New ToolStripSeparator(), mnuSaveLog, mnuExportCsv, New ToolStripSeparator(), mnuExit})

            Dim mnuQuery As New ToolStripMenuItem("&Query")
            Dim mnuExecAll As New ToolStripMenuItem("Execute &All", Nothing, AddressOf OnExecuteAll) With {.ShortcutKeys = Keys.F5}
            Dim mnuExecSel As New ToolStripMenuItem("Execute &Selected", Nothing, AddressOf OnExecuteSelected) With {.ShortcutKeys = Keys.Control Or Keys.F5}
            Dim mnuCancel As New ToolStripMenuItem("&Cancel Execution", Nothing, AddressOf OnCancelExecution)
            mnuQuery.DropDownItems.AddRange(New ToolStripItem() {mnuExecAll, mnuExecSel, mnuCancel})

            Dim mnuConn As New ToolStripMenuItem("&Connection")
            Dim mnuManageConn As New ToolStripMenuItem("&Manage Profiles...", Nothing, AddressOf OnManageProfiles)
            Dim mnuTestConnItem As New ToolStripMenuItem("&Test Connection", Nothing, AddressOf OnQuickTestConnection)
            mnuConn.DropDownItems.AddRange(New ToolStripItem() {mnuManageConn, mnuTestConnItem})

            Dim mnuOptionsItem As New ToolStripMenuItem("&Options")
            Dim mnuSpufiOpts As New ToolStripMenuItem("&SPUFI Execution Options...", Nothing, AddressOf OnShowOptions)
            mnuOptionsItem.DropDownItems.Add(mnuSpufiOpts)

            Dim mnuHelp As New ToolStripMenuItem("&Help")
            Dim mnuAbout As New ToolStripMenuItem("&About", Nothing, AddressOf OnAbout)
            mnuHelp.DropDownItems.Add(mnuAbout)

            menuStrip.Items.AddRange(New ToolStripItem() {mnuFile, mnuQuery, mnuConn, mnuOptionsItem, mnuHelp})

            ' ── ToolStrip ────────────────────────────────────────────────────────────
            toolStrip = New ToolStrip() With {.GripStyle = ToolStripGripStyle.Hidden}

            Dim lblProfile As New ToolStripLabel(" Profile: ")
            cboProfiles = New ToolStripComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
            AddHandler cboProfiles.SelectedIndexChanged, AddressOf OnProfileChanged

            btnTestConn = New ToolStripButton("Test", Nothing, AddressOf OnQuickTestConnection) With {
                .ToolTipText = "Test connection to active profile"
            }
            btnManageProfiles = New ToolStripButton("Profiles...", Nothing, AddressOf OnManageProfiles) With {
                .ToolTipText = "Manage Db2 connection profiles"
            }

            btnOpenFile = New ToolStripButton("Open", Nothing, AddressOf OnOpenScript) With {.ToolTipText = "Open SQL Script (Ctrl+O)"}
            btnSaveFile = New ToolStripButton("Save", Nothing, AddressOf OnSaveScript) With {.ToolTipText = "Save SQL Script (Ctrl+S)"}

            btnRunAll = New ToolStripButton("▶ Execute (F5)", Nothing, AddressOf OnExecuteAll) With {
                .Font = New Font(Me.Font, FontStyle.Bold),
                .ForeColor = Color.DarkGreen,
                .ToolTipText = "Execute all SQL statements in editor (F5)"
            }

            btnRunSelected = New ToolStripButton("▶ Run Selection", Nothing, AddressOf OnExecuteSelected) With {
                .ToolTipText = "Execute only the selected SQL text (Ctrl+F5)"
            }

            btnCancelRun = New ToolStripButton("■ Stop", Nothing, AddressOf OnCancelExecution) With {
                .Enabled = False,
                .ForeColor = Color.DarkRed,
                .ToolTipText = "Cancel running query"
            }

            btnOptions = New ToolStripButton("Options...", Nothing, AddressOf OnShowOptions) With {
                .ToolTipText = "Configure statement delimiter, commit mode, max rows"
            }

            toolStrip.Items.AddRange(New ToolStripItem() {
                lblProfile, cboProfiles, btnTestConn, btnManageProfiles,
                New ToolStripSeparator(),
                btnOpenFile, btnSaveFile,
                New ToolStripSeparator(),
                btnRunAll, btnRunSelected, btnCancelRun,
                New ToolStripSeparator(),
                btnOptions
            })

            ' ── SplitContainer: Editor (Top) & Results (Bottom) ────────────────────
            splitContainer = New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Horizontal,
                .SplitterDistance = 280,
                .SplitterWidth = 6
            }

            ' Top pane: Editor header and RichTextBox
            Dim pnlEditorTop As New Panel() With {.Dock = DockStyle.Top, .Height = 26, .BackColor = Color.FromArgb(240, 240, 240)}
            lblEditorInfo = New Label() With {
                .Text = "SQL Input (Enter statements separated by ';' or load file):",
                .Dock = DockStyle.Left,
                .AutoSize = False,
                .Width = 600,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Padding = New Padding(5, 0, 0, 0),
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            }
            pnlEditorTop.Controls.Add(lblEditorInfo)

            txtSqlInput = New RichTextBox() With {
                .Dock = DockStyle.Fill,
                .Font = New Font("Consolas", 11.0F),
                .AcceptsTab = True,
                .WordWrap = False,
                .ScrollBars = RichTextBoxScrollBars.Both,
                .DetectUrls = False
            }
            txtSqlInput.Text = "SELECT CURRENT SERVER, CURRENT SQLID FROM SYSIBM.SYSDUMMY1;" & vbCrLf &
                               "SELECT CREATOR, NAME, DBNAME FROM SYSIBM.SYSTABLES WHERE DBNAME = 'PRICE3D' ORDER BY NAME;" & vbCrLf

            splitContainer.Panel1.Controls.Add(txtSqlInput)
            splitContainer.Panel1.Controls.Add(pnlEditorTop)

            ' Bottom pane: Tabs for SPUFI Log, Interactive Grids, and Messages
            tabResults = New TabControl() With {.Dock = DockStyle.Fill}

            ' Tab 1: SPUFI Text Output
            tabSpufiLog = New TabPage("SPUFI Text Output")
            Dim pnlLogTop As New Panel() With {.Dock = DockStyle.Top, .Height = 30, .BackColor = Color.FromArgb(245, 245, 245)}
            Dim btnCopyLog As New Button() With {.Text = "Copy All", .Location = New Point(5, 3), .Size = New Size(80, 24)}
            AddHandler btnCopyLog.Click, Sub()
                                             If Not String.IsNullOrEmpty(txtSpufiOutput.Text) Then
                                                 Clipboard.SetText(txtSpufiOutput.Text)
                                                 MessageBox.Show("SPUFI log copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                             End If
                                         End Sub
            Dim btnSaveLogDirect As New Button() With {.Text = "Save Log As...", .Location = New Point(90, 3), .Size = New Size(100, 24)}
            AddHandler btnSaveLogDirect.Click, AddressOf OnSaveSpufiLog
            Dim btnClearLog As New Button() With {.Text = "Clear Log", .Location = New Point(195, 3), .Size = New Size(80, 24)}
            AddHandler btnClearLog.Click, Sub() txtSpufiOutput.Clear()

            pnlLogTop.Controls.AddRange(New Control() {btnCopyLog, btnSaveLogDirect, btnClearLog})

            txtSpufiOutput = New RichTextBox() With {
                .Dock = DockStyle.Fill,
                .Font = New Font("Consolas", 10.0F),
                .ReadOnly = True,
                .WordWrap = False,
                .BackColor = Color.White,
                .ScrollBars = RichTextBoxScrollBars.Both
            }
            tabSpufiLog.Controls.Add(txtSpufiOutput)
            tabSpufiLog.Controls.Add(pnlLogTop)

            ' Tab 2: Interactive Data Grids
            tabGrids = New TabPage("Result Grids")
            tabDataGrids = New TabControl() With {.Dock = DockStyle.Fill}
            tabGrids.Controls.Add(tabDataGrids)

            ' Tab 3: Messages / Diagnostics Log
            tabMessages = New TabPage("Messages & Diagnostics")
            txtMessages = New TextBox() With {
                .Dock = DockStyle.Fill,
                .Multiline = True,
                .ReadOnly = True,
                .ScrollBars = ScrollBars.Both,
                .Font = New Font("Consolas", 9.5F),
                .BackColor = Color.FromArgb(250, 250, 250)
            }
            tabMessages.Controls.Add(txtMessages)

            tabResults.TabPages.AddRange(New TabPage() {tabSpufiLog, tabGrids, tabMessages})
            splitContainer.Panel2.Controls.Add(tabResults)

            ' ── StatusStrip ──────────────────────────────────────────────────────────
            statusStrip = New StatusStrip()
            statusConnLabel = New ToolStripStatusLabel("Disconnected") With {.AutoSize = True}
            statusExecutionLabel = New ToolStripStatusLabel("Ready") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
            statusProgressBar = New ToolStripProgressBar() With {.Visible = False, .Width = 100, .Style = ProgressBarStyle.Marquee}
            statusStatsLabel = New ToolStripStatusLabel("0 statements | 0.00s")

            statusStrip.Items.AddRange(New ToolStripItem() {
                statusConnLabel,
                New ToolStripSeparator(),
                statusExecutionLabel,
                statusProgressBar,
                New ToolStripSeparator(),
                statusStatsLabel
            })

            ' ── Assemble Form ────────────────────────────────────────────────────────
            Me.Controls.Add(splitContainer)
            Me.Controls.Add(statusStrip)
            Me.Controls.Add(toolStrip)
            Me.Controls.Add(menuStrip)
            Me.MainMenuStrip = menuStrip
        End Sub

        ' ── Profile Management ───────────────────────────────────────────────────
        Private Sub LoadProfiles()
            Dim profiles = _profileManager.LoadProfiles()
            cboProfiles.Items.Clear()
            For Each p In profiles
                cboProfiles.Items.Add(p)
            Next

            Dim def = _profileManager.GetDefaultProfile()
            If def IsNot Nothing Then
                Dim idx = profiles.FindIndex(Function(p) p.Id = def.Id)
                If idx >= 0 Then cboProfiles.SelectedIndex = idx
            ElseIf cboProfiles.Items.Count > 0 Then
                cboProfiles.SelectedIndex = 0
            End If

            UpdateConnectionStatus()
        End Sub

        Private Sub OnProfileChanged(sender As Object, e As EventArgs)
            _currentProfile = TryCast(cboProfiles.SelectedItem, ConnectionProfile)
            UpdateConnectionStatus()
        End Sub

        Private Sub UpdateConnectionStatus()
            If _currentProfile IsNot Nothing Then
                statusConnLabel.Text = $"Server: {_currentProfile.Database} @ {_currentProfile.Server}:{_currentProfile.Port} ({_currentProfile.User})"
                statusConnLabel.ForeColor = Color.DarkSlateBlue
            Else
                statusConnLabel.Text = "No profile selected"
                statusConnLabel.ForeColor = Color.DarkRed
            End If
        End Sub

        Private Sub OnManageProfiles(sender As Object, e As EventArgs)
            Using dlg As New ConnectionDialog(_profileManager, _currentProfile)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    LoadProfiles()
                    If dlg.SelectedProfile IsNot Nothing Then
                        For i As Integer = 0 To cboProfiles.Items.Count - 1
                            Dim p = TryCast(cboProfiles.Items(i), ConnectionProfile)
                            If p IsNot Nothing AndAlso p.Id = dlg.SelectedProfile.Id Then
                                cboProfiles.SelectedIndex = i
                                Exit For
                            End If
                        Next
                    End If
                End If
            End Using
        End Sub

        Private Async Sub OnQuickTestConnection(sender As Object, e As EventArgs)
            If _currentProfile Is Nothing Then
                MessageBox.Show("Please select a connection profile first.", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            SetExecutingState(True, "Testing connection...")
            LogMessage($"Testing connection to {_currentProfile.Database} @ {_currentProfile.Server}:{_currentProfile.Port}...")

            Try
                Dim result = Await ConnectionTester.TestConnectionAsync(_currentProfile)
                If result.IsSuccess Then
                    LogMessage($"Connection OK! Roundtrip: {result.RoundTripTime.TotalMilliseconds:F0}ms. Server: {result.CurrentServer}, Version: {result.ServerVersion}, SQLID: {result.CurrentSqlId}")
                    MessageBox.Show($"Connection successful!{vbCrLf}{vbCrLf}Server Version: {result.ServerVersion}{vbCrLf}Current Server: {result.CurrentServer}{vbCrLf}Current SQLID: {result.CurrentSqlId}{vbCrLf}Roundtrip: {result.RoundTripTime.TotalMilliseconds:F0} ms", "Connection OK", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    LogMessage($"Connection Failed: {result.ErrorMessage} (SQLCODE: {result.SqlCode}, SQLSTATE: {result.SqlState})")
                    MessageBox.Show($"Connection failed!{vbCrLf}{vbCrLf}SQLCODE: {result.SqlCode}{vbCrLf}SQLSTATE: {result.SqlState}{vbCrLf}Error: {result.ErrorMessage}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Catch ex As Exception
                LogMessage($"Test error: {ex.Message}")
                MessageBox.Show("Error testing connection: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                SetExecutingState(False, "Ready")
            End Try
        End Sub

        ' ── Query Execution ──────────────────────────────────────────────────────
        Private Sub OnExecuteAll(sender As Object, e As EventArgs)
            ExecuteSql(txtSqlInput.Text)
        End Sub

        Private Sub OnExecuteSelected(sender As Object, e As EventArgs)
            Dim sql = txtSqlInput.SelectedText
            If String.IsNullOrWhiteSpace(sql) Then
                sql = txtSqlInput.Text
            End If
            ExecuteSql(sql)
        End Sub

        Private Async Sub ExecuteSql(sql As String)
            If String.IsNullOrWhiteSpace(sql) Then
                MessageBox.Show("Please enter SQL statements to execute.", "No SQL", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If _currentProfile Is Nothing Then
                MessageBox.Show("Please select or configure a connection profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            _cancellationTokenSource = New CancellationTokenSource()
            SetExecutingState(True, "Starting execution...")

            LogMessage($"--- SPUFI Execution Started ({DateTime.Now:HH:mm:ss}) ---")
            Dim progress As New Progress(Of SpufiProgressUpdate)(Sub(update)
                                                                    statusExecutionLabel.Text = update.StatusMessage
                                                                    If Not String.IsNullOrEmpty(update.CurrentSqlSnippet) Then
                                                                        LogMessage($"Executing [{update.CurrentStatementIndex}/{update.TotalStatements}]: {update.CurrentSqlSnippet}")
                                                                    End If
                                                                End Sub)

            Try
                Dim runResult = Await SpufiEngine.ExecuteScriptAsync(sql, _currentProfile, _options, progress, _cancellationTokenSource.Token)

                ' Render SPUFI text log
                txtSpufiOutput.Text = runResult.FormattedLog

                ' Render DataGrids
                PopulateResultGrids(runResult)

                ' Update status
                statusStatsLabel.Text = $"{runResult.Statements.Count} statements | {runResult.TotalDuration.TotalSeconds:F2}s"
                If runResult.HasErrors Then
                    statusExecutionLabel.Text = $"Completed with errors ({runResult.TotalDuration.TotalSeconds:F2}s)"
                    LogMessage($"Execution completed with errors in {runResult.TotalDuration.TotalSeconds:F2}s.")
                Else
                    statusExecutionLabel.Text = $"Success ({runResult.TotalDuration.TotalSeconds:F2}s)"
                    LogMessage($"Execution completed successfully in {runResult.TotalDuration.TotalSeconds:F2}s.")
                End If

                ' Switch tab: If SELECT query was run, show Result Grids; otherwise SPUFI Text Output
                Dim hasSelect = runResult.Statements.Any(Function(s) s.DataTable IsNot Nothing)
                If hasSelect AndAlso tabDataGrids.TabPages.Count > 0 Then
                    tabResults.SelectedTab = tabGrids
                Else
                    tabResults.SelectedTab = tabSpufiLog
                End If

            Catch ex As OperationCanceledException
                statusExecutionLabel.Text = "Execution cancelled."
                LogMessage("Execution was cancelled by the user.")
            Catch ex As Exception
                statusExecutionLabel.Text = "Execution failed: " & ex.Message
                LogMessage($"Execution error: {ex.Message}")
                MessageBox.Show("Execution error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                SetExecutingState(False, statusExecutionLabel.Text)
                If _cancellationTokenSource IsNot Nothing Then
                    _cancellationTokenSource.Dispose()
                    _cancellationTokenSource = Nothing
                End If
            End Try
        End Sub

        Private Sub PopulateResultGrids(runResult As SpufiRunResult)
            tabDataGrids.TabPages.Clear()

            Dim queryIndex As Integer = 1
            For Each stmt In runResult.Statements
                If stmt.DataTable IsNot Nothing Then
                    Dim pageTitle = $"Query {queryIndex} ({stmt.RowsReturned} rows)"
                    Dim page As New TabPage(pageTitle)

                    Dim topBar As New Panel() With {.Dock = DockStyle.Top, .Height = 32, .BackColor = Color.FromArgb(245, 245, 245)}
                    Dim capturedTable = stmt.DataTable
                    Dim btnExport As New Button() With {
                        .Text = "Export to CSV...",
                        .Location = New Point(6, 4),
                        .Size = New Size(110, 24)
                    }
                    AddHandler btnExport.Click, Sub() ExportDataTableToCsv(capturedTable)

                    Dim lblRows As New Label() With {
                        .Text = $"Rows: {stmt.RowsReturned} | Duration: {stmt.ExecutionDuration.TotalMilliseconds:F0} ms | SQLCODE: {stmt.SqlCode}",
                        .Location = New Point(125, 8),
                        .AutoSize = True,
                        .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
                    }
                    topBar.Controls.AddRange(New Control() {btnExport, lblRows})

                    Dim grid As New DataGridView() With {
                        .Dock = DockStyle.Fill,
                        .DataSource = stmt.DataTable,
                        .ReadOnly = True,
                        .AllowUserToAddRows = False,
                        .AllowUserToDeleteRows = False,
                        .AllowUserToResizeColumns = True,
                        .AllowUserToOrderColumns = True,
                        .BackgroundColor = Color.White,
                        .BorderStyle = BorderStyle.None,
                        .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(240, 244, 250)},
                        .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                    }

                    ' Auto-size initially so columns fit their content, then leave them freely adjustable by the user
                    grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells)
                    For Each col As DataGridViewColumn In grid.Columns
                        col.Resizable = DataGridViewTriState.True
                        col.MinimumWidth = 50
                    Next

                    page.Controls.Add(grid)
                    page.Controls.Add(topBar)
                    tabDataGrids.TabPages.Add(page)
                    queryIndex += 1
                End If
            Next

            If tabDataGrids.TabPages.Count = 0 Then
                Dim emptyPage As New TabPage("No Data")
                Dim lblNone As New Label() With {
                    .Text = "No SELECT queries produced tabular results in this execution run." & vbCrLf &
                            "Check the 'SPUFI Text Output' tab for non-query status, rows affected, or error messages.",
                    .Dock = DockStyle.Fill,
                    .TextAlign = ContentAlignment.MiddleCenter,
                    .ForeColor = Color.Gray
                }
                emptyPage.Controls.Add(lblNone)
                tabDataGrids.TabPages.Add(emptyPage)
            End If
        End Sub

        Private Sub OnCancelExecution(sender As Object, e As EventArgs)
            If _cancellationTokenSource IsNot Nothing Then
                _cancellationTokenSource.Cancel()
                btnCancelRun.Enabled = False
                statusExecutionLabel.Text = "Cancelling execution..."
            End If
        End Sub

        Private Sub SetExecutingState(isExecuting As Boolean, statusText As String)
            btnRunAll.Enabled = Not isExecuting
            btnRunSelected.Enabled = Not isExecuting
            btnCancelRun.Enabled = isExecuting
            cboProfiles.Enabled = Not isExecuting
            statusProgressBar.Visible = isExecuting
            statusExecutionLabel.Text = statusText
        End Sub

        Private Sub LogMessage(msg As String)
            Dim line = $"[{DateTime.Now:HH:mm:ss}] {msg}"
            txtMessages.AppendText(line & vbCrLf)
        End Sub

        ' ── File Operations ──────────────────────────────────────────────────────
        Private Sub OnNewScript(sender As Object, e As EventArgs)
            If ConfirmDiscard() Then
                txtSqlInput.Clear()
                _currentFilePath = Nothing
                UpdateEditorTitle()
            End If
        End Sub

        Private Sub OnOpenScript(sender As Object, e As EventArgs)
            If Not ConfirmDiscard() Then Return

            Using dlg As New OpenFileDialog() With {
                .Filter = "SQL Files (*.sql)|*.sql|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                .Title = "Open SQL Script File"
            }
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    txtSqlInput.Text = File.ReadAllText(dlg.FileName)
                    _currentFilePath = dlg.FileName
                    UpdateEditorTitle()
                    LogMessage($"Loaded script file: {_currentFilePath}")
                End If
            End Using
        End Sub

        Private Sub OnSaveScript(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(_currentFilePath) Then
                OnSaveScriptAs(sender, e)
            Else
                File.WriteAllText(_currentFilePath, txtSqlInput.Text)
                LogMessage($"Saved script file: {_currentFilePath}")
            End If
        End Sub

        Private Sub OnSaveScriptAs(sender As Object, e As EventArgs)
            Using dlg As New SaveFileDialog() With {
                .Filter = "SQL Files (*.sql)|*.sql|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                .Title = "Save SQL Script File"
            }
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    File.WriteAllText(dlg.FileName, txtSqlInput.Text)
                    _currentFilePath = dlg.FileName
                    UpdateEditorTitle()
                    LogMessage($"Saved script file as: {_currentFilePath}")
                End If
            End Using
        End Sub

        Private Sub OnSaveSpufiLog(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(txtSpufiOutput.Text) Then
                MessageBox.Show("SPUFI log is empty. Execute a query first.", "Empty Log", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dlg As New SaveFileDialog() With {
                .Filter = "Log Files (*.log;*.out)|*.log;*.out|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                .Title = "Save SPUFI Output Report",
                .FileName = $"SPUFI_Output_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            }
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    File.WriteAllText(dlg.FileName, txtSpufiOutput.Text)
                    LogMessage($"Saved SPUFI log to: {dlg.FileName}")
                    MessageBox.Show("SPUFI report saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        End Sub

        Private Sub OnExportActiveGridCsv(sender As Object, e As EventArgs)
            If tabDataGrids.SelectedTab IsNot Nothing Then
                For Each ctl As Control In tabDataGrids.SelectedTab.Controls
                    Dim grid = TryCast(ctl, DataGridView)
                    If grid IsNot Nothing Then
                        Dim dt = TryCast(grid.DataSource, DataTable)
                        If dt IsNot Nothing Then
                            ExportDataTableToCsv(dt)
                            Return
                        End If
                    End If
                Next
            End If
            MessageBox.Show("No active result grid to export.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub ExportDataTableToCsv(dt As DataTable)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                MessageBox.Show("No data available to export.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dlg As New SaveFileDialog() With {
                .Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                .Title = "Export Results to CSV",
                .FileName = $"{dt.TableName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            }
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    CsvExporter.ExportToCsvFile(dt, dlg.FileName)
                    LogMessage($"Exported {dt.Rows.Count} rows to CSV: {dlg.FileName}")
                    MessageBox.Show($"Exported {dt.Rows.Count} row(s) to CSV successfully.", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        End Sub

        Private Function ConfirmDiscard() As Boolean
            Return True
        End Function

        Private Sub UpdateEditorTitle()
            Dim name = If(String.IsNullOrEmpty(_currentFilePath), "Untitled Script", Path.GetFileName(_currentFilePath))
            lblEditorInfo.Text = $"SQL Input ({name}) — Delimiter: '{_options.StatementDelimiter}':"
        End Sub

        ' ── Options & About ──────────────────────────────────────────────────────
        Private Sub OnShowOptions(sender As Object, e As EventArgs)
            Using dlg As New OptionsDialog(_options)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    _options = dlg.Options
                    UpdateEditorTitle()
                    LogMessage($"Options updated: Delimiter='{_options.StatementDelimiter}', MaxRows={_options.MaxRows}, CommitMode={_options.CommitMode}, OnError={_options.OnError}")
                End If
            End Using
        End Sub

        Private Sub OnAbout(sender As Object, e As EventArgs)
            MessageBox.Show(
                "SPUFI for Db2 (SQL Processing Using File Input)" & vbCrLf &
                "Targeting IBM Db2 for z/OS & Db2 LUW" & vbCrLf & vbCrLf &
                "Built on .NET 10 | Windows Forms" & vbCrLf &
                "Driver: IBM.Data.Db2 (DRDA Protocol)" & vbCrLf &
                "Core Library: Db2Spufi.Core" & vbCrLf & vbCrLf &
                "Features:" & vbCrLf &
                "- Direct SQL dispatching to Db2 engine" & vbCrLf &
                "- Classic formatted SPUFI text reports" & vbCrLf &
                "- Interactive DataGridView query results" & vbCrLf &
                "- Connection profile manager & test facility" & vbCrLf &
                "- One-click CLI package binding" & vbCrLf &
                "- CSV export of query datasets",
                "About SPUFI for Db2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
        End Sub

    End Class

End Namespace
