Imports IBM.Data.Db2

Namespace Db

    Public Class Project
        Public Property PName     As String
        Public Property PFile     As String
        Public Property PRuntime  As Integer
        Public Property PCostMin  As Decimal
        Public Property PMarkup   As Decimal
        Public Property PFil      As String
        Public Property PGrams    As Decimal
    End Class

    Public Module ProjectRepo

        Public Function GetAll() As List(Of Project)
            Dim list As New List(Of Project)
            Using conn = Database.OpenConnection()
                Using cmd As New DB2Command("SELECT PNAME,PFILE,PRUNTIME,PCOSTMIN,PMARKUP,PFIL,PGRAMS FROM SCOTT.PROJECTS ORDER BY PNAME", conn)
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            list.Add(New Project With {
                                .PName    = r.GetString(0).Trim(),
                                .PFile    = If(r.IsDBNull(1), "", r.GetString(1).Trim()),
                                .PRuntime = If(r.IsDBNull(2), 0, r.GetInt32(2)),
                                .PCostMin = If(r.IsDBNull(3), 0, r.GetDecimal(3)),
                                .PMarkup  = If(r.IsDBNull(4), 0, r.GetDecimal(4)),
                                .PFil     = If(r.IsDBNull(5), "", r.GetString(5).Trim()),
                                .PGrams   = If(r.IsDBNull(6), 0, r.GetDecimal(6))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Sub Upsert(proj As Project, isNew As Boolean)
            Using conn = Database.OpenConnection()
                Dim sql As String
                If isNew Then
                    sql = "INSERT INTO SCOTT.PROJECTS(PNAME,PFILE,PRUNTIME,PCOSTMIN,PMARKUP,PFIL,PGRAMS) VALUES(?,?,?,?,?,?,?)"
                Else
                    sql = "UPDATE SCOTT.PROJECTS SET PFILE=?,PRUNTIME=?,PCOSTMIN=?,PMARKUP=?,PFIL=?,PGRAMS=? WHERE PNAME=?"
                End If
                Using cmd As New DB2Command(sql, conn)
                    If isNew Then
                        cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PName})
                    End If
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PFile})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PRuntime})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PCostMin})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PMarkup})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PFil})
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PGrams})
                    If Not isNew Then
                        cmd.Parameters.Add(New DB2Parameter() With {.Value = proj.PName})
                    End If
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub Delete(pName As String)
            Using conn = Database.OpenConnection()
                Using cmd As New DB2Command("DELETE FROM SCOTT.PROJECTS WHERE PNAME=?", conn)
                    cmd.Parameters.Add(New DB2Parameter() With {.Value = pName})
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

    End Module

End Namespace
