Imports IBM.Data.Db2

Namespace Db

    ''' <summary>Shared connection factory and bind-on-demand logic.</summary>
    Public Module Database

        Public Const Server   As String = "10.10.13.2"
        Public Const Port     As String = "8103"
        Public Const Location As String = "DBD1LOC"
        Public Const User     As String = "SCOTT"
        Public Const Password As String = "mlkhbu"

        Public ReadOnly Property ConnectionString As String
            Get
                Return $"Server={Server}:{Port};Database={Location};UID={User};PWD={Password};"
            End Get
        End Property

        ''' <summary>Opens a new connection, auto-binding packages on SQL0805N.</summary>
        Public Function OpenConnection() As DB2Connection
            Dim conn As New DB2Connection(ConnectionString)
            Try
                conn.Open()
            Catch ex As DB2Exception When IsPackageNotFound(ex)
                conn.Dispose()
                BindCliPackages()
                conn = New DB2Connection(ConnectionString)
                conn.Open()
            End Try
            Return conn
        End Function

        Private Function IsPackageNotFound(ex As DB2Exception) As Boolean
            For Each err As DB2Error In ex.Errors
                If err.SQLState = "51002" OrElse err.NativeError = -805 Then Return True
            Next
            Return False
        End Function

        Private Sub BindCliPackages()
            Dim exeDir As String = AppContext.BaseDirectory
            Dim db2cli As String = IO.Path.Combine(exeDir, "clidriver", "bin", "db2cli.exe")
            Dim bndDir As String = IO.Path.Combine(exeDir, "clidriver", "bnd")
            If Not IO.File.Exists(db2cli) Then Return
            Dim target As String = $"{Location}:{Server}:{Port}"
            For Each bndFile In IO.Directory.GetFiles(bndDir, "*.bnd")
                Dim psi As New Diagnostics.ProcessStartInfo(db2cli) With {
                    .Arguments = $"bind ""{bndFile}"" -database ""{target}"" -user {User} -passwd {Password} -options ""GRANT PUBLIC""",
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                Using proc = Diagnostics.Process.Start(psi)
                    proc.StandardOutput.ReadToEnd()
                    proc.WaitForExit()
                End Using
            Next
        End Sub

    End Module

End Namespace
