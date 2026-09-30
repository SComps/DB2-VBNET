Imports System
Imports System.Windows.Forms
Imports Db2Spufi.WinForms.Forms

Module Program

    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New MainForm())
    End Sub

End Module
