Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Db2Spufi.Core.Models

Namespace Services

    Public Class PackageBinder

        Public Shared Function GetDb2CliPath() As String
            Dim exeDir As String = AppContext.BaseDirectory
            Dim cliBin As String = If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "db2cli.exe", "db2cli")

            ' Priority 1: local clidriver next to executable
            Dim candidate As String = Path.Combine(exeDir, "clidriver", "bin", cliBin)
            If File.Exists(candidate) Then Return candidate

            ' Priority 2: environment variable
            Dim sysDriver As String = Environment.GetEnvironmentVariable("DB2_CLI_DRIVER_INSTALL_PATH")
            If Not String.IsNullOrEmpty(sysDriver) Then
                candidate = Path.Combine(sysDriver, "bin", cliBin)
                If File.Exists(candidate) Then Return candidate
            End If

            ' Priority 3: standard Linux path
            If Not RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                candidate = Path.Combine("/usr/lib/ibm/db2clidriver", "bin", cliBin)
                If File.Exists(candidate) Then Return candidate
            End If

            Return Nothing
        End Function

        Public Shared Function GetBndDirectory() As String
            Dim exeDir As String = AppContext.BaseDirectory
            Dim candidate As String = Path.Combine(exeDir, "clidriver", "bnd")
            If Directory.Exists(candidate) Then Return candidate

            Dim sysDriver As String = Environment.GetEnvironmentVariable("DB2_CLI_DRIVER_INSTALL_PATH")
            If Not String.IsNullOrEmpty(sysDriver) Then
                candidate = Path.Combine(sysDriver, "bnd")
                If Directory.Exists(candidate) Then Return candidate
            End If

            If Not RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                candidate = "/usr/lib/ibm/db2clidriver/bnd"
                If Directory.Exists(candidate) Then Return candidate
            End If

            Return Nothing
        End Function

        Public Shared Async Function BindPackagesAsync(profile As ConnectionProfile,
                                                      logCallback As Action(Of String)) As Task(Of Boolean)
            Dim db2cli = GetDb2CliPath()
            Dim bndDir = GetBndDirectory()

            If String.IsNullOrEmpty(db2cli) OrElse Not File.Exists(db2cli) Then
                logCallback?.Invoke($"ERROR: db2cli executable could not be found.")
                Return False
            End If

            If String.IsNullOrEmpty(bndDir) OrElse Not Directory.Exists(bndDir) Then
                logCallback?.Invoke($"ERROR: .bnd files directory could not be found.")
                Return False
            End If

            Dim bndFiles = Directory.GetFiles(bndDir, "*.bnd")
            If bndFiles.Length = 0 Then
                logCallback?.Invoke($"ERROR: No .bnd files found in {bndDir}")
                Return False
            End If

            Dim target As String = $"{profile.Database}:{profile.Server}:{profile.Port}"
            logCallback?.Invoke($"Starting CLI package bind against {target} for user {profile.User}...")
            logCallback?.Invoke($"Using db2cli: {db2cli}")
            logCallback?.Invoke($"Found {bndFiles.Length} .bnd file(s) in {bndDir}")

            Dim allOk As Boolean = True

            For Each bndFile In bndFiles
                Dim fileName As String = Path.GetFileName(bndFile)
                logCallback?.Invoke($"Binding {fileName}...")

                Dim psi As New Diagnostics.ProcessStartInfo(db2cli) With {
                    .Arguments = $"bind ""{bndFile}"" -database ""{target}"" -user {profile.User} -passwd {profile.Password} -options ""GRANT PUBLIC""",
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }

                Using proc As New Diagnostics.Process() With {.StartInfo = psi}
                    proc.Start()
                    Dim outputTask = proc.StandardOutput.ReadToEndAsync()
                    Dim errTask = proc.StandardError.ReadToEndAsync()
                    Await proc.WaitForExitAsync()
                    Dim output = Await outputTask
                    Dim err = Await errTask

                    If output.Contains("""0"" errors") Then
                        logCallback?.Invoke($"  -> {fileName}: OK")
                    Else
                        logCallback?.Invoke($"  -> {fileName} output:")
                        If Not String.IsNullOrWhiteSpace(output) Then
                            logCallback?.Invoke(output.Trim())
                        End If
                        If Not String.IsNullOrWhiteSpace(err) Then
                            logCallback?.Invoke(err.Trim())
                        End If
                        If output.Contains("SQL0092N") Then
                            allOk = False
                        End If
                    End If
                End Using
            Next

            If allOk Then
                logCallback?.Invoke("CLI package binding complete.")
            Else
                logCallback?.Invoke("CLI package binding completed with errors.")
            End If

            Return allOk
        End Function

    End Class

End Namespace
