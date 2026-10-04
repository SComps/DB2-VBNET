Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports IBM.Data.Db2
Imports Db2Spufi.Core.Models

Namespace Linq

    Public Class Db2DataContext
        Implements IDisposable

        Private ReadOnly _connectionString As String
        Private ReadOnly _profile As ConnectionProfile

        Public Sub New(profile As ConnectionProfile)
            If profile Is Nothing Then Throw New ArgumentNullException(NameOf(profile))
            _profile = profile
            _connectionString = profile.BuildConnectionString()
        End Sub

        Public Sub New(connectionString As String)
            If String.IsNullOrWhiteSpace(connectionString) Then Throw New ArgumentNullException(NameOf(connectionString))
            _connectionString = connectionString
        End Sub

        Public ReadOnly Property Profile As ConnectionProfile
            Get
                Return _profile
            End Get
        End Property

        Public ReadOnly Property ConnectionString As String
            Get
                Return _connectionString
            End Get
        End Property

        Public Function Query(Of T As {Class, New})(sql As String, ParamArray parameters As Object()) As List(Of T)
            Return QueryAsync(Of T)(sql, CancellationToken.None, parameters).GetAwaiter().GetResult()
        End Function

        Public Function AsQueryable(Of T As {Class, New})(sql As String, ParamArray parameters As Object()) As IQueryable(Of T)
            Return Query(Of T)(sql, parameters).AsQueryable()
        End Function

        Public Function GetTable(Of T As {Class, New})(tableName As String) As IQueryable(Of T)
            Dim sql = $"SELECT * FROM {tableName}"
            Return AsQueryable(Of T)(sql)
        End Function

        Public Async Function QueryAsync(Of T As {Class, New})(sql As String,
                                                               cancellationToken As CancellationToken,
                                                               ParamArray parameters As Object()) As Task(Of List(Of T))
            Dim dt As New DataTable()

            Using conn As New DB2Connection(_connectionString)
                Await conn.OpenAsync(cancellationToken)
                Using cmd As DB2Command = conn.CreateCommand()
                    cmd.CommandText = sql
                    AddParameters(cmd, parameters)

                    Using reader As DB2DataReader = DirectCast(Await cmd.ExecuteReaderAsync(cancellationToken), DB2DataReader)
                        dt.Load(reader)
                    End Using
                End Using
            End Using

            Return dt.ToEntities(Of T)()
        End Function

        Public Async Function ExecuteNonQueryAsync(sql As String,
                                                   cancellationToken As CancellationToken,
                                                   ParamArray parameters As Object()) As Task(Of Integer)
            Using conn As New DB2Connection(_connectionString)
                Await conn.OpenAsync(cancellationToken)
                Using cmd As DB2Command = conn.CreateCommand()
                    cmd.CommandText = sql
                    AddParameters(cmd, parameters)
                    Return Await cmd.ExecuteNonQueryAsync(cancellationToken)
                End Using
            End Using
        End Function

        Public Async Function ExecuteScalarAsync(Of T)(sql As String,
                                                       cancellationToken As CancellationToken,
                                                       ParamArray parameters As Object()) As Task(Of T)
            Using conn As New DB2Connection(_connectionString)
                Await conn.OpenAsync(cancellationToken)
                Using cmd As DB2Command = conn.CreateCommand()
                    cmd.CommandText = sql
                    AddParameters(cmd, parameters)
                    Dim raw As Object = Await cmd.ExecuteScalarAsync(cancellationToken)
                    If raw Is Nothing OrElse Convert.IsDBNull(raw) Then
                        Return Nothing
                    End If
                    Return CType(Convert.ChangeType(raw, GetType(T)), T)
                End Using
            End Using
        End Function

        Private Sub AddParameters(cmd As DB2Command, parameters As Object())
            If parameters Is Nothing OrElse parameters.Length = 0 Then Return
            For i As Integer = 0 To parameters.Length - 1
                Dim pVal = parameters(i)
                Dim pName = $"@p{i}"
                Dim dbParam = cmd.CreateParameter()
                dbParam.ParameterName = pName
                dbParam.Value = If(pVal, DBNull.Value)
                cmd.Parameters.Add(dbParam)
            Next
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Connection cleanup handled per operation
        End Sub
    End Class

End Namespace
