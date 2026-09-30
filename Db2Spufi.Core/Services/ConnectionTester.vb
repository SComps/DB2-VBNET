Imports System.Diagnostics
Imports System.Threading.Tasks
Imports IBM.Data.Db2
Imports Db2Spufi.Core.Models

Namespace Services

    Public Class ConnectionTestResult
        Public Property IsSuccess As Boolean
        Public Property ServerVersion As String = ""
        Public Property CurrentServer As String = ""
        Public Property CurrentSqlId As String = ""
        Public Property RoundTripTime As TimeSpan
        Public Property ErrorMessage As String = ""
        Public Property SqlCode As Integer = 0
        Public Property SqlState As String = ""
        Public Property RequiresPackageBind As Boolean = False
    End Class

    Public Class ConnectionTester

        Public Shared Async Function TestConnectionAsync(profile As ConnectionProfile) As Task(Of ConnectionTestResult)
            Dim result As New ConnectionTestResult()
            Dim sw = Stopwatch.StartNew()
            Dim connStr = profile.BuildConnectionString()

            Try
                Using conn As New DB2Connection(connStr)
                    Await conn.OpenAsync()
                    result.ServerVersion = conn.ServerVersion

                    ' Test basic query against dummy table
                    Using cmd As DB2Command = conn.CreateCommand()
                        cmd.CommandText = "SELECT CURRENT SERVER, CURRENT SQLID FROM SYSIBM.SYSDUMMY1"
                        Using reader As DB2DataReader = CType(Await cmd.ExecuteReaderAsync(), DB2DataReader)
                            If Await reader.ReadAsync() Then
                                If Not reader.IsDBNull(0) Then result.CurrentServer = reader.GetString(0).Trim()
                                If Not reader.IsDBNull(1) Then result.CurrentSqlId = reader.GetString(1).Trim()
                            End If
                        End Using
                    End Using

                    sw.Stop()
                    result.RoundTripTime = sw.Elapsed
                    result.IsSuccess = True
                End Using

            Catch ex As DB2Exception
                sw.Stop()
                result.RoundTripTime = sw.Elapsed
                result.IsSuccess = False
                result.ErrorMessage = ex.Message
                If ex.Errors.Count > 0 Then
                    Dim firstErr = ex.Errors(0)
                    result.SqlCode = firstErr.NativeError
                    result.SqlState = firstErr.SQLState
                End If

                For Each err As DB2Error In ex.Errors
                    If err.SQLState = "51002" OrElse err.NativeError = -805 Then
                        result.RequiresPackageBind = True
                        Exit For
                    End If
                Next

            Catch ex As Exception
                sw.Stop()
                result.RoundTripTime = sw.Elapsed
                result.IsSuccess = False
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

    End Class

End Namespace
