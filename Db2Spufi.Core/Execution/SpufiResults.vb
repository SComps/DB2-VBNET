Imports System.Data

Namespace Execution

    Public Enum SpufiStatementType
        SelectQuery
        NonQuery
        Commit
        Rollback
    End Enum

    Public Class SpufiStatementResult
        Public Property StatementIndex As Integer
        Public Property SqlText As String = ""
        Public Property StatementType As SpufiStatementType = SpufiStatementType.NonQuery
        Public Property IsSuccess As Boolean
        Public Property SqlCode As Integer = 0
        Public Property SqlState As String = "00000"
        Public Property RowsAffected As Integer = 0
        Public Property RowsReturned As Integer = 0
        Public Property DataTable As DataTable = Nothing
        Public Property ExecutionDuration As TimeSpan = TimeSpan.Zero
        Public Property ErrorMessage As String = ""
    End Class

    Public Class SpufiRunResult
        Public Property Statements As New List(Of SpufiStatementResult)()
        Public Property TotalDuration As TimeSpan = TimeSpan.Zero
        Public Property HasErrors As Boolean = False
        Public Property FormattedLog As String = ""
    End Class

    Public Class SpufiProgressUpdate
        Public Property CurrentStatementIndex As Integer
        Public Property TotalStatements As Integer
        Public Property CurrentSqlSnippet As String
        Public Property StatusMessage As String
    End Class

End Namespace
