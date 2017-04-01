Imports System.IO

Public Class Form1
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If NumericUpDown1.Value = 0 Then
            CreateSnapshot() 'Runs the snapshot one time only
        Else
            CreateSnapshot()
            Timer1.Interval = NumericUpDown1.Value * 60 * 60 * 1000 'Hours * 60 minutes * 60 seconds * 1000 milliseconds
            Timer1.Start()
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'Save File Dialog
        SaveFileDialog1.Title = "Browse for a location to save Snapshot" 'Title
        SaveFileDialog1.Filter = "7zip File (*.7z)|*.7z" 'Extension to show
        SaveFileDialog1.FileName = Path.GetFileName(TextBox1.Text) 'Filename
        SaveFileDialog1.ShowDialog() 'Shows Dialog
        If String.IsNullOrEmpty(SaveFileDialog1.FileName) = False Then TextBox1.Text = SaveFileDialog1.FileName 'If the filename is not empty, show the specified filename in the textbox1
    End Sub
    Private Sub CreateSnapshot()
        'We will send a signal to properly close the Gridcoin Wallet
        Dim GridcoinProcess As Process() = Process.GetProcessesByName("gridcoinresearch")
        For Each GRCProcess In GridcoinProcess
            GRCProcess.CloseMainWindow()
            'While loop to wait for the process to exit
            While GRCProcess.HasExited = False
                'Waits 1 second, then it will check again if the process has exited
                Threading.Thread.Sleep(1000)
            End While
            'As of this moment, the Gridcoin Wallet should has been closed properly
        Next
        'Now, we will compress the txleveldb folder and the blk0001.dat file
        Dim CompressionProcess As New ProcessStartInfo("C:\Program Files\7-zip\7z.exe")
        Dim AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\GridcoinResearch"
        CompressionProcess.Arguments = "a -m0=LZMA2 -mmt -mx9 """ & Path.GetDirectoryName(TextBox1.Text) & "\" & Path.GetFileNameWithoutExtension(TextBox1.Text) & " - " & DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss tt") & ".7z"" """ & AppDataPath & "\txleveldb"" """ & AppDataPath & "\blk0001.dat"""
        Dim StartProcess As Process = Process.Start(CompressionProcess)
        StartProcess.WaitForExit()
        'Backup should now be complete and we will launch the wallet again!
        If My.Computer.FileSystem.FileExists("C:\Program Files (x86)\GridcoinResearch\gridcoinresearch.exe") Then
            Process.Start("C:\Program Files (x86)\GridcoinResearch\gridcoinresearch.exe")
        ElseIf My.Computer.FileSystem.FileExists("C:\Program Files\GridcoinResearch\gridcoinresearch.exe") Then
            Process.Start("C:\Program Files\GridcoinResearch\gridcoinresearch.exe")
        Else
            MsgBox("GridcoinResearch.exe could not be found. Please launch the wallet manually")
        End If

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        CreateSnapshot() 'When reached the specified amount of hours, the snapshot will be created again
    End Sub
End Class
