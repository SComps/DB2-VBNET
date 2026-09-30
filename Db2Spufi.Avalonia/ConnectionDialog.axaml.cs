using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Db2Spufi.Core.Config;
using Db2Spufi.Core.Models;
using Db2Spufi.Core.Services;

namespace Db2Spufi.Avalonia;

public partial class ConnectionDialog : Window
{
    private readonly ProfileManager _profileManager;
    private readonly ObservableCollection<ConnectionProfile> _profiles = new();
    public ConnectionProfile? SelectedProfile { get; private set; }

    public ConnectionDialog() : this(new ProfileManager(), null)
    {
    }

    public ConnectionDialog(ProfileManager profileManager, ConnectionProfile? initialSelectedProfile = null)
    {
        _profileManager = profileManager;
        SelectedProfile = initialSelectedProfile;

        InitializeComponent();
        LstProfiles.ItemsSource = _profiles;

        LoadProfiles();
    }

    private void LoadProfiles()
    {
        _profiles.Clear();
        var list = _profileManager.LoadProfiles();
        foreach (var p in list)
        {
            _profiles.Add(p);
        }

        if (SelectedProfile != null)
        {
            var match = _profiles.FirstOrDefault(p => p.Id == SelectedProfile.Id);
            if (match != null)
            {
                LstProfiles.SelectedItem = match;
                return;
            }
        }

        if (_profiles.Count > 0)
        {
            LstProfiles.SelectedIndex = 0;
        }
    }

