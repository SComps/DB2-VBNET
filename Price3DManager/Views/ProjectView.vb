Imports Terminal.Gui
Imports TDim = Terminal.Gui.Dim
Imports Price3DManager.Db
Imports System.Collections.ObjectModel

Namespace Views

    Public Class ProjectView
        Inherits Window

        Private _list  As ListView
        Private _items As List(Of Project)

        Public Sub New()
            MyBase.New("Projects (PROJECTS)")
            X = 0 : Y = 1
            Width = TDim.Fill() : Height = TDim.Fill()
            LoadData()
            BuildUi()
        End Sub

        Private Sub LoadData()
            _items = ProjectRepo.GetAll()
        End Sub

        Private Function ToOC() As ObservableCollection(Of String)
            Dim oc As New ObservableCollection(Of String)
            For Each p In _items
                oc.Add($"{p.PName,-20} {p.PFile,-10} {p.PRuntime,5}min  Fil:{p.PFil,-8} {p.PGrams,6:F1}g")
            Next
            Return oc
        End Function

        Private Sub BuildUi()
            Add(New Label($"{"Name",-20} {"File",-10} {"Runtime",9}  {"Filament",-12} {"Grams",8}") With {.X = 1, .Y = 0})

            _list = New ListView(ToOC()) With {
                .X = 1, .Y = 1,
                .Width = TDim.Fill(1),
                .Height = TDim.Fill(4)
            }
            AddHandler _list.OpenSelectedItem, AddressOf OnListEnter
            Add(_list)

            Dim btnAdd  As New Button("_Add")    With {.X = 1,                     .Y = Pos.AnchorEnd(1)}
            Dim btnEdit As New Button("_Edit")   With {.X = Pos.Right(btnAdd) + 1,  .Y = Pos.AnchorEnd(1)}
            Dim btnDel  As New Button("_Delete") With {.X = Pos.Right(btnEdit) + 1, .Y = Pos.AnchorEnd(1)}
            Dim btnBack As New Button("_Back")   With {.X = Pos.Right(btnDel) + 1,  .Y = Pos.AnchorEnd(1)}

            AddHandler btnAdd.Clicked,  AddressOf OnAdd
            AddHandler btnEdit.Clicked, AddressOf OnEdit
            AddHandler btnDel.Clicked,  AddressOf OnDelete
            AddHandler btnBack.Clicked, Sub() Application.RequestStop()

            Add(btnAdd, btnEdit, btnDel, btnBack)
        End Sub

        Private Sub OnListEnter(e As ListViewItemEventArgs)
            OnEdit()
        End Sub

        Private Sub OnAdd()
            Dim dlg As New ProjectEditDialog(New Project(), True)
            Application.Run(dlg)
            If dlg.Saved Then SaveAndRefresh(dlg.Item, True)
        End Sub

        Private Sub OnEdit()
            If _items.Count = 0 OrElse _list.SelectedItem < 0 Then Return
            Dim dlg As New ProjectEditDialog(_items(_list.SelectedItem), False)
            Application.Run(dlg)
            If dlg.Saved Then SaveAndRefresh(dlg.Item, False)
        End Sub

        Private Sub OnDelete()
            If _items.Count = 0 OrElse _list.SelectedItem < 0 Then Return
            Dim p = _items(_list.SelectedItem)
            If MessageBox.Query(50, 7, "Confirm", $"Delete project '{p.PName}'?", "Yes", "No") = 0 Then
                Try
                    ProjectRepo.Delete(p.PName)
                    LoadData() : _list.SetSource(ToOC())
                Catch ex As Exception
                    MessageBox.ErrorQuery(60, 7, "Error", ex.Message, "OK")
                End Try
            End If
        End Sub

        Private Sub SaveAndRefresh(proj As Project, isNew As Boolean)
            Try
                ProjectRepo.Upsert(proj, isNew)
                LoadData() : _list.SetSource(ToOC())
            Catch ex As Exception
                MessageBox.ErrorQuery(60, 7, "Save Error", ex.Message, "OK")
            End Try
        End Sub

    End Class

    Public Class ProjectEditDialog
        Inherits Dialog

        Private _item  As Project
        Private _saved As Boolean

        Public ReadOnly Property Item As Project
            Get
                Return _item
            End Get
        End Property
        Public ReadOnly Property Saved As Boolean
            Get
                Return _saved
            End Get
        End Property

        Private _fName, _fFile, _fRuntime, _fCostMin, _fMarkup, _fFil, _fGrams As TextField
        Private _isNew As Boolean

        Public Sub New(item As Project, isNew As Boolean)
            MyBase.New(If(isNew, "Add Project", "Edit Project"), 55, 14)
            _item  = item
            _isNew = isNew
            BuildUi()
        End Sub

        Private Sub BuildUi()
            Dim labels = {"Project Name:", "File:", "Runtime (min):", "Cost/Min:", "Markup %:", "Filament ID:", "Grams:"}
            Dim values = {If(_item.PName, ""), If(_item.PFile, ""), _item.PRuntime.ToString(),
                          _item.PCostMin.ToString(), _item.PMarkup.ToString(),
                          If(_item.PFil, ""), _item.PGrams.ToString()}

            Dim fields As New List(Of TextField)
            For i = 0 To labels.Length - 1
                Add(New Label(labels(i)) With {.X = 1, .Y = i})
                Dim tf As New TextField(values(i)) With {
                    .X = 16, .Y = i, .Width = 32,
                    .ReadOnly = (i = 0 AndAlso Not _isNew)
                }
                Add(tf)
                fields.Add(tf)
            Next

            _fName    = fields(0) : _fFile    = fields(1) : _fRuntime = fields(2)
            _fCostMin = fields(3) : _fMarkup  = fields(4) : _fFil     = fields(5)
            _fGrams   = fields(6)

            Dim btnSave   As New Button("_Save")
            Dim btnCancel As New Button("_Cancel")
            AddHandler btnSave.Clicked, Sub()
                _item.PName    = _fName.Text.ToString()
                _item.PFile    = _fFile.Text.ToString()
                _item.PRuntime = ParseInt(_fRuntime.Text.ToString())
                _item.PCostMin = ParseDec(_fCostMin.Text.ToString())
                _item.PMarkup  = ParseDec(_fMarkup.Text.ToString())
                _item.PFil     = _fFil.Text.ToString()
                _item.PGrams   = ParseDec(_fGrams.Text.ToString())
                _saved = True
                Application.RequestStop()
            End Sub
            AddHandler btnCancel.Clicked, Sub() Application.RequestStop()
            AddButton(btnSave)
            AddButton(btnCancel)
        End Sub

        Private Function ParseDec(s As String) As Decimal
            Dim v As Decimal : Decimal.TryParse(s, v) : Return v
        End Function
        Private Function ParseInt(s As String) As Integer
            Dim v As Integer : Integer.TryParse(s, v) : Return v
        End Function

    End Class

End Namespace
