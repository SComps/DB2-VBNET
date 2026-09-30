Namespace Execution

    Public Enum SpufiCommitMode
        AutoCommitPerStatement
        CommitOnCompletion
        Manual
    End Enum

    Public Enum SpufiOnErrorAction
        HaltOnError
        ContinueOnError
    End Enum

    Public Class SpufiOptions
        Public Property CommitMode As SpufiCommitMode = SpufiCommitMode.AutoCommitPerStatement
        Public Property OnError As SpufiOnErrorAction = SpufiOnErrorAction.HaltOnError
        Public Property MaxRows As Integer = 1000
        Public Property StatementDelimiter As String = ";"
        Public Property EchoInputSql As Boolean = True
    End Class

End Namespace
