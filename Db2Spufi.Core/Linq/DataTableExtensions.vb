Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Db2Spufi.Core.Execution

Namespace Linq

    Public Module DataTableExtensions

        <Extension()>
        Public Function ToEntities(Of T As {Class, New})(table As DataTable) As List(Of T)
            Dim list As New List(Of T)()
            If table Is Nothing OrElse table.Rows.Count = 0 Then
                Return list
            End If

            Dim targetType As Type = GetType(T)
            Dim props As PropertyInfo() = targetType.GetProperties(BindingFlags.Public Or BindingFlags.Instance)
            Dim propMap As New Dictionary(Of String, PropertyInfo)(StringComparer.OrdinalIgnoreCase)

            For Each prop In props
                If prop.CanWrite Then
                    propMap(prop.Name) = prop
                End If
            Next

            For Each row As DataRow In table.Rows
                Dim entity As T = Activator.CreateInstance(Of T)()
                For Each col As DataColumn In table.Columns
                    Dim prop As PropertyInfo = Nothing
                    If propMap.TryGetValue(col.ColumnName, prop) Then
                        Dim cellValue As Object = row(col)
                        SetPropertyValue(entity, prop, cellValue)
                    End If
                Next
                list.Add(entity)
            Next

            Return list
        End Function

        <Extension()>
        Public Function AsEnumerable(Of T As {Class, New})(table As DataTable) As IEnumerable(Of T)
            Return ToEntities(Of T)(table)
        End Function

        <Extension()>
        Public Function AsQueryable(Of T As {Class, New})(table As DataTable) As IQueryable(Of T)
            Return ToEntities(Of T)(table).AsQueryable()
        End Function

        <Extension()>
        Public Function AsQueryable(Of T As {Class, New})(stmtResult As SpufiStatementResult) As IQueryable(Of T)
            If stmtResult Is Nothing OrElse stmtResult.DataTable Is Nothing Then
                Return New List(Of T)().AsQueryable()
            End If
            Return ToEntities(Of T)(stmtResult.DataTable).AsQueryable()
        End Function

        Private Sub SetPropertyValue(entity As Object, prop As PropertyInfo, value As Object)
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return
            End If

            Try
                Dim targetType As Type = prop.PropertyType
                Dim underlyingType As Type = Nullable.GetUnderlyingType(targetType)
                If underlyingType IsNot Nothing Then
                    targetType = underlyingType
                End If

                Dim convertedValue As Object = Nothing

                If targetType.IsEnum Then
                    If TypeOf value Is String Then
                        convertedValue = [Enum].Parse(targetType, CStr(value), True)
                    Else
                        convertedValue = [Enum].ToObject(targetType, value)
                    End If
                ElseIf targetType = GetType(Guid) Then
                    If TypeOf value Is String Then
                        convertedValue = Guid.Parse(CStr(value))
                    ElseIf TypeOf value Is Byte() Then
                        convertedValue = New Guid(DirectCast(value, Byte()))
                    End If
                ElseIf targetType = GetType(Boolean) Then
                    If TypeOf value Is String Then
                        Dim s As String = CStr(value).Trim()
                        If s = "1" OrElse s.Equals("Y", StringComparison.OrdinalIgnoreCase) OrElse s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) Then
                            convertedValue = True
                        Else
                            convertedValue = False
                        End If
                    Else
                        convertedValue = Convert.ToBoolean(value)
                    End If
                Else
                    convertedValue = Convert.ChangeType(value, targetType)
                End If

                prop.SetValue(entity, convertedValue, Nothing)
            Catch ex As Exception
                ' Ignore non-critical property conversion mismatches
            End Try
        End Sub

    End Module

End Namespace
