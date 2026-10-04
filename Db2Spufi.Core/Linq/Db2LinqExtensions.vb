Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Db2Spufi.Core.Models

Namespace Linq

    Public Module Db2LinqExtensions

        <Extension()>
        Public Function CreateDataContext(profile As ConnectionProfile) As Db2DataContext
            Return New Db2DataContext(profile)
        End Function

        <Extension()>
        Public Function ExecuteQuery(Of T As {Class, New})(profile As ConnectionProfile, sql As String, ParamArray parameters As Object()) As List(Of T)
            Using ctx As New Db2DataContext(profile)
                Return ctx.Query(Of T)(sql, parameters)
            End Using
        End Function

        <Extension()>
        Public Function ExecuteQueryAsync(Of T As {Class, New})(profile As ConnectionProfile, sql As String, cancellationToken As CancellationToken, ParamArray parameters As Object()) As Task(Of List(Of T))
            Using ctx As New Db2DataContext(profile)
                Return ctx.QueryAsync(Of T)(sql, cancellationToken, parameters)
            End Using
        End Function

        <Extension()>
        Public Function AsQueryable(Of T As {Class, New})(profile As ConnectionProfile, sql As String) As IQueryable(Of T)
            Using ctx As New Db2DataContext(profile)
                Return ctx.AsQueryable(Of T)(sql)
            End Using
        End Function

    End Module

End Namespace
