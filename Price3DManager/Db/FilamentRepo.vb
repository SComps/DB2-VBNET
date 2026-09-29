Imports IBM.Data.Db2

Namespace Db

    Public Class FilamentItem
        Public Property ItemId    As String
        Public Property ItType    As String
        Public Property ItColor   As String
        Public Property ItWeigh   As Decimal
        Public Property ItUsed    As Decimal
        Public Property ItRemain  As Decimal
        Public Property ItPurch   As Decimal
        Public Property ItVendor  As String
        Public Property ItVendorId As String
        Public Property ItVReorder As String
        Public Property ItLastDt  As String  ' stored as VARCHAR on z/OS ADCD
    End Class

    Public Module FilamentRepo

        Public Function GetAll() As List(Of FilamentItem)
            Dim list As New List(Of FilamentItem)
            Using conn = Database.OpenConnection()
                Using cmd As New DB2Command("SELECT ITEMID,ITTYPE,ITCOLOR,ITWEIGH,ITUSED,ITREMAIN,ITPURCH,ITVENDOR,ITVENDORID,ITVREORDER,ITLASTDT FROM SCOTT.FILMNT ORDER BY ITEMID", conn)
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            list.Add(New FilamentItem With {
                                .ItemId     = r.GetString(0).Trim(),
                                .ItType     = If(r.IsDBNull(1), "", r.GetString(1).Trim()),
                                .ItColor    = If(r.IsDBNull(2), "", r.GetString(2).Trim()),
                                .ItWeigh    = If(r.IsDBNull(3), 0D, Convert.ToDecimal(r.GetValue(3))),
                                .ItUsed     = If(r.IsDBNull(4), 0D, Convert.ToDecimal(r.GetValue(4))),
                                .ItRemain   = If(r.IsDBNull(5), 0D, Convert.ToDecimal(r.GetValue(5))),
                                .ItPurch    = If(r.IsDBNull(6), 0D, Convert.ToDecimal(r.GetValue(6))),
                                .ItVendor   = If(r.IsDBNull(7), "", r.GetString(7).Trim()),
                                .ItVendorId = If(r.IsDBNull(8), "", r.GetString(8).Trim()),
                                .ItVReorder = If(r.IsDBNull(9), "", r.GetString(9).Trim()),
                                .ItLastDt   = If(r.IsDBNull(10), "", r.GetValue(10).ToString().Trim())
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub Upsert(item As FilamentItem, isNew As Boolean)
            Using conn = Database.OpenConnection()
                Dim sql As String
                If isNew Then
                    sql = "INSERT INTO SCOTT.FILMNT(ITEMID,ITTYPE,ITCOLOR,ITWEIGH,ITUSED,ITREMAIN,ITPURCH,ITVENDOR,ITVENDORID,ITVREORDER,ITLASTDT) VALUES(?,?,?,?,?,?,?,?,?,?,?)"
                Else
                    sql = "UPDATE SCOTT.FILMNT SET ITTYPE=?,ITCOLOR=?,ITWEIGH=?,ITUSED=?,ITREMAIN=?,ITPURCH=?,ITVENDOR=?,ITVENDORID=?,ITVREORDER=?,ITLASTDT=? WHERE ITEMID=?"
                End If
                Using cmd As New DB2Command(sql, conn)
                    If isNew Then
                        cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItemId})
                    End If
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItType})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItColor})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItWeigh})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItUsed})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItRemain})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItPurch})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItVendor})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItVendorId})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItVReorder})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItLastDt})
                    If Not isNew Then
                        cmd.Parameters.Add(New DB2Parameter() With {.Value = item.ItemId})
                    End If
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub Delete(itemId As String)
            Using conn = Database.OpenConnection()
                Using cmd As New DB2Command("DELETE FROM SCOTT.FILMNT WHERE ITEMID=?", conn)
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = itemId})
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

    End Module

End Namespace
