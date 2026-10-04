using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Db2Spufi.Core.Config;
using Db2Spufi.Core.Execution;
using Db2Spufi.Core.Formatting;
using Db2Spufi.Core.Models;
using Db2Spufi.Core.Services;

namespace Db2Spufi.Avalonia;

public sealed class TabularRow
{
    public string?[] Values { get; }
    public TabularRow(string?[] values) => Values = values;
}

public partial class MainWindow : Window
{
    private readonly ProfileManager _profileManager;
    private readonly ObservableCollection<ConnectionProfile> _profiles = new();
    private readonly Dictionary<TabItem, DataTable> _gridDataTables = new();

    private ConnectionProfile? _currentProfile;
    private SpufiOptions _options;
    private string? _currentFilePath;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();

        _profileManager = new ProfileManager();
        _options = new SpufiOptions();

        CboProfiles.ItemsSource = _profiles;

        TxtSqlInput.Text = "SELECT CURRENT SERVER, CURRENT SQLID FROM SYSIBM.SYSDUMMY1;\n" +
                           "SELECT CREATOR, NAME, DBNAME FROM SYSIBM.SYSTABLES WHERE DBNAME = 'PRICE3D' ORDER BY NAME;\n";

        LoadProfiles();
        UpdateEditorTitle();
    }

    // ── Profile Management ───────────────────────────────────────────────────
    private void LoadProfiles()
    {
        _profiles.Clear();
        var list = _profileManager.LoadProfiles();
        foreach (var p in list)
        {
            _profiles.Add(p);
        }

        var def = _profileManager.GetDefaultProfile();
        if (def != null)
        {
            var match = _profiles.FirstOrDefault(p => p.Id == def.Id);
            if (match != null)
            {
                CboProfiles.SelectedItem = match;
                UpdateConnectionStatus();
                return;
            }
        }

        if (_profiles.Count > 0)
        {
            CboProfiles.SelectedIndex = 0;
        }

        UpdateConnectionStatus();
    }

    private void OnProfileChanged(object? sender, SelectionChangedEventArgs e)
    {
        _currentProfile = CboProfiles.SelectedItem as ConnectionProfile;
        UpdateConnectionStatus();
    }

    private void UpdateConnectionStatus()
    {
        if (_currentProfile != null)
        {
            StatusConnLabel.Text = $"Server: {_currentProfile.Database} @ {_currentProfile.Server}:{_currentProfile.Port} ({_currentProfile.User})";
            StatusConnLabel.Foreground = Brushes.DarkSlateBlue;
        }
        else
        {
            StatusConnLabel.Text = "No profile selected";
            StatusConnLabel.Foreground = Brushes.Crimson;
        }
    }

    private async void OnManageProfiles(object? sender, RoutedEventArgs e)
    {
        var dlg = new ConnectionDialog(_profileManager, _currentProfile);
        var result = await dlg.ShowDialog<bool>(this);
        if (result)
        {
            LoadProfiles();
            if (dlg.SelectedProfile != null)
            {
                var match = _profiles.FirstOrDefault(p => p.Id == dlg.SelectedProfile.Id);
                if (match != null)
                {
                    CboProfiles.SelectedItem = match;
                }
            }
        }
    }

    private async void OnQuickTestConnection(object? sender, RoutedEventArgs e)
    {
        if (_currentProfile == null)
        {
            await MessageDialog.ShowAsync(this, "Please select a connection profile first.", "Test Connection");
            return;
        }

        SetExecutingState(true, "Testing connection...");
        LogMessage($"Testing connection to {_currentProfile.Database} @ {_currentProfile.Server}:{_currentProfile.Port}...");

        try
        {
            var result = await ConnectionTester.TestConnectionAsync(_currentProfile);
            if (result.IsSuccess)
            {
                LogMessage($"Connection OK! Roundtrip: {result.RoundTripTime.TotalMilliseconds:F0}ms. Server: {result.CurrentServer}, Version: {result.ServerVersion}, SQLID: {result.CurrentSqlId}");
                await MessageDialog.ShowAsync(this,
                    $"Connection successful!\n\nServer Version: {result.ServerVersion}\nCurrent Server: {result.CurrentServer}\nCurrent SQLID: {result.CurrentSqlId}\nRoundtrip: {result.RoundTripTime.TotalMilliseconds:F0} ms",
                    "Connection OK");
            }
            else
            {
                LogMessage($"Connection Failed: {result.ErrorMessage} (SQLCODE: {result.SqlCode}, SQLSTATE: {result.SqlState})");
                await MessageDialog.ShowAsync(this,
                    $"Connection failed!\n\nSQLCODE: {result.SqlCode}\nSQLSTATE: {result.SqlState}\nError: {result.ErrorMessage}",
                    "Connection Error");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Test error: {ex.Message}");
            await MessageDialog.ShowAsync(this, "Error testing connection: " + ex.Message, "Error");
        }
        finally
        {
            SetExecutingState(false, "Ready");
        }
    }

    // ── Query Execution ──────────────────────────────────────────────────────
    private void OnExecuteAll(object? sender, RoutedEventArgs e)
    {
        ExecuteSql(TxtSqlInput.Text ?? "");
    }

    private void OnExecuteSelected(object? sender, RoutedEventArgs e)
    {
        var text = TxtSqlInput.Text ?? "";
        int start = Math.Min(TxtSqlInput.SelectionStart, TxtSqlInput.SelectionEnd);
        int length = Math.Abs(TxtSqlInput.SelectionEnd - TxtSqlInput.SelectionStart);

        string sql = "";
        if (length > 0 && start >= 0 && start + length <= text.Length)
        {
            sql = text.Substring(start, length);
        }

        if (string.IsNullOrWhiteSpace(sql))
        {
            sql = text;
        }

        ExecuteSql(sql);
    }

    private async void ExecuteSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            await MessageDialog.ShowAsync(this, "Please enter SQL statements to execute.", "No SQL");
            return;
        }

        if (_currentProfile == null)
        {
            await MessageDialog.ShowAsync(this, "Please select or configure a connection profile.", "No Profile");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        SetExecutingState(true, "Starting execution...");

        LogMessage($"--- SPUFI Execution Started ({DateTime.Now:HH:mm:ss}) ---");

        var progress = new Progress<SpufiProgressUpdate>(update =>
        {
            StatusExecutionLabel.Text = update.StatusMessage;
            if (!string.IsNullOrEmpty(update.CurrentSqlSnippet))
            {
                LogMessage($"Executing [{update.CurrentStatementIndex}/{update.TotalStatements}]: {update.CurrentSqlSnippet}");
            }
        });

        try
        {
            var runResult = await SpufiEngine.ExecuteScriptAsync(sql, _currentProfile, _options, progress, _cancellationTokenSource.Token);

            // Render SPUFI text log
            TxtSpufiOutput.Text = runResult.FormattedLog;

            // Render DataGrids
            PopulateResultGrids(runResult);

            // Update stats
            StatusStatsLabel.Text = $"{runResult.Statements.Count} statements | {runResult.TotalDuration.TotalSeconds:F2}s";
            if (runResult.HasErrors)
            {
                StatusExecutionLabel.Text = $"Completed with errors ({runResult.TotalDuration.TotalSeconds:F2}s)";
                LogMessage($"Execution completed with errors in {runResult.TotalDuration.TotalSeconds:F2}s.");
            }
            else
            {
                StatusExecutionLabel.Text = $"Success ({runResult.TotalDuration.TotalSeconds:F2}s)";
                LogMessage($"Execution completed successfully in {runResult.TotalDuration.TotalSeconds:F2}s.");
            }

            // Switch to Result Grids tab if SELECT queries returned data, else SPUFI Text Output tab
            bool hasSelect = runResult.Statements.Any(s => s.DataTable != null && s.DataTable.Rows.Count > 0);
            TabResults.SelectedIndex = hasSelect ? 1 : 0;
        }
        catch (OperationCanceledException)
        {
            StatusExecutionLabel.Text = "Execution cancelled.";
            LogMessage("Execution was cancelled by the user.");
        }
        catch (Exception ex)
        {
            StatusExecutionLabel.Text = "Execution failed: " + ex.Message;
            LogMessage($"Execution error: {ex.Message}");
            await MessageDialog.ShowAsync(this, "Execution error: " + ex.Message, "Error");
        }
        finally
        {
            SetExecutingState(false, StatusExecutionLabel.Text ?? "Ready");
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void PopulateResultGrids(SpufiRunResult runResult)
    {
        TabDataGrids.Items.Clear();
        _gridDataTables.Clear();

        int queryIndex = 1;
        foreach (var stmt in runResult.Statements)
        {
            if (stmt.DataTable != null)
            {
                var dt = stmt.DataTable;
                var tabItem = new TabItem
                {
                    Header = $"Query {queryIndex} ({stmt.RowsReturned} rows)"
                };

                var mainGrid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,*")
                };

                // Top bar with Export button & stats
                var topBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(225, 225, 225)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(6, 3)
                };

                var topPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var capturedTable = dt;
                var btnExport = new Button
                {
                    Content = "Export to CSV...",
                    Height = 24,
                    FontSize = 12
                };
                btnExport.Click += async (_, _) => await ExportDataTableToCsvAsync(capturedTable);

                var lblInfo = new TextBlock
                {
                    Text = $"Rows: {stmt.RowsReturned} | Duration: {stmt.ExecutionDuration.TotalMilliseconds:F0} ms | SQLCODE: {stmt.SqlCode}",
                    FontWeight = FontWeight.SemiBold,
                    FontSize = 11.5,
                    VerticalAlignment = VerticalAlignment.Center
                };

                topPanel.Children.Add(btnExport);
                topPanel.Children.Add(lblInfo);
                topBar.Child = topPanel;
                Grid.SetRow(topBar, 0);
                mainGrid.Children.Add(topBar);

                // DataGrid
                var dataGrid = new DataGrid
                {
                    IsReadOnly = true,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    CanUserResizeColumns = true,
                    CanUserReorderColumns = true,
                    BorderThickness = new Thickness(0)
                };

                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    int colIdx = c;
                    var colName = dt.Columns[c].ColumnName;

                    var col = new DataGridTemplateColumn
                    {
                        Header = colName,
                        CanUserSort = false,
                        CellTemplate = new FuncDataTemplate<TabularRow>((row, _) =>
                        {
                            var text = (row != null && colIdx < row.Values.Length) ? row.Values[colIdx] ?? "(null)" : "";
                            return new TextBlock
                            {
                                Text = text,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 11.5,
                                Margin = new Thickness(4, 2)
                            };
                        })
                    };

                    dataGrid.Columns.Add(col);
                }

                // Populate row data
                var rows = new List<TabularRow>(dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    var values = new string?[dt.Columns.Count];
                    for (int c = 0; c < dt.Columns.Count; c++)
                    {
                        values[c] = row.IsNull(c) ? "(null)" : row[c]?.ToString();
                    }
                    rows.Add(new TabularRow(values));
                }

                dataGrid.ItemsSource = rows;

                Grid.SetRow(dataGrid, 1);
                mainGrid.Children.Add(dataGrid);

                tabItem.Content = mainGrid;
                TabDataGrids.Items.Add(tabItem);
                _gridDataTables[tabItem] = dt;

                queryIndex++;
            }
        }

        if (TabDataGrids.Items.Count == 0)
        {
            var emptyTab = new TabItem
            {
                Header = "No Data",
                Content = new TextBlock
                {
                    Text = "No SELECT queries produced tabular results in this execution run.\n" +
                           "Check the 'SPUFI Text Output' tab for statement status, rows affected, or error reports.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Center
                }
            };
            TabDataGrids.Items.Add(emptyTab);
        }
    }

    private void OnCancelExecution(object? sender, RoutedEventArgs e)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            BtnCancelRun.IsEnabled = false;
            StatusExecutionLabel.Text = "Cancelling execution...";
        }
    }

    private void SetExecutingState(bool isExecuting, string statusText)
    {
        BtnRunAll.IsEnabled = !isExecuting;
        BtnRunSelected.IsEnabled = !isExecuting;
        BtnCancelRun.IsEnabled = isExecuting;
        CboProfiles.IsEnabled = !isExecuting;
        StatusProgressBar.IsVisible = isExecuting;
        StatusExecutionLabel.Text = statusText;
    }

    private void LogMessage(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        TxtMessages.Text = (TxtMessages.Text ?? "") + line + "\n";
    }

    // ── File Operations ──────────────────────────────────────────────────────
    private void OnNewScript(object? sender, RoutedEventArgs e)
    {
        TxtSqlInput.Text = "";
        _currentFilePath = null;
        UpdateEditorTitle();
    }

    private async void OnOpenScript(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open SQL Script File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SQL Files (*.sql)") { Patterns = new[] { "*.sql" } },
                new FilePickerFileType("Text Files (*.txt)") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files (*.*)") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            TxtSqlInput.Text = await reader.ReadToEndAsync();
            _currentFilePath = file.Path.LocalPath;
            UpdateEditorTitle();
            LogMessage($"Loaded script file: {_currentFilePath}");
        }
    }

    private async void OnSaveScript(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            OnSaveScriptAs(sender, e);
        }
        else
        {
            await File.WriteAllTextAsync(_currentFilePath, TxtSqlInput.Text ?? "");
            LogMessage($"Saved script file: {_currentFilePath}");
        }
    }

    private async void OnSaveScriptAs(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save SQL Script File",
            DefaultExtension = "sql",
            SuggestedFileName = "script.sql",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SQL Files (*.sql)") { Patterns = new[] { "*.sql" } },
                new FilePickerFileType("Text Files (*.txt)") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files (*.*)") { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            _currentFilePath = file.Path.LocalPath;
            await File.WriteAllTextAsync(_currentFilePath, TxtSqlInput.Text ?? "");
            UpdateEditorTitle();
            LogMessage($"Saved script file as: {_currentFilePath}");
        }
    }

    private async void OnSaveSpufiLog(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSpufiOutput.Text))
        {
            await MessageDialog.ShowAsync(this, "SPUFI log is empty. Execute a query first.", "Empty Log");
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save SPUFI Output Report",
            DefaultExtension = "log",
            SuggestedFileName = $"SPUFI_Output_{DateTime.Now:yyyyMMdd_HHmmss}.log",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Log Files (*.log;*.out)") { Patterns = new[] { "*.log", "*.out" } },
                new FilePickerFileType("Text Files (*.txt)") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files (*.*)") { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            await File.WriteAllTextAsync(path, TxtSpufiOutput.Text);
            LogMessage($"Saved SPUFI log to: {path}");
            await MessageDialog.ShowAsync(this, "SPUFI report saved successfully.", "Saved");
        }
    }

    private async void OnExportActiveGridCsv(object? sender, RoutedEventArgs e)
    {
        if (TabDataGrids.SelectedItem is TabItem selectedTab && _gridDataTables.TryGetValue(selectedTab, out var dt))
        {
            await ExportDataTableToCsvAsync(dt);
        }
        else
        {
            await MessageDialog.ShowAsync(this, "No active result grid to export.", "Export CSV");
        }
    }

    private async Task ExportDataTableToCsvAsync(DataTable dt)
    {
        if (dt == null || dt.Rows.Count == 0)
        {
            await MessageDialog.ShowAsync(this, "No data available to export.", "Export CSV");
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var defaultName = string.IsNullOrEmpty(dt.TableName) ? "QueryResult" : dt.TableName;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Results to CSV",
            DefaultExtension = "csv",
            SuggestedFileName = $"{defaultName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV Files (*.csv)") { Patterns = new[] { "*.csv" } },
                new FilePickerFileType("All Files (*.*)") { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            CsvExporter.ExportToCsvFile(dt, path);
            LogMessage($"Exported {dt.Rows.Count} rows to CSV: {path}");
            await MessageDialog.ShowAsync(this, $"Exported {dt.Rows.Count} row(s) to CSV successfully.", "Export Successful");
        }
    }

    private async void OnCopyLogClick(object? sender, RoutedEventArgs e)
    {
        var text = TxtSpufiOutput.Text;
        if (!string.IsNullOrEmpty(text))
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                await MessageDialog.ShowAsync(this, "SPUFI log copied to clipboard.", "Copied");
            }
        }
    }

    private void OnClearLogClick(object? sender, RoutedEventArgs e)
    {
        TxtSpufiOutput.Text = "";
    }

    private void UpdateEditorTitle()
    {
        var name = string.IsNullOrEmpty(_currentFilePath) ? "Untitled Script" : Path.GetFileName(_currentFilePath);
        LblEditorInfo.Text = $"SQL Input ({name}) — Delimiter: '{_options.StatementDelimiter}':";
    }

    // ── Options & About ──────────────────────────────────────────────────────
    private async void OnShowOptions(object? sender, RoutedEventArgs e)
    {
        var dlg = new OptionsDialog(_options);
        var result = await dlg.ShowDialog<bool>(this);
        if (result)
        {
            _options = dlg.Options;
            UpdateEditorTitle();
            LogMessage($"Options updated: Delimiter='{_options.StatementDelimiter}', MaxRows={_options.MaxRows}, CommitMode={_options.CommitMode}, OnError={_options.OnError}");
        }
    }

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var dlg = new AboutDialog();
        await dlg.ShowDialog(this);
    }

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}