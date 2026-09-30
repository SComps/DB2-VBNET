Imports System.Data
Imports System.Text
Imports Db2Spufi.Core.Execution
Imports Db2Spufi.Core.Models

Namespace Formatting

    Public Class SpufiFormatter
        Private Const RulerLine As String = "---------+---------+---------+---------+---------+---------+---------+---------+"

        Public Shared Function FormatRunResult(runResult As SpufiRunResult, profile As ConnectionProfile) As String
            Dim sb As New StringBuilder()

            sb.AppendLine("=== SPUFI SQL PROCESSING REPORT ===")
            If profile IsNot Nothing Then
                sb.AppendLine($"Server / Subsystem : {profile.Database} @ {profile.Server}:{profile.Port}")
                sb.AppendLine($"User ID            : {profile.User}")
            End If
            sb.AppendLine($"Run Timestamp      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            sb.AppendLine($"Statements Executed: {runResult.Statements.Count}")
            sb.AppendLine($"Total Elapsed Time : {runResult.TotalDuration.TotalSeconds:F3}s")
            sb.AppendLine()

            For Each stmt In runResult.Statements
                sb.AppendLine(FormatStatementResult(stmt))
            Next

            sb.AppendLine("=== END OF SPUFI REPORT ===")
            Return sb.ToString()
        End Function

        Public Shared Function FormatStatementResult(stmt As SpufiStatementResult) As String
            Dim sb As New StringBuilder()

            sb.AppendLine(RulerLine)
            sb.AppendLine(stmt.SqlText)
            sb.AppendLine(RulerLine)

            If stmt.IsSuccess Then
                If stmt.DataTable IsNot Nothing Then
                    sb.Append(FormatDataTable(stmt.DataTable))
                    sb.AppendLine($"DSNE610I NUMBER OF ROWS DISPLAYED IS {stmt.RowsReturned}")
                Else
                    If stmt.RowsAffected >= 0 Then
                        sb.AppendLine($"DSNE615I NUMBER OF ROWS AFFECTED IS {stmt.RowsAffected}")
                    End If
                End If
                sb.AppendLine($"DSNE616I STATEMENT EXECUTION WAS SUCCESSFUL, SQLCODE IS {stmt.SqlCode} (SQLSTATE {stmt.SqlState})")
                sb.AppendLine($"Execution Time: {stmt.ExecutionDuration.TotalMilliseconds:F1} ms")
            Else
                sb.AppendLine($"DSNT408I SQLCODE = {stmt.SqlCode}, ERROR: {stmt.ErrorMessage}")
                sb.AppendLine($"DSNT418I SQLSTATE = {stmt.SqlState}")
                sb.AppendLine($"Execution Time: {stmt.ExecutionDuration.TotalMilliseconds:F1} ms")
            End If

            sb.AppendLine()
            Return sb.ToString()
        End Function

        Public Shared Function FormatDataTable(dt As DataTable) As String
            If dt Is Nothing OrElse dt.Columns.Count = 0 Then Return ""

            Dim sb As New StringBuilder()
            Dim colCount = dt.Columns.Count
            Dim colWidths(colCount - 1) As Integer

            ' Calculate widths
            For c As Integer = 0 To colCount - 1
                colWidths(c) = Math.Max(colWidths(c), dt.Columns(c).ColumnName.Length)
            Next

            For Each row As DataRow In dt.Rows
                For c As Integer = 0 To colCount - 1
                    Dim val = If(row.IsNull(c), "NULL", row(c).ToString())
                    colWidths(c) = Math.Max(colWidths(c), val.Length)
                Next
            Next

            ' Limit maximum column display width to keep it reasonable
            For c As Integer = 0 To colCount - 1
                colWidths(c) = Math.Min(colWidths(c), 60)
            Next

            ' Print headers
            Dim headerCols As New List(Of String)()
            Dim underlineCols As New List(Of String)()
            For c As Integer = 0 To colCount - 1
                Dim colName = dt.Columns(c).ColumnName
                If colName.Length > colWidths(c) Then colName = colName.Substring(0, colWidths(c))
                headerCols.Add(colName.PadRight(colWidths(c)))
                underlineCols.Add(New String("-"c, colWidths(c)))
            Next
            sb.AppendLine(String.Join("  ", headerCols))
            sb.AppendLine(String.Join("  ", underlineCols))

            ' Print rows
            For Each row As DataRow In dt.Rows
                Dim rowCols As New List(Of String)()
                For c As Integer = 0 To colCount - 1
                    Dim val = If(row.IsNull(c), "NULL", row(c).ToString())
                    If val.Length > colWidths(c) Then val = val.Substring(0, colWidths(c))
                    rowCols.Add(val.PadRight(colWidths(c)))
                Next
                sb.AppendLine(String.Join("  ", rowCols))
            Next

            Return sb.ToString()
        End Function

    End Class

End Namespace
