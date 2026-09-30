Imports System.Windows.Forms
Imports Db2Spufi.Core.Execution

Namespace Forms

    Public Class OptionsDialog
        Inherits Form

        Private txtDelimiter As TextBox
        Private numMaxRows As NumericUpDown
        Private cboCommitMode As ComboBox
        Private cboOnError As ComboBox
        Private chkEchoSql As CheckBox
        Private btnOk As Button
        Private btnCancel As Button

        <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
        Public Property Options As SpufiOptions

        Public Sub New(currentOptions As SpufiOptions)
            If currentOptions Is Nothing Then
                Options = New SpufiOptions()
            Else
                Options = New SpufiOptions() With {
                    .StatementDelimiter = currentOptions.StatementDelimiter,
                    .MaxRows = currentOptions.MaxRows,
                    .CommitMode = currentOptions.CommitMode,
                    .OnError = currentOptions.OnError,
                    .EchoInputSql = currentOptions.EchoInputSql
                }
            End If

            InitializeComponents()
            LoadValues()
        End Sub

        Private Sub InitializeComponents()
            Me.Text = "SPUFI Execution Options"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.StartPosition = FormStartPosition.CenterParent
            Me.ClientSize = New Drawing.Size(420, 280)
            Me.Font = New Drawing.Font("Segoe UI", 9.0F, Drawing.FontStyle.Regular)

            Dim lblDelim As New Label() With {.Text = "Statement Delimiter:", .Location = New Drawing.Point(20, 22), .AutoSize = True}
            txtDelimiter = New TextBox() With {.Location = New Drawing.Point(200, 20), .Width = 60, .MaxLength = 5}

            Dim lblMaxRows As New Label() With {.Text = "Max Rows per Query (0 = all):", .Location = New Drawing.Point(20, 57), .AutoSize = True}
            numMaxRows = New NumericUpDown() With {.Location = New Drawing.Point(200, 55), .Width = 100, .Maximum = 1000000, .Minimum = 0}

            Dim lblCommit As New Label() With {.Text = "Transaction Commit Mode:", .Location = New Drawing.Point(20, 92), .AutoSize = True}
            cboCommitMode = New ComboBox() With {.Location = New Drawing.Point(200, 90), .Width = 190, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboCommitMode.Items.AddRange(New Object() {
                "Auto-Commit per statement",
                "Commit on completion",
                "Manual"
            })

            Dim lblError As New Label() With {.Text = "Action on Error:", .Location = New Drawing.Point(20, 127), .AutoSize = True}
            cboOnError = New ComboBox() With {.Location = New Drawing.Point(200, 125), .Width = 190, .DropDownStyle = ComboBoxStyle.DropDownList}
            cboOnError.Items.AddRange(New Object() {
                "Halt execution on error",
                "Continue executing script"
            })

            chkEchoSql = New CheckBox() With {
                .Text = "Echo input SQL statements in SPUFI report",
                .Location = New Drawing.Point(20, 165),
                .AutoSize = True
            }

            btnOk = New Button() With {.Text = "OK", .DialogResult = DialogResult.OK, .Location = New Drawing.Point(220, 225), .Width = 85, .Height = 30}
            btnCancel = New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Drawing.Point(315, 225), .Width = 85, .Height = 30}

            AddHandler btnOk.Click, AddressOf OnOkClick

            Me.Controls.AddRange(New Control() {
                lblDelim, txtDelimiter,
                lblMaxRows, numMaxRows,
                lblCommit, cboCommitMode,
                lblError, cboOnError,
                chkEchoSql,
                btnOk, btnCancel
            })

            Me.AcceptButton = btnOk
            Me.CancelButton = btnCancel
        End Sub

        Private Sub LoadValues()
            txtDelimiter.Text = Options.StatementDelimiter
            numMaxRows.Value = Math.Max(0, Options.MaxRows)
            Select Case Options.CommitMode
                Case SpufiCommitMode.AutoCommitPerStatement : cboCommitMode.SelectedIndex = 0
                Case SpufiCommitMode.CommitOnCompletion : cboCommitMode.SelectedIndex = 1
                Case SpufiCommitMode.Manual : cboCommitMode.SelectedIndex = 2
            End Select

            Select Case Options.OnError
                Case SpufiOnErrorAction.HaltOnError : cboOnError.SelectedIndex = 0
                Case SpufiOnErrorAction.ContinueOnError : cboOnError.SelectedIndex = 1
            End Select

            chkEchoSql.Checked = Options.EchoInputSql
        End Sub

        Private Sub OnOkClick(sender As Object, e As EventArgs)
            Options.StatementDelimiter = If(String.IsNullOrEmpty(txtDelimiter.Text), ";", txtDelimiter.Text)
            Options.MaxRows = CInt(numMaxRows.Value)
            Select Case cboCommitMode.SelectedIndex
                Case 0 : Options.CommitMode = SpufiCommitMode.AutoCommitPerStatement
                Case 1 : Options.CommitMode = SpufiCommitMode.CommitOnCompletion
                Case 2 : Options.CommitMode = SpufiCommitMode.Manual
            End Select

            Select Case cboOnError.SelectedIndex
                Case 0 : Options.OnError = SpufiOnErrorAction.HaltOnError
                Case 1 : Options.OnError = SpufiOnErrorAction.ContinueOnError
            End Select

            Options.EchoInputSql = chkEchoSql.Checked
        End Sub

    End Class

End Namespace
