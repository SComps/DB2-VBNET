Imports System.Drawing
Imports System.Windows.Forms
Imports Db2Spufi.Core.Config
Imports Db2Spufi.Core.Models
Imports Db2Spufi.Core.Services

Namespace Forms

    Public Class ConnectionDialog
        Inherits Form

        Private ReadOnly _profileManager As ProfileManager
        Private _profiles As List(Of ConnectionProfile)
        Private _selectedProfile As ConnectionProfile

        Private lstProfiles As ListBox
        Private txtName As TextBox
        Private txtServer As TextBox
        Private numPort As NumericUpDown
        Private txtDatabase As TextBox
        Private txtUser As TextBox
        Private txtPassword As TextBox
        Private txtCurrentSqlId As TextBox
        Private chkDefault As CheckBox

        Private btnNew As Button
        Private btnSave As Button
        Private btnDelete As Button
        Private btnTest As Button
        Private btnClose As Button
        Private lblTestResult As Label
        Private txtLog As TextBox

        <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
        Public Property SelectedProfile As ConnectionProfile
            Get
                Return _selectedProfile
            End Get
            Private Set(value As ConnectionProfile)
                _selectedProfile = value
            End Set
        End Property

        Public Sub New(profileManager As ProfileManager, Optional initialSelectedProfile As ConnectionProfile = Nothing)
            _profileManager = profileManager
            _selectedProfile = initialSelectedProfile
            InitializeComponents()
            LoadProfiles()
        End Sub

        Private Sub InitializeComponents()
            Me.Text = "Db2 Connection Profiles"
            Me.Size = New Size(700, 520)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Font = New Font("Segoe UI", 9.0F)

            ' Left panel: Profile list
            Dim lblList As New Label() With {.Text = "Saved Profiles:", .Location = New Point(15, 12), .AutoSize = True}
            lstProfiles = New ListBox() With {.Location = New Point(15, 32), .Size = New Size(220, 390)}
            AddHandler lstProfiles.SelectedIndexChanged, AddressOf OnProfileSelectionChanged

            btnNew = New Button() With {.Text = "+ New Profile", .Location = New Point(15, 432), .Size = New Size(105, 30)}
            AddHandler btnNew.Click, AddressOf OnNewClick

            btnDelete = New Button() With {.Text = "Delete", .Location = New Point(130, 432), .Size = New Size(105, 30)}
            AddHandler btnDelete.Click, AddressOf OnDeleteClick

            ' Right panel: Profile Details
            Dim grpDetails As New GroupBox() With {
                .Text = "Connection Parameters",
                .Location = New Point(250, 15),
                .Size = New Size(420, 300)
            }

            Dim y = 25
            Dim AddField = Sub(lblText As String, ctl As Control)
                               Dim lbl As New Label() With {.Text = lblText, .Location = New Point(20, y + 3), .Width = 120}
                               ctl.Location = New Point(145, y)
                               ctl.Width = 250
                               grpDetails.Controls.Add(lbl)
                               grpDetails.Controls.Add(ctl)
                               y += 32
                           End Sub

            txtName = New TextBox()
            AddField("Profile Name:", txtName)

            txtServer = New TextBox()
            AddField("Server (Host/IP):", txtServer)

            numPort = New NumericUpDown() With {.Minimum = 1, .Maximum = 65535, .Value = 8103}
            AddField("Port:", numPort)

            txtDatabase = New TextBox()
            AddField("Location / DB:", txtDatabase)

            txtUser = New TextBox()
            AddField("User ID:", txtUser)

            txtPassword = New TextBox() With {.UseSystemPasswordChar = True}
            AddField("Password:", txtPassword)

            txtCurrentSqlId = New TextBox()
            AddField("Current SQLID:", txtCurrentSqlId)

            chkDefault = New CheckBox() With {
                .Text = "Default Connection Profile",
                .Location = New Point(145, y),
                .AutoSize = True
            }
            grpDetails.Controls.Add(chkDefault)

            ' Action buttons
            btnSave = New Button() With {.Text = "Save Profile", .Location = New Point(250, 325), .Size = New Size(100, 30)}
            AddHandler btnSave.Click, AddressOf OnSaveClick

            btnTest = New Button() With {.Text = "Test Connection", .Location = New Point(360, 325), .Size = New Size(120, 30)}
            AddHandler btnTest.Click, AddressOf OnTestClick

            btnClose = New Button() With {.Text = "Use & Close", .Location = New Point(560, 325), .Size = New Size(110, 30)}
            AddHandler btnClose.Click, Sub()
                                          Me.DialogResult = DialogResult.OK
                                          Me.Close()
                                      End Sub

            lblTestResult = New Label() With {
                .Location = New Point(250, 365),
                .Size = New Size(420, 35),
                .ForeColor = Color.DarkBlue,
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            }

            txtLog = New TextBox() With {
                .Location = New Point(250, 405),
                .Size = New Size(420, 60),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .ReadOnly = True,
                .Font = New Font("Consolas", 8.5F)
            }

            Me.Controls.AddRange(New Control() {
                lblList, lstProfiles, btnNew, btnDelete,
                grpDetails, btnSave, btnTest, btnClose,
                lblTestResult, txtLog
            })

            Me.AcceptButton = btnClose
        End Sub

        Private Sub LoadProfiles()
            _profiles = _profileManager.LoadProfiles()
            lstProfiles.Items.Clear()
            For Each p In _profiles
                lstProfiles.Items.Add(p)
            Next

            If _selectedProfile IsNot Nothing Then
                Dim idx = _profiles.FindIndex(Function(p) p.Id = _selectedProfile.Id)
                If idx >= 0 Then
                    lstProfiles.SelectedIndex = idx
                ElseIf lstProfiles.Items.Count > 0 Then
                    lstProfiles.SelectedIndex = 0
                End If
            ElseIf lstProfiles.Items.Count > 0 Then
                lstProfiles.SelectedIndex = 0
            End If
        End Sub

        Private Sub OnProfileSelectionChanged(sender As Object, e As EventArgs)
            Dim p = TryCast(lstProfiles.SelectedItem, ConnectionProfile)
            If p IsNot Nothing Then
                _selectedProfile = p
                PopulateFields(p)
            End If
        End Sub

        Private Sub PopulateFields(p As ConnectionProfile)
            txtName.Text = p.Name
            txtServer.Text = p.Server
            numPort.Value = Math.Max(1, Math.Min(65535, p.Port))
            txtDatabase.Text = p.Database
            txtUser.Text = p.User
            txtPassword.Text = p.Password
            txtCurrentSqlId.Text = p.CurrentSqlId
            chkDefault.Checked = p.IsDefault
            lblTestResult.Text = ""
            txtLog.Text = ""
        End Sub

        Private Sub OnNewClick(sender As Object, e As EventArgs)
            Dim newP As New ConnectionProfile With {
                .Name = "New Db2 Server",
                .Server = "10.10.13.2",
                .Port = 8103,
                .Database = "DBD1LOC",
                .User = "SCOTT",
                .Password = "",
                .CurrentSqlId = ""
            }
            _profiles.Add(newP)
            lstProfiles.Items.Add(newP)
            lstProfiles.SelectedItem = newP
            txtName.Focus()
            txtName.SelectAll()
        End Sub

        Private Sub OnSaveClick(sender As Object, e As EventArgs)
            Dim p = TryCast(lstProfiles.SelectedItem, ConnectionProfile)
            If p Is Nothing Then
                p = New ConnectionProfile()
                _profiles.Add(p)
                lstProfiles.Items.Add(p)
            End If

            p.Name = txtName.Text.Trim()
            p.Server = txtServer.Text.Trim()
            p.Port = CInt(numPort.Value)
            p.Database = txtDatabase.Text.Trim()
            p.User = txtUser.Text.Trim()
            p.Password = txtPassword.Text
            p.CurrentSqlId = txtCurrentSqlId.Text.Trim()
            p.IsDefault = chkDefault.Checked

            If p.IsDefault Then
                For Each other In _profiles
                    If other.Id <> p.Id Then other.IsDefault = False
                Next
            End If

            _profileManager.SaveProfiles(_profiles)
            _selectedProfile = p

            ' Refresh list display
            Dim idx = lstProfiles.SelectedIndex
            lstProfiles.Items(idx) = p
            lblTestResult.ForeColor = Color.Green
            lblTestResult.Text = "Profile saved successfully."
        End Sub

        Private Sub OnDeleteClick(sender As Object, e As EventArgs)
            Dim p = TryCast(lstProfiles.SelectedItem, ConnectionProfile)
            If p Is Nothing Then Return

            If _profiles.Count <= 1 Then
                MessageBox.Show("Cannot delete the only connection profile.", "Delete Profile", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If MessageBox.Show($"Are you sure you want to delete profile '{p.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim idx = lstProfiles.SelectedIndex
                _profiles.Remove(p)
                _profileManager.SaveProfiles(_profiles)
                lstProfiles.Items.RemoveAt(idx)
                If lstProfiles.Items.Count > 0 Then
                    lstProfiles.SelectedIndex = Math.Min(idx, lstProfiles.Items.Count - 1)
                End If
            End If
        End Sub

        Private Async Sub OnTestClick(sender As Object, e As EventArgs)
            btnTest.Enabled = False
            lblTestResult.ForeColor = Color.DarkSlateBlue
            lblTestResult.Text = "Testing connection..."
            txtLog.Text = ""

            Dim testProfile As New ConnectionProfile With {
                .Name = txtName.Text.Trim(),
                .Server = txtServer.Text.Trim(),
                .Port = CInt(numPort.Value),
                .Database = txtDatabase.Text.Trim(),
                .User = txtUser.Text.Trim(),
                .Password = txtPassword.Text,
                .CurrentSqlId = txtCurrentSqlId.Text.Trim()
            }

            Try
                Dim result = Await ConnectionTester.TestConnectionAsync(testProfile)

                If result.IsSuccess Then
                    lblTestResult.ForeColor = Color.Green
                    lblTestResult.Text = $"Connection Successful! (Roundtrip: {result.RoundTripTime.TotalMilliseconds:F0} ms)" & vbCrLf &
                                         $"Server: {result.CurrentServer} | Version: {result.ServerVersion} | SQLID: {result.CurrentSqlId}"
                    txtLog.Text = $"Connection opened successfully.{vbCrLf}Server Version: {result.ServerVersion}{vbCrLf}Current Server: {result.CurrentServer}{vbCrLf}Current SQLID: {result.CurrentSqlId}"
                Else
                    lblTestResult.ForeColor = Color.Red
                    lblTestResult.Text = $"Connection Failed! (SQLCODE: {result.SqlCode}, SQLSTATE: {result.SqlState})"
                    txtLog.Text = $"Error: {result.ErrorMessage}{vbCrLf}SQLCODE: {result.SqlCode}{vbCrLf}SQLSTATE: {result.SqlState}"

                    If result.RequiresPackageBind Then
                        Dim answer = MessageBox.Show(
                            "CLI driver packages are not bound on this server (SQL0805N / SQLSTATE 51002)." & vbCrLf & vbCrLf &
                            "Would you like to run the CLI package bind utility now?",
                            "Bind CLI Packages Required",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning)

                        If answer = DialogResult.Yes Then
                            lblTestResult.Text = "Binding CLI packages..."
                            txtLog.AppendText(vbCrLf & "--- Starting CLI Package Bind ---" & vbCrLf)
                            Dim bindOk = Await PackageBinder.BindPackagesAsync(testProfile, Sub(msg)
                                                                                               txtLog.AppendText(msg & vbCrLf)
                                                                                           End Sub)
                            If bindOk Then
                                lblTestResult.ForeColor = Color.Green
                                lblTestResult.Text = "Package bind succeeded! Retrying connection test..."
                                OnTestClick(sender, e)
                            Else
                                lblTestResult.ForeColor = Color.Red
                                lblTestResult.Text = "Package bind failed. Check details in log above."
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                lblTestResult.ForeColor = Color.Red
                lblTestResult.Text = "Test encountered unexpected error: " & ex.Message
                txtLog.Text = ex.ToString()
            Finally
                btnTest.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
