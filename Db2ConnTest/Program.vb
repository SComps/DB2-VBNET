Imports IBM.Data.Db2
Imports System.IO

Module Program

    Const Server   As String = "10.10.13.2"
    Const Port     As String = "8103"
    Const Database As String = "DBD1LOC"
    Const User     As String = "SCOTT"
    Const Password As String = "mlkhbu"

    Sub Main()
        Dim connStr As String = $"Server={Server}:{Port};Database={Database};UID={User};PWD={Password};"
        Using conn As New DB2Connection(connStr)
            Try
                conn.Open()
                Console.WriteLine("Connection successful. Server version: " & conn.ServerVersion)
                Console.WriteLine()
                QueryPrice3D(conn)
            Catch ex As DB2Exception When IsPackageNotFound(ex)
                Console.WriteLine("CLI packages not bound on server. Running bind now...")
                Console.WriteLine()
                If BindCliPackages() Then
                    Console.WriteLine("Bind succeeded. Retrying query...")
                    Console.WriteLine()
                    ' Reopen — connection state is broken after the error
                    conn.Close()
                    conn.Open()
                    QueryPrice3D(conn)
                Else
                    Console.WriteLine("Bind failed. Cannot continue.")
                End If
            Catch ex As Exception
                Console.WriteLine("Error: " & ex.Message)
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' Returns True when the DB2Exception is SQL0805N — package not found.
    ''' SQLSTATE 51002 is the standard code for this condition on z/OS.
    ''' </summary>
    Private Function IsPackageNotFound(ex As DB2Exception) As Boolean
        For Each err As DB2Error In ex.Errors
            If err.SQLState = "51002" OrElse err.NativeError = -805 Then
                Return True
            End If
        Next
        Return False
    End Function

    ''' <summary>
    ''' Locates the clidriver shipped with the NuGet package (next to the running
    ''' executable) and binds all .bnd files against the configured server.
    ''' Returns True on success.
    ''' </summary>
    Private Function BindCliPackages() As Boolean
        ' Locate db2cli and bnd files.
        ' Priority: local clidriver next to the exe (Windows publish),
        ' then DB2_CLI_DRIVER_INSTALL_PATH env var,
        ' then the system-wide install at /opt/ibm/db2clidriver.
        Dim exeDir  As String = AppContext.BaseDirectory
        Dim cliBin  As String = If(Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                                    Runtime.InteropServices.OSPlatform.Windows), "db2cli.exe", "db2cli")
        Dim db2cli  As String = Path.Combine(exeDir, "clidriver", "bin", cliBin)
        Dim bndDir  As String = Path.Combine(exeDir, "clidriver", "bnd")

        If Not File.Exists(db2cli) Then
            Dim sysDriver As String = Environment.GetEnvironmentVariable("DB2_CLI_DRIVER_INSTALL_PATH")
            If String.IsNullOrEmpty(sysDriver) Then sysDriver = "/usr/lib/ibm/db2clidriver"
            db2cli = Path.Combine(sysDriver, "bin", cliBin)
            bndDir = Path.Combine(sysDriver, "bnd")
        End If

        If Not File.Exists(db2cli) Then
            Console.WriteLine($"db2cli not found at: {db2cli}")
            Return False
        End If

        Dim bndFiles = Directory.GetFiles(bndDir, "*.bnd")
        If bndFiles.Length = 0 Then
            Console.WriteLine($"No .bnd files found in: {bndDir}")
            Return False
        End If

        Dim target As String = $"{Database}:{Server}:{Port}"
        Dim allOk  As Boolean = True

        For Each bndFile In bndFiles
            Dim name As String = Path.GetFileName(bndFile)
            Console.Write($"  Binding {name} ... ")

            Dim psi As New Diagnostics.ProcessStartInfo(db2cli) With {
                .Arguments              = $"bind ""{bndFile}"" -database ""{target}"" -user {User} -passwd {Password} -options ""GRANT PUBLIC""",
                .RedirectStandardOutput = True,
                .RedirectStandardError  = True,
                .UseShellExecute        = False,
                .CreateNoWindow         = True
            }

            Using proc As Diagnostics.Process = Diagnostics.Process.Start(psi)
                Dim output As String = proc.StandardOutput.ReadToEnd()
                proc.WaitForExit()

                If output.Contains("""0"" errors") Then
                    Console.WriteLine("OK")
                Else
                    Console.WriteLine("warnings/errors — check output:")
                    Console.WriteLine(output)
                    ' Warnings (e.g. unsupported LUW options on z/OS) are non-fatal
                    If output.Contains("SQL0092N") Then allOk = False
                End If
            End Using
        Next

        Return allOk
    End Function

    ''' <summary>
    ''' Queries all tables in the PRICE3D database and prints their contents.
    ''' </summary>
    Private Sub QueryPrice3D(conn As DB2Connection)
        Dim tableNames  As New List(Of String)
        Dim schemaNames As New List(Of String)

        Dim tableQuery As String =
            "SELECT CREATOR, NAME FROM SYSIBM.SYSTABLES " &
            "WHERE DBNAME = 'PRICE3D' AND TYPE = 'T' " &
            "ORDER BY CREATOR, NAME"

        Using tableCmd As New DB2Command(tableQuery, conn)
            Using reader As DB2DataReader = tableCmd.ExecuteReader()
                While reader.Read()
                    schemaNames.Add(reader.GetString(0).Trim())
                    tableNames.Add(reader.GetString(1).Trim())
                End While
            End Using
        End Using

        If tableNames.Count = 0 Then
            Console.WriteLine("No tables found in database PRICE3D.")
            Return
        End If

        Console.WriteLine($"Found {tableNames.Count} table(s) in PRICE3D:")
        For i As Integer = 0 To tableNames.Count - 1
            Console.WriteLine($"  - {schemaNames(i)}.{tableNames(i)}")
        Next
        Console.WriteLine()

        For i As Integer = 0 To tableNames.Count - 1
            Dim schema    As String = schemaNames(i)
            Dim tableName As String = tableNames(i)
            Console.WriteLine($"=== {schema}.{tableName} ===")

            Using dataCmd As New DB2Command($"SELECT * FROM {schema}.{tableName}", conn)
                Using reader As DB2DataReader = dataCmd.ExecuteReader()
                    Dim colCount As Integer = reader.FieldCount
                    Dim headers  As New List(Of String)
                    For c As Integer = 0 To colCount - 1
                        headers.Add(reader.GetName(c))
                    Next
                    Console.WriteLine(String.Join(" | ", headers))
                    Console.WriteLine(New String("-"c, 80))

                    Dim rowCount As Integer = 0
                    While reader.Read()
                        Dim values As New List(Of String)
                        For c As Integer = 0 To colCount - 1
                            values.Add(If(reader.IsDBNull(c), "NULL", reader.GetValue(c).ToString()))
                        Next
                        Console.WriteLine(String.Join(" | ", values))
                        rowCount += 1
                    End While
                    Console.WriteLine($"({rowCount} row(s))")
                End Using
            End Using
            Console.WriteLine()
        Next i
    End Sub

End Module
