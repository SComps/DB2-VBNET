Imports Terminal.Gui
Imports TDim = Terminal.Gui.Dim
Imports Price3DManager.Views

Module Program

    Sub Main()
        Application.Init()

        Dim top = Application.Top

        Dim menuBar As New MenuBar(New MenuBarItem() {
            New MenuBarItem("_File", New MenuItem() {
                New MenuItem("_Quit", "Exit application", Sub() Application.RequestStop())
            }),
            New MenuBarItem("_Tables", New MenuItem() {
                New MenuItem("_Filament Inventory", "Browse/edit FILMNT",   AddressOf OpenFilament),
                New MenuItem("_Customers",          "Browse/edit P3DCUST",  AddressOf OpenCustomers),
                New MenuItem("_Projects",           "Browse/edit PROJECTS", AddressOf OpenProjects)
            }),
            New MenuBarItem("_Help", New MenuItem() {
                New MenuItem("_About", "", Sub()
                    MessageBox.Query(50, 10, "About",
                        "PRICE3D Manager" & Chr(10) &
                        "" & Chr(10) &
                        "Db2 for z/OS  |  .NET 10" & Chr(10) &
                        "Terminal.Gui  |  IBM DRDA" & Chr(10) &
                        "" & Chr(10) &
                        "Server:  10.10.13.2:8103" & Chr(10) &
                        "DB:      DBD1LOC", "OK")
                End Sub)
            })
        })

        Dim win As New Window("PRICE3D Database Manager") With {
            .X = 0, .Y = 1,
            .Width = TDim.Fill(),
            .Height = TDim.Fill()
        }

        Dim lines = {
            "",
            "  Welcome to the PRICE3D Database Manager",
            "",
            "  Use the menu bar above to navigate:",
            "",
            "    Tables > Filament Inventory  -  Manage filament stock (FILMNT)",
            "    Tables > Customers           -  Manage customer records (P3DCUST)",
            "    Tables > Projects            -  Manage print projects (PROJECTS)",
            "",
            "  Keyboard shortcuts:",
            "    F9 / Alt+Space  Open menu bar",
            "    Arrow keys      Navigate lists",
            "    Enter           Activate button",
            "    Esc             Go back / cancel",
            "",
            "  Connected to:  10.10.13.2:8103  (DBD1LOC)"
        }

        Dim y = 0
        For Each line In lines
            win.Add(New Label(line) With {.X = 1, .Y = y})
            y += 1
        Next

        top.Add(menuBar, win)
        Application.Run()
        Application.Shutdown()
    End Sub

    Private Sub OpenFilament()
        Application.Run(New FilamentView())
    End Sub

    Private Sub OpenCustomers()
        Application.Run(New CustomerView())
    End Sub

    Private Sub OpenProjects()
        Application.Run(New ProjectView())
    End Sub

End Module