    private void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstProfiles.SelectedItem is ConnectionProfile p)
        {
            SelectedProfile = p;
            PopulateFields(p);
        }
    }

    private void PopulateFields(ConnectionProfile p)
    {
        TxtName.Text = p.Name;
        TxtServer.Text = p.Server;
        NumPort.Value = Math.Max(1, Math.Min(65535, p.Port));
        TxtDatabase.Text = p.Database;
        TxtUser.Text = p.User;
        TxtPassword.Text = p.Password;
        TxtCurrentSqlId.Text = p.CurrentSqlId;
        ChkDefault.IsChecked = p.IsDefault;
        LblTestResult.Text = "";
        TxtLog.Text = "";
        BtnBindPackages.IsVisible = false;
    }

    private void OnNewClick(object? sender, RoutedEventArgs e)
    {
        var newP = new ConnectionProfile
        {
            Name = "New Db2 Server",
            Server = "10.10.13.2",
            Port = 8103,
            Database = "DBD1LOC",
            User = "SCOTT",
            Password = "",
            CurrentSqlId = ""
        };

        _profiles.Add(newP);
        LstProfiles.SelectedItem = newP;
        TxtName.Focus();
        TxtName.SelectAll();
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (LstProfiles.SelectedItem is not ConnectionProfile p) return;

        if (_profiles.Count <= 1)
        {
            LblTestResult.Foreground = Brushes.OrangeRed;
            LblTestResult.Text = "Cannot delete the only connection profile.";
            return;
        }

        var idx = LstProfiles.SelectedIndex;
        _profiles.Remove(p);
        _profileManager.SaveProfiles(_profiles.ToList());

        if (_profiles.Count > 0)
        {
            LstProfiles.SelectedIndex = Math.Clamp(idx, 0, _profiles.Count - 1);
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (LstProfiles.SelectedItem is not ConnectionProfile p)
        {
            p = new ConnectionProfile();
            _profiles.Add(p);
            LstProfiles.SelectedItem = p;
        }

        p.Name = (TxtName.Text ?? "").Trim();
        p.Server = (TxtServer.Text ?? "").Trim();
        p.Port = (int)(NumPort.Value ?? 8103);
        p.Database = (TxtDatabase.Text ?? "").Trim();
        p.User = (TxtUser.Text ?? "").Trim();
        p.Password = TxtPassword.Text ?? "";
        p.CurrentSqlId = (TxtCurrentSqlId.Text ?? "").Trim();
        p.IsDefault = ChkDefault.IsChecked == true;

        if (p.IsDefault)
        {
            foreach (var other in _profiles)
            {
                if (other.Id != p.Id) other.IsDefault = false;
            }
        }

        _profileManager.SaveProfiles(_profiles.ToList());
        SelectedProfile = p;

        // Force refresh list items
        var curIdx = LstProfiles.SelectedIndex;
        _profiles[curIdx] = p;
        LstProfiles.SelectedIndex = curIdx;

        LblTestResult.Foreground = Brushes.ForestGreen;
        LblTestResult.Text = "Profile saved successfully.";
    }

    private async void OnTestClick(object? sender, RoutedEventArgs e)
    {
        BtnTest.IsEnabled = false;
        LblTestResult.Foreground = Brushes.SteelBlue;
        LblTestResult.Text = "Testing connection to Db2...";
        TxtLog.Text = "";
        BtnBindPackages.IsVisible = false;

        var testProfile = BuildProfileFromInputs();

        try
        {
            var result = await ConnectionTester.TestConnectionAsync(testProfile);

            if (result.IsSuccess)
            {
                LblTestResult.Foreground = Brushes.ForestGreen;
                LblTestResult.Text = $"Connection Successful! (Roundtrip: {result.RoundTripTime.TotalMilliseconds:F0} ms)\n" +
                                     $"Server: {result.CurrentServer} | Version: {result.ServerVersion} | SQLID: {result.CurrentSqlId}";
                TxtLog.Text = $"Connection opened successfully.\nServer Version: {result.ServerVersion}\nCurrent Server: {result.CurrentServer}\nCurrent SQLID: {result.CurrentSqlId}";
            }
            else
            {
                LblTestResult.Foreground = Brushes.Crimson;
                LblTestResult.Text = $"Connection Failed! (SQLCODE: {result.SqlCode}, SQLSTATE: {result.SqlState})";
                TxtLog.Text = $"Error: {result.ErrorMessage}\nSQLCODE: {result.SqlCode}\nSQLSTATE: {result.SqlState}";

                if (result.RequiresPackageBind)
                {
                    BtnBindPackages.IsVisible = true;
                    LblTestResult.Text += "\nNote: CLI driver packages need to be bound on this Db2 subsystem.";
                }
            }
        }
        catch (Exception ex)
        {
            LblTestResult.Foreground = Brushes.Crimson;
            LblTestResult.Text = $"Unexpected error: {ex.Message}";
            TxtLog.Text = ex.ToString();
        }
        finally
        {
            BtnTest.IsEnabled = true;
        }
    }

    private async void OnBindPackagesClick(object? sender, RoutedEventArgs e)
    {
        BtnBindPackages.IsEnabled = false;
        LblTestResult.Foreground = Brushes.DarkOrange;
        LblTestResult.Text = "Binding CLI packages...";
        TxtLog.Text += "\n--- Starting CLI Package Bind ---\n";

        var testProfile = BuildProfileFromInputs();

        try
        {
            var success = await PackageBinder.BindPackagesAsync(testProfile, msg =>
            {
                TxtLog.Text += msg + "\n";
            });

            if (success)
            {
                LblTestResult.Foreground = Brushes.ForestGreen;
                LblTestResult.Text = "CLI package bind succeeded! Retesting connection...";
                OnTestClick(sender, e);
            }
            else
            {
                LblTestResult.Foreground = Brushes.Crimson;
                LblTestResult.Text = "Package bind failed. Review details above.";
            }
        }
        catch (Exception ex)
        {
            LblTestResult.Foreground = Brushes.Crimson;
            LblTestResult.Text = $"Package bind error: {ex.Message}";
            TxtLog.Text += "\n" + ex;
        }
        finally
        {
            BtnBindPackages.IsEnabled = true;
        }
    }

    private ConnectionProfile BuildProfileFromInputs()
    {
        return new ConnectionProfile
        {
            Name = (TxtName.Text ?? "").Trim(),
            Server = (TxtServer.Text ?? "").Trim(),
            Port = (int)(NumPort.Value ?? 8103),
            Database = (TxtDatabase.Text ?? "").Trim(),
            User = (TxtUser.Text ?? "").Trim(),
            Password = TxtPassword.Text ?? "",
            CurrentSqlId = (TxtCurrentSqlId.Text ?? "").Trim(),
            IsDefault = ChkDefault.IsChecked == true
        };
    }

    private void OnUseCloseClick(object? sender, RoutedEventArgs e)
    {
        if (LstProfiles.SelectedItem is ConnectionProfile p)
        {
            SelectedProfile = p;
        }
        Close(true);
    }
}
