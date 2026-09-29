Imports IBM.Data.Db2

Namespace Db

    Public Class Customer
        Public Property CustId  As String
        Public Property CName   As String
        Public Property Addr1   As String
        Public Property Addr2   As String
        Public Property City    As String
        Public Property State   As String
        Public Property Zip     As String
        Public Property Phone   As String
        Public Property Email   As String
    End Class

    Public Module CustomerRepo

        Public Function GetAll() As List(Of Customer)
            Dim list As New List(Of Customer)
            Using conn = Database.OpenConnection()
                Using cmd As New DB2Command("SELECT CUSTID,CNAME,ADDR1,ADDR2,CITY,STATE,ZIP,PHONE,EMAIL FROM SCOTT.P3DCUST ORDER BY CUSTID", conn)
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            list.Add(New Customer With {
                                .CustId = r.GetString(0).Trim(),
                                .CName  = If(r.IsDBNull(1), "", r.GetString(1).Trim()),
                                .Addr1  = If(r.IsDBNull(2), "", r.GetString(2).Trim()),
                                .Addr2  = If(r.IsDBNull(3), "", r.GetString(3).Trim()),
                                .City   = If(r.IsDBNull(4), "", r.GetString(4).Trim()),
                                .State  = If(r.IsDBNull(5), "", r.GetString(5).Trim()),
                                .Zip    = If(r.IsDBNull(6), "", r.GetString(6).Trim()),
                                .Phone  = If(r.IsDBNull(7), "", r.GetString(7).Trim()),
                                .Email  = If(r.IsDBNull(8), "", r.GetString(8).Trim())
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub Upsert(cust As Customer, isNew As Boolean)
            Using conn = Database.OpenConnection()
                Dim sql As String
                If isNew Then
                    sql = "INSERT INTO SCOTT.P3DCUST(CUSTID,CNAME,ADDR1,ADDR2,CITY,STATE,ZIP,PHONE,EMAIL) VALUES(?,?,?,?,?,?,?,?,?)"
                Else
                    sql = "UPDATE SCOTT.P3DCUST SET CNAME=?,ADDR1=?,ADDR2=?,CITY=?,STATE=?,ZIP=?,PHONE=?,EMAIL=? WHERE CUSTID=?"
                End If
                Using cmd As New DB2Command(sql, conn)
                    If isNew Then
                        cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.CustId})
                    End If
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.CName})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.Addr1})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = If(cust.Addr2 = "", DBNull.Value, cust.Addr2)})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.City})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.State})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.Zip})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = If(cust.Phone = "", DBNull.Value, cust.Phone)})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = If(cust.Email = "", DBNull.Value, cust.Email)})
                    If Not isNew Then
                        cmd.Parameters.Add(New DB2Parameter() With {.Value = cust.CustId})
                    End If
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub Delete(custId As String)
            Using conn = Database.OpenConnection()
                Using cmd As New DB2Command("DELETE FROM SCOTT.P3DCUST WHERE CUSTID=?", conn)
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = custId})
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

    End Module

End Namespace
