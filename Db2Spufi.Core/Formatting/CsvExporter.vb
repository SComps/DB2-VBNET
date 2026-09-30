Imports System.Data
Imports System.IO
Imports System.Text

Namespace Formatting

    Public Class CsvExporter

        Public Shared Function ExportToCsvString(dt As DataTable) As String
            If dt Is Nothing Then Return ""

            Dim sb As New StringBuilder()

            ' Headers
            Dim headers As New List(Of String)()
            For Each col As DataColumn In dt.Columns
                headers.Add(EscapeCsvField(col.ColumnName))
            Next
            sb.AppendLine(String.Join(",", headers))

            ' Rows
            For Each row As DataRow In dt.Rows
                Dim fields As New List(Of String)()
                For c As Integer = 0 To dt.Columns.Count - 1
                    Dim val = If(row.IsNull(c), "", row(c).ToString())
                    fields.Add(EscapeCsvField(val))
                Next
                sb.AppendLine(String.Join(",", fields))
            Next

            Return sb.ToString()
        End Function

        Public Shared Sub ExportToCsvFile(dt As DataTable, filePath As String)
            Dim csv = ExportToCsvString(dt)
            File.WriteAllText(filePath, csv, Encoding.UTF8)
        End Sub

        Private Shared Function EscapeCsvField(field As String) As String
            If field Is Nothing Then Return ""
            Dim mustQuote = field.Contains(","c) OrElse field.Contains(""""c) OrElse field.Contains(vbCr) OrElse field.Contains(vbLf)
            If field.Contains(""""c) Then
                field = field.Replace("""", """""")
            End If
            If mustQuote Then
                Return $"""{field}"""
            End If
            Return field
        End Function

    End Class

End Namespace
