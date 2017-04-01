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
        SaveFileDialog1.Title = "Browse for a location to save Snapshot"
        SaveFileDialog1.Filter = "7zip File (*.7z)|*.7z"
        SaveFileDialog1.FileName = TextBox1.Text
        SaveFileDialog1.ShowDialog()
        If String.IsNullOrEmpty(SaveFileDialog1.FileName) = False Then TextBox1.Text = SaveFileDialog1.FileName
    End Sub
    Private Sub CreateSnapshot()
        'We will send a signal to close the Gridcoin Wallet
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
        CompressionProcess.Arguments = "a -m0=LZMA2 -mmt -mx9 """ & Path.GetDirectoryName(TextBox1.Text) & "\" & Path.GetFileNameWithoutExtension(TextBox1.Text) & " " & DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss tt") & ".7z"" """ & AppDataPath & "\txleveldb"" """ & AppDataPath & "\blk0001.dat"""
        Dim StartProcess As Process = Process.Start(CompressionProcess)
        StartProcess.WaitForExit()
        'Backup should now be complete!
        If My.Computer.FileSystem.FileExists("C:\Program Files (x86)\GridcoinResearch\gridcoinresearch.exe") Then
            Process.Start("C:\Program Files (x86)\GridcoinResearch\gridcoinresearch.exe")
        ElseIf My.Computer.FileSystem.FileExists("C:\Program Files\GridcoinResearch\gridcoinresearch.exe") Then
            Process.Start("C:\Program Files\GridcoinResearch\gridcoinresearch.exe")
        Else
            MsgBox("GridcoinResearch.exe could not be found. Please launch the wallet manually")
        End If

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        CreateSnapshot()
    End Sub
End Class
