Imports Terminal.Gui
Imports TDim = Terminal.Gui.Dim
Imports Price3DManager.Db
Imports System.Collections.ObjectModel

Namespace Views

    Public Class CustomerView
        Inherits Window

        Private _list  As ListView
        Private _items As List(Of Customer)

        Public Sub New()
            MyBase.New("Customers (P3DCUST)")
            X = 0 : Y = 1
            Width = TDim.Fill() : Height = TDim.Fill()
            LoadData()
            BuildUi()
        End Sub

        Private Sub LoadData()
            _items = CustomerRepo.GetAll()
        End Sub

        Private Function ToOC() As ObservableCollection(Of String)
            Dim oc As New ObservableCollection(Of String)
            For Each c In _items
                oc.Add($"{c.CustId,-8} {c.CName,-30} {c.City,-14} {c.State}")
            Next
            Return oc
        End Function

        Private Sub BuildUi()
            Add(New Label($"{"ID",-8} {"Name",-30} {"City",-14} St") With {.X = 1, .Y = 0})

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
            Dim dlg As New CustomerEditDialog(New Customer(), True)
            Application.Run(dlg)
            If dlg.Saved Then SaveAndRefresh(dlg.Item, True)
        End Sub

        Private Sub OnEdit()
            If _items.Count = 0 OrElse _list.SelectedItem < 0 Then Return
            Dim dlg As New CustomerEditDialog(_items(_list.SelectedItem), False)
            Application.Run(dlg)
            If dlg.Saved Then SaveAndRefresh(dlg.Item, False)
        End Sub

        Private Sub OnDelete()
            If _items.Count = 0 OrElse _list.SelectedItem < 0 Then Return
            Dim c = _items(_list.SelectedItem)
            If MessageBox.Query(50, 7, "Confirm", $"Delete customer '{c.CustId}'?", "Yes", "No") = 0 Then
                Try
                    CustomerRepo.Delete(c.CustId)
                    LoadData() : _list.SetSource(ToOC())
                Catch ex As Exception
                    MessageBox.ErrorQuery(60, 7, "Error", ex.Message, "OK")
                End Try
            End If
        End Sub

        Private Sub SaveAndRefresh(cust As Customer, isNew As Boolean)
            Try
                CustomerRepo.Upsert(cust, isNew)
                LoadData() : _list.SetSource(ToOC())
            Catch ex As Exception
                MessageBox.ErrorQuery(60, 7, "Save Error", ex.Message, "OK")
            End Try
        End Sub

    End Class

    Public Class CustomerEditDialog
        Inherits Dialog

        Private _item  As Customer
        Private _saved As Boolean

        Public ReadOnly Property Item As Customer
            Get
                Return _item
            End Get
        End Property
        Public ReadOnly Property Saved As Boolean
            Get
                Return _saved
            End Get
        End Property

        Private _fId, _fName, _fAddr1, _fAddr2, _fCity, _fState, _fZip, _fPhone, _fEmail As TextField
        Private _isNew As Boolean

        Public Sub New(item As Customer, isNew As Boolean)
            MyBase.New(If(isNew, "Add Customer", "Edit Customer"), 60, 16)
            _item  = item
            _isNew = isNew
            BuildUi()
        End Sub

        Private Sub BuildUi()
            Dim labels = {"Customer ID:", "Name:", "Address 1:", "Address 2:", "City:", "State:", "Zip:", "Phone:", "Email:"}
            Dim values = {If(_item.CustId, ""), If(_item.CName, ""), If(_item.Addr1, ""),
                          If(_item.Addr2, ""), If(_item.City, ""), If(_item.State, ""),
                          If(_item.Zip, ""), If(_item.Phone, ""), If(_item.Email, "")}

            Dim fields As New List(Of TextField)
            For i = 0 To labels.Length - 1
                Add(New Label(labels(i)) With {.X = 1, .Y = i})
                Dim tf As New TextField(values(i)) With {
                    .X = 14, .Y = i, .Width = 40,
                    .ReadOnly = (i = 0 AndAlso Not _isNew)
                }
                Add(tf)
                fields.Add(tf)
            Next

            _fId    = fields(0) : _fName  = fields(1) : _fAddr1 = fields(2)
            _fAddr2 = fields(3) : _fCity  = fields(4) : _fState = fields(5)
            _fZip   = fields(6) : _fPhone = fields(7) : _fEmail = fields(8)

            Dim btnSave   As New Button("_Save")
            Dim btnCancel As New Button("_Cancel")
            AddHandler btnSave.Clicked, Sub()
                _item.CustId = _fId.Text.ToString()    : _item.CName  = _fName.Text.ToString()
                _item.Addr1  = _fAddr1.Text.ToString() : _item.Addr2  = _fAddr2.Text.ToString()
                _item.City   = _fCity.Text.ToString()  : _item.State  = _fState.Text.ToString()
                _item.Zip    = _fZip.Text.ToString()   : _item.Phone  = _fPhone.Text.ToString()
                _item.Email  = _fEmail.Text.ToString()
                _saved = True
                Application.RequestStop()
            End Sub
            AddHandler btnCancel.Clicked, Sub() Application.RequestStop()
            AddButton(btnSave)
            AddButton(btnCancel)
        End Sub

    End Class

End Namespace
