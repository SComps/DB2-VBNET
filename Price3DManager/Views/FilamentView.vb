Imports Terminal.Gui
Imports TDim = Terminal.Gui.Dim
Imports Price3DManager.Db
Imports System.Collections.ObjectModel

Namespace Views

    Public Class FilamentView
        Inherits Window

        Private _list  As ListView
        Private _items As List(Of FilamentItem)

        Public Sub New()
            MyBase.New("Filament Inventory (FILMNT)")
            X = 0 : Y = 1
            Width = TDim.Fill() : Height = TDim.Fill()
            LoadData()
            BuildUi()
        End Sub

        Private Sub LoadData()
            _items = FilamentRepo.GetAll()
        End Sub

        Private Function ToOC() As ObservableCollection(Of String)
            Dim oc As New ObservableCollection(Of String)
            For Each i In _items
                oc.Add($"{i.ItemId,-10} {i.ItType,-6} {i.ItColor,-10} Wt:{i.ItWeigh,6:F1}  Rem:{i.ItRemain,6:F1}")
            Next
            Return oc
        End Function

        Private Sub BuildUi()
            Dim header As New Label($"{"ID",-10} {"Type",-6} {"Color",-10} {"Weight",9}  {"Remain",9}") With {.X = 1, .Y = 0}
            Add(header)

            _list = New ListView(ToOC()) With {
                .X = 1, .Y = 1,
                .Width = TDim.Fill(1),
                .Height = TDim.Fill(4)
            }
            Add(_list)

            Dim btnAdd  As New Button("_Add")    With {.X = 1,                    .Y = Pos.AnchorEnd(1)}
            Dim btnEdit As New Button("_Edit")   With {.X = Pos.Right(btnAdd) + 1, .Y = Pos.AnchorEnd(1)}
            Dim btnDel  As New Button("_Delete") With {.X = Pos.Right(btnEdit) + 1,.Y = Pos.AnchorEnd(1)}
            Dim btnBack As New Button("_Back")   With {.X = Pos.Right(btnDel) + 1, .Y = Pos.AnchorEnd(1)}

            AddHandler btnAdd.Clicked,  AddressOf OnAdd
            AddHandler btnEdit.Clicked, AddressOf OnEdit
            AddHandler btnDel.Clicked,  AddressOf OnDelete
            AddHandler btnBack.Clicked, AddressOf OnBack

            Add(btnAdd, btnEdit, btnDel, btnBack)
        End Sub

        Private Sub OnAdd()
            Dim dlg As New FilamentEditDialog(New FilamentItem(), True)
            Application.Run(dlg)
            If dlg.Saved Then
                Try
                    FilamentRepo.Upsert(dlg.Item, True)
                    LoadData()
                    _list.SetSource(ToOC())
                Catch ex As Exception
                    MessageBox.ErrorQuery(60, 7, "Save Error", ex.Message, "OK")
                End Try
            End If
        End Sub

        Private Sub OnEdit()
            If _items.Count = 0 Then Return
            Dim idx = _list.SelectedItem
            If idx < 0 OrElse idx >= _items.Count Then Return
            Dim dlg As New FilamentEditDialog(_items(idx), False)
            Application.Run(dlg)
            If dlg.Saved Then
                Try
                    FilamentRepo.Upsert(dlg.Item, False)
                    LoadData()
                    _list.SetSource(ToOC())
                Catch ex As Exception
                    MessageBox.ErrorQuery(60, 7, "Save Error", ex.Message, "OK")
                End Try
            End If
        End Sub

        Private Sub OnDelete()
            If _items.Count = 0 Then Return
            Dim idx = _list.SelectedItem
            If idx < 0 OrElse idx >= _items.Count Then Return
            Dim item = _items(idx)
            Dim res = MessageBox.Query(50, 7, "Confirm", $"Delete filament '{item.ItemId}'?", "Yes", "No")
            If res = 0 Then
                Try
                    FilamentRepo.Delete(item.ItemId)
                    LoadData()
                    _list.SetSource(ToOC())
                Catch ex As Exception
                    MessageBox.ErrorQuery(60, 7, "Delete Error", ex.Message, "OK")
                End Try
            End If
        End Sub

        Private Sub OnBack()
            Application.RequestStop()
        End Sub

    End Class

    Public Class FilamentEditDialog
        Inherits Dialog

        Public ReadOnly Property Item  As FilamentItem
        Public ReadOnly Property Saved As Boolean

        Private _fItemId, _fType, _fColor, _fWeigh, _fUsed, _fRemain, _fPurch,
                _fVendor, _fVendorId, _fVReorder, _fLastDt As TextField
        Private _isNew As Boolean

        Public Sub New(item As FilamentItem, isNew As Boolean)
            MyBase.New(If(isNew, "Add Filament", "Edit Filament"), 62, 18)
            Item   = item
            _isNew = isNew
            BuildUi()
        End Sub

        Private Sub BuildUi()
            Dim labels = {"Item ID:", "Type:", "Color:", "Weight (g):", "Used (g):", "Remaining (g):",
                          "Purchased (g):", "Vendor:", "Vendor ID:", "Reorder?:", "Last Date:"}
            Dim values = {If(Item.ItemId, ""), If(Item.ItType, ""), If(Item.ItColor, ""),
                          Item.ItWeigh.ToString(), Item.ItUsed.ToString(), Item.ItRemain.ToString(),
                          Item.ItPurch.ToString(), If(Item.ItVendor, ""), If(Item.ItVendorId, ""),
                          If(Item.ItVReorder, ""), If(Item.ItLastDt, "")}

            Dim fields As New List(Of TextField)
            For i = 0 To labels.Length - 1
                Add(New Label(labels(i)) With {.X = 1, .Y = i})
                Dim tf As New TextField(values(i)) With {
                    .X = 16, .Y = i, .Width = 40,
                    .ReadOnly = (i = 0 AndAlso Not _isNew)
                }
                Add(tf)
                fields.Add(tf)
            Next

            _fItemId = fields(0)   : _fType     = fields(1)  : _fColor    = fields(2)
            _fWeigh  = fields(3)   : _fUsed      = fields(4)  : _fRemain   = fields(5)
            _fPurch  = fields(6)   : _fVendor    = fields(7)  : _fVendorId = fields(8)
            _fVReorder = fields(9) : _fLastDt    = fields(10)

            Dim btnSave   As New Button("_Save")
            Dim btnCancel As New Button("_Cancel")
            AddHandler btnSave.Clicked,   AddressOf OnSave
            AddHandler btnCancel.Clicked, Sub() Application.RequestStop()
            AddButton(btnSave)
            AddButton(btnCancel)
        End Sub

        Private Sub OnSave()
            Item.ItemId     = _fItemId.Text.ToString()
            Item.ItType     = _fType.Text.ToString()
            Item.ItColor    = _fColor.Text.ToString()
            Item.ItWeigh    = ParseDec(_fWeigh.Text.ToString())
            Item.ItUsed     = ParseDec(_fUsed.Text.ToString())
            Item.ItRemain   = ParseDec(_fRemain.Text.ToString())
            Item.ItPurch    = ParseDec(_fPurch.Text.ToString())
            Item.ItVendor   = _fVendor.Text.ToString()
            Item.ItVendorId = _fVendorId.Text.ToString()
            Item.ItVReorder = _fVReorder.Text.ToString()
            Item.ItLastDt   = _fLastDt.Text.ToString()
            _Saved = True
            Application.RequestStop()
        End Sub

        Private Function ParseDec(s As String) As Decimal
            Dim v As Decimal : Decimal.TryParse(s, v) : Return v
        End Function

    End Class

End Namespace
