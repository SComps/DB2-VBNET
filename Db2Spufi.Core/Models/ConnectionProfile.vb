Imports System.Text.Json.Serialization

Namespace Models

    Public Class ConnectionProfile
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property Name As String = "New Connection"
        Public Property Server As String = "10.10.13.2"
        Public Property Port As Integer = 8103
        Public Property Database As String = "DBD1LOC"
        Public Property User As String = "SCOTT"
        Public Property Password As String = "mlkhbu"
        Public Property CurrentSqlId As String = ""
        Public Property IsDefault As Boolean = False

        Public Function BuildConnectionString() As String
            Dim connStr As String = $"Server={Server}:{Port};Database={Database};UID={User};PWD={Password};"
            If Not String.IsNullOrWhiteSpace(CurrentSqlId) Then
                connStr &= $"CurrentSQLID={CurrentSqlId};"
            End If
            Return connStr
        End Function

        Public Function Clone() As ConnectionProfile
            Return New ConnectionProfile() With {
                .Id = Guid.NewGuid().ToString("N"),
                .Name = Me.Name & " (Copy)",
                .Server = Me.Server,
                .Port = Me.Port,
                .Database = Me.Database,
                .User = Me.User,
                .Password = Me.Password,
                .CurrentSqlId = Me.CurrentSqlId,
                .IsDefault = False
            }
        End Function

        Public Overrides Function ToString() As String
            Return $"{Name} ({Database} @ {Server}:{Port})"
        End Function
    End Class

End Namespace
