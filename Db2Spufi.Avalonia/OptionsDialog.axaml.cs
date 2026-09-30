using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Db2Spufi.Core.Execution;

namespace Db2Spufi.Avalonia;

public partial class OptionsDialog : Window
{
    public SpufiOptions Options { get; private set; }

    public OptionsDialog() : this(new SpufiOptions())
    {
    }

    public OptionsDialog(SpufiOptions currentOptions)
    {
        InitializeComponent();

        Options = new SpufiOptions
        {
            StatementDelimiter = currentOptions?.StatementDelimiter ?? ";",
            MaxRows = currentOptions?.MaxRows ?? 1000,
            CommitMode = currentOptions?.CommitMode ?? SpufiCommitMode.AutoCommitPerStatement,
            OnError = currentOptions?.OnError ?? SpufiOnErrorAction.HaltOnError,
            EchoInputSql = currentOptions?.EchoInputSql ?? true
        };

        LoadValues();
    }

    private void LoadValues()
    {
        TxtDelimiter.Text = Options.StatementDelimiter;
        NumMaxRows.Value = Math.Max(0, Options.MaxRows);

        CboCommitMode.SelectedIndex = Options.CommitMode switch
        {
            SpufiCommitMode.AutoCommitPerStatement => 0,
            SpufiCommitMode.CommitOnCompletion => 1,
            SpufiCommitMode.Manual => 2,
            _ => 0
        };

        CboOnError.SelectedIndex = Options.OnError switch
        {
            SpufiOnErrorAction.HaltOnError => 0,
            SpufiOnErrorAction.ContinueOnError => 1,
            _ => 0
        };

        ChkEchoSql.IsChecked = Options.EchoInputSql;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Options.StatementDelimiter = string.IsNullOrWhiteSpace(TxtDelimiter.Text) ? ";" : TxtDelimiter.Text.Trim();
        Options.MaxRows = (int)(NumMaxRows.Value ?? 1000);

        Options.CommitMode = CboCommitMode.SelectedIndex switch
        {
            0 => SpufiCommitMode.AutoCommitPerStatement,
            1 => SpufiCommitMode.CommitOnCompletion,
            2 => SpufiCommitMode.Manual,
            _ => SpufiCommitMode.AutoCommitPerStatement
        };

        Options.OnError = CboOnError.SelectedIndex switch
        {
            0 => SpufiOnErrorAction.HaltOnError,
            1 => SpufiOnErrorAction.ContinueOnError,
            _ => SpufiOnErrorAction.HaltOnError
        };

        Options.EchoInputSql = ChkEchoSql.IsChecked == true;

        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
