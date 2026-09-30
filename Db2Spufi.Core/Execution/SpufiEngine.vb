Imports System.Data
Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks
Imports IBM.Data.Db2
Imports Db2Spufi.Core.Formatting
Imports Db2Spufi.Core.Models

Namespace Execution

    Public Class SpufiEngine

        Public Shared Async Function ExecuteScriptAsync(sqlScript As String,
                                                       profile As ConnectionProfile,
                                                       options As SpufiOptions,
                                                       progress As IProgress(Of SpufiProgressUpdate),
                                                       cancellationToken As CancellationToken) As Task(Of SpufiRunResult)
            Dim runResult As New SpufiRunResult()
            Dim totalSw = Stopwatch.StartNew()

            If options Is Nothing Then options = New SpufiOptions()

            ' Direct split by statement delimiter without client-side parsing
            Dim rawStatements As List(Of String) = SplitStatements(sqlScript, options.StatementDelimiter)
            If rawStatements.Count = 0 Then
                totalSw.Stop()
                runResult.TotalDuration = totalSw.Elapsed
                runResult.FormattedLog = SpufiFormatter.FormatRunResult(runResult, profile)
                Return runResult
            End If

            Dim connStr = profile.BuildConnectionString()

            Using conn As New DB2Connection(connStr)
                Await conn.OpenAsync(cancellationToken)

                Dim tx As DB2Transaction = Nothing
                If options.CommitMode = SpufiCommitMode.CommitOnCompletion Then
                    tx = conn.BeginTransaction()
                End If

                Try
                    For i As Integer = 0 To rawStatements.Count - 1
                        cancellationToken.ThrowIfCancellationRequested()

                        Dim stmtSql = rawStatements(i)
                        Dim stmtNum = i + 1

                        progress?.Report(New SpufiProgressUpdate With {
                            .CurrentStatementIndex = stmtNum,
                            .TotalStatements = rawStatements.Count,
                            .CurrentSqlSnippet = GetSqlSnippet(stmtSql),
                            .StatusMessage = $"Executing statement {stmtNum} of {rawStatements.Count}..."
                        })

                        Dim stmtResult = Await ExecuteSingleStatementAsync(conn, tx, stmtSql, stmtNum, options, cancellationToken)
                        runResult.Statements.Add(stmtResult)

                        If Not stmtResult.IsSuccess Then
                            runResult.HasErrors = True
                            If options.OnError = SpufiOnErrorAction.HaltOnError Then
                                Exit For
                            End If
                        End If
                    Next

                    ' Transaction finalization
                    If tx IsNot Nothing Then
                        If runResult.HasErrors Then
                            Await tx.RollbackAsync(cancellationToken)
                        Else
                            Await tx.CommitAsync(cancellationToken)
                        End If
                    End If

                Catch ex As OperationCanceledException
                    If tx IsNot Nothing Then tx.Rollback()
                    Throw
                Catch ex As Exception
                    If tx IsNot Nothing Then tx.Rollback()
                    Throw
                Finally
                    If tx IsNot Nothing Then
                        tx.Dispose()
                    End If
                End Try
            End Using

            totalSw.Stop()
            runResult.TotalDuration = totalSw.Elapsed
            runResult.FormattedLog = SpufiFormatter.FormatRunResult(runResult, profile)

            progress?.Report(New SpufiProgressUpdate With {
                .CurrentStatementIndex = rawStatements.Count,
                .TotalStatements = rawStatements.Count,
                .StatusMessage = $"Execution completed in {runResult.TotalDuration.TotalSeconds:F2}s"
            })

            Return runResult
        End Function

        ''' <summary>
        ''' Strips SQL comments (-- single line and /* */ block) leaving only valid SQL.
        ''' Accurately preserves string literals containing comment markers.
        ''' </summary>
        Public Shared Function StripComments(sqlScript As String) As String
            If String.IsNullOrWhiteSpace(sqlScript) Then Return ""

            Dim sb As New Text.StringBuilder()
            Dim i As Integer = 0
            Dim len As Integer = sqlScript.Length
            Dim inSingleQuote As Boolean = False

            While i < len
                Dim c = sqlScript(i)
                Dim nextC = If(i + 1 < len, sqlScript(i + 1), ControlChars.NullChar)

                If Not inSingleQuote Then
                    ' Single-line comment: --
                    If c = "-"c AndAlso nextC = "-"c Then
                        i += 2
                        While i < len AndAlso sqlScript(i) <> vbLf AndAlso sqlScript(i) <> vbCr
                            i += 1
                        End While
                        Continue While
                    End If

                    ' Multi-line comment: /* ... */
                    If c = "/"c AndAlso nextC = "*"c Then
                        i += 2
                        While i < len
                            If sqlScript(i) = "*"c AndAlso i + 1 < len AndAlso sqlScript(i + 1) = "/"c Then
                                i += 2
                                Exit While
                            End If
                            i += 1
                        End While
                        Continue While
                    End If

                    If c = "'"c Then
                        inSingleQuote = True
                        sb.Append(c)
                        i += 1
                        Continue While
                    End If
                Else
                    ' Inside string literal: handle escaped single quote ''
                    If c = "'"c Then
                        If nextC = "'"c Then
                            sb.Append("''")
                            i += 2
                            Continue While
                        Else
                            inSingleQuote = False
                        End If
                    End If
                End If

                sb.Append(c)
                i += 1
            End While

            Return sb.ToString().Trim()
        End Function

        Private Shared Function SplitStatements(sqlScript As String, delimiter As String) As List(Of String)
            Dim list As New List(Of String)()
            If String.IsNullOrWhiteSpace(sqlScript) Then Return list

            ' Remove all comments leaving only valid SQL statements
            Dim cleanScript = StripComments(sqlScript)
            If String.IsNullOrWhiteSpace(cleanScript) Then Return list

            If String.IsNullOrEmpty(delimiter) Then
                Dim trimmed = cleanScript.Trim()
                If Not String.IsNullOrEmpty(trimmed) Then list.Add(trimmed)
                Return list
            End If

            Dim parts = cleanScript.Split(New String() {delimiter}, StringSplitOptions.None)
            For Each part In parts
                Dim cleanPart = StripComments(part).Trim()
                If Not String.IsNullOrEmpty(cleanPart) Then
                    list.Add(cleanPart)
                End If
            Next

            Return list
        End Function

        Private Shared Function GetSqlSnippet(sql As String) As String
            Dim oneLine = sql.Replace(vbCr, " ").Replace(vbLf, " ").Trim()
            If oneLine.Length > 60 Then
                Return oneLine.Substring(0, 57) & "..."
            End If
            Return oneLine
        End Function

        Private Shared Async Function ExecuteSingleStatementAsync(conn As DB2Connection,
                                                                 tx As DB2Transaction,
                                                                 sql As String,
                                                                 stmtIndex As Integer,
                                                                 options As SpufiOptions,
                                                                 cancellationToken As CancellationToken) As Task(Of SpufiStatementResult)
            Dim result As New SpufiStatementResult With {
                .StatementIndex = stmtIndex,
                .SqlText = sql
            }

            Dim isSelect = IsSelectQuery(sql)
            Dim sw = Stopwatch.StartNew()

            Try
                Using cmd As DB2Command = conn.CreateCommand()
                    cmd.CommandText = sql
                    If tx IsNot Nothing Then cmd.Transaction = tx

                    If isSelect Then
                        result.StatementType = SpufiStatementType.SelectQuery
                        Using reader As DB2DataReader = CType(Await cmd.ExecuteReaderAsync(cancellationToken), DB2DataReader)
                            Dim dt As New DataTable("Result_" & stmtIndex)
                            Dim colCount = reader.FieldCount

                            For c As Integer = 0 To colCount - 1
                                Dim colName = reader.GetName(c)
                                If String.IsNullOrEmpty(colName) Then colName = $"COL_{c + 1}"
                                ' Ensure unique column names in DataTable
                                Dim uniqueName = colName
                                Dim suffix As Integer = 1
                                While dt.Columns.Contains(uniqueName)
                                    suffix += 1
                                    uniqueName = $"{colName}_{suffix}"
                                End While
                                dt.Columns.Add(uniqueName, GetType(String))
                            Next

                            Dim rowsRead As Integer = 0
                            While Await reader.ReadAsync(cancellationToken)
                                If options.MaxRows > 0 AndAlso rowsRead >= options.MaxRows Then
                                    Exit While
                                End If

                                Dim row = dt.NewRow()
                                For c As Integer = 0 To colCount - 1
                                    row(c) = If(reader.IsDBNull(c), "NULL", reader.GetValue(c).ToString())
                                Next
                                dt.Rows.Add(row)
                                rowsRead += 1
                            End While

                            result.DataTable = dt
                            result.RowsReturned = rowsRead
                            result.IsSuccess = True
                            result.SqlCode = 0
                            result.SqlState = "00000"
                        End Using
                    Else
                        result.StatementType = SpufiStatementType.NonQuery
                        Dim rowsAffected = Await cmd.ExecuteNonQueryAsync(cancellationToken)
                        result.RowsAffected = rowsAffected
                        result.IsSuccess = True
                        result.SqlCode = 0
                        result.SqlState = "00000"
                    End If
                End Using

            Catch ex As DB2Exception
                sw.Stop()
                result.ExecutionDuration = sw.Elapsed
                result.IsSuccess = False
                result.ErrorMessage = ex.Message
                If ex.Errors.Count > 0 Then
                    Dim firstErr = ex.Errors(0)
                    result.SqlCode = firstErr.NativeError
                    result.SqlState = firstErr.SQLState
                Else
                    result.SqlCode = -1
                    result.SqlState = "ERROR"
                End If
                Return result

            Catch ex As Exception
                sw.Stop()
                result.ExecutionDuration = sw.Elapsed
                result.IsSuccess = False
                result.ErrorMessage = ex.Message
                result.SqlCode = -1
                result.SqlState = "ERROR"
                Return result
            End Try

            sw.Stop()
            result.ExecutionDuration = sw.Elapsed
            Return result
        End Function

        Private Shared Function IsSelectQuery(sql As String) As Boolean
            Dim trimmed = sql.TrimStart()
            ' Strip leading SQL comments if present at very beginning
            While trimmed.StartsWith("--")
                Dim nl = trimmed.IndexOfAny(New Char() {ChrW(10), ChrW(13)})
                If nl = -1 Then Return False
                trimmed = trimmed.Substring(nl).TrimStart()
            End While

            Return trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("VALUES", StringComparison.OrdinalIgnoreCase)
        End Function

    End Class

End Namespace
