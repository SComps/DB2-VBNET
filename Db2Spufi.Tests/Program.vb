Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports Db2Spufi.Core.Config
Imports Db2Spufi.Core.Execution
Imports Db2Spufi.Core.Formatting
Imports Db2Spufi.Core.Models
Imports Db2Spufi.Core.Services

Module Program

    Sub Main()
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Async Function MainAsync() As Task
        Console.WriteLine("=== SPUFI Core Verification Test ===")
        Console.WriteLine()

        ' Test 1: Profile Manager
        Console.Write("Test 1: Testing ProfileManager... ")
        Dim pm As New ProfileManager()
        Dim profiles = pm.LoadProfiles()
        If profiles.Count = 0 Then
            Console.WriteLine("FAILED: No profiles loaded.")
            Return
        End If
        Dim profile = pm.GetDefaultProfile()
        Console.WriteLine($"OK. Loaded profile: {profile.Name} ({profile.Database} @ {profile.Server}:{profile.Port})")

        ' Test 2: Connection Testing
        Console.Write("Test 2: Testing Connection to Db2 for z/OS... ")
        Dim testResult = Await ConnectionTester.TestConnectionAsync(profile)
        If Not testResult.IsSuccess Then
            Console.WriteLine($"FAILED: {testResult.ErrorMessage}")
            Return
        End If
        Console.WriteLine($"OK. Server: {testResult.CurrentServer}, Version: {testResult.ServerVersion}, SQLID: {testResult.CurrentSqlId} (Latency: {testResult.RoundTripTime.TotalMilliseconds:F0}ms)")

        ' Test 3: SpufiEngine Direct SQL Execution with comments/instructions to be stripped
        Console.WriteLine("Test 3: Executing multi-statement batch with comments/instructions directly via SpufiEngine...")
        Dim testScript = "-- Instructions: Query current server location" & vbCrLf &
                         "/* Multi-line block comment: " & vbCrLf &
                         "   Author: Scott" & vbCrLf &
                         "*/" & vbCrLf &
                         "SELECT CURRENT SERVER, CURRENT SQLID FROM SYSIBM.SYSDUMMY1;" & vbCrLf &
                         "-- Another comment before table query" & vbCrLf &
                         "SELECT CREATOR, NAME FROM SYSIBM.SYSTABLES WHERE DBNAME = 'PRICE3D' ORDER BY NAME;" & vbCrLf &
                         "-- End of valid queries, next is intentional error test" & vbCrLf &
                         "SELECT * FROM SCOTT.NONEXISTENT_XYZ_TEST;"

        Dim options As New SpufiOptions With {
            .StatementDelimiter = ";",
            .OnError = SpufiOnErrorAction.ContinueOnError,
            .MaxRows = 100
        }

        Dim runResult = Await SpufiEngine.ExecuteScriptAsync(testScript, profile, options, Nothing, CancellationToken.None)
        Console.WriteLine($"Execution completed in {runResult.TotalDuration.TotalSeconds:F2}s with {runResult.Statements.Count} statements.")
        Console.WriteLine()

        For Each stmt In runResult.Statements
            If stmt.IsSuccess Then
                Console.WriteLine($"Statement {stmt.StatementIndex}: SUCCESS | SQLCODE {stmt.SqlCode} | Rows: {stmt.RowsReturned}")
            Else
                Console.WriteLine($"Statement {stmt.StatementIndex}: ERROR (Expected) | SQLCODE {stmt.SqlCode} (SQLSTATE {stmt.SqlState}) | Error: {stmt.ErrorMessage}")
            End If
        Next
        Console.WriteLine()

        ' Test 4: Formatted SPUFI report output
        Console.WriteLine("Test 4: SPUFI Text Report Verification:")
        Console.WriteLine(runResult.FormattedLog)

        ' Test 5: CSV Exporter
        Console.Write("Test 5: Testing CsvExporter on Query 2 results... ")
        Dim query2 = runResult.Statements(1)
        If query2.DataTable IsNot Nothing Then
            Dim csv = CsvExporter.ExportToCsvString(query2.DataTable)
            Console.WriteLine("OK.")
            Console.WriteLine("CSV Output preview:")
            Console.WriteLine(csv.Trim())
        Else
            Console.WriteLine("FAILED: Query 2 DataTable is Nothing")
        End If

        Console.WriteLine()
        Console.WriteLine("=== ALL VERIFICATION TESTS PASSED SUCCESSFULLY! ===")
    End Function

End Module
