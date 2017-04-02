Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading
Imports Google.Apis.Auth.OAuth2
Imports Google.Apis.Drive.v3
Imports Google.Apis.Services
Imports Google.Apis.Upload
Imports Google.Apis.Util.Store

Public Class Form1
    Shared Scopes As String() = {DriveService.Scope.DriveFile, DriveService.Scope.Drive}
    Shared ApplicationName As String = "Gridcoin Snapshot Generator"
    Public service As DriveService
    Public DriveFolderID As String = ""
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If String.IsNullOrEmpty(TextBox1.Text) Then
            MsgBox("Please browse for a location to save the snapshot, then click the Create Snapshot button")
        Else
            If NumericUpDown1.Value = 0 Then
                CreateSnapshot() 'Runs the snapshot one time only
            Else
                CreateSnapshot()
                Timer1.Interval = NumericUpDown1.Value * 60 * 60 * 1000 'Hours * 60 minutes * 60 seconds * 1000 milliseconds
                Timer1.Start()
            End If
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'Save File Dialog
        SaveFileDialog1.Title = "Browse for a location to save Snapshot" 'Title
        SaveFileDialog1.Filter = "7zip File (*.7z)|*.7z|ZIP File (*.zip)|*.zip" 'Extension to show
        SaveFileDialog1.FileName = Path.GetFileName(TextBox1.Text) 'Filename
        SaveFileDialog1.ShowDialog() 'Shows Dialog
        If String.IsNullOrEmpty(SaveFileDialog1.FileName) = False Then
            TextBox1.Text = SaveFileDialog1.FileName 'If the filename is not empty, show the specified filename in the textbox1
            If Path.GetExtension(SaveFileDialog1.FileName) = ".zip" Then
                RadioButton1.Checked = True
            Else
                RadioButton2.Checked = True
            End If
        End If
    End Sub
    Private UploadCancellationToken As System.Threading.CancellationToken
    Private starttime As DateTime
    Private timespent As TimeSpan
    Private secondsremaining As Integer = 0
    Private UploadCompleted As Boolean = False
    Private FileToUpload As String = ""
    Private FileUploadUri As Uri = Nothing
    Private Async Sub CreateSnapshot()
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
        Dim SnapshotFileName As String = Path.GetDirectoryName(TextBox1.Text) & "\" & Path.GetFileNameWithoutExtension(TextBox1.Text) & " - " & DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss tt")
        Dim FileNameExtension As String = ""
        Dim CompressionArguments As String = ""
        If RadioButton1.Checked = True Then
            FileNameExtension = ".zip"
            CompressionArguments = "a -tzip """ & SnapshotFileName & ".zip"" """ & AppDataPath & "\txleveldb"" """ & AppDataPath & "\blk0001.dat"""
        Else
            FileNameExtension = ".7z"
            CompressionArguments = "a -m0=LZMA2 -mmt -mx9 """ & SnapshotFileName & ".7z"" """ & AppDataPath & "\txleveldb"" """ & AppDataPath & "\blk0001.dat"""
        End If
        CompressionProcess.Arguments = CompressionArguments
        Dim StartProcess As Process = Process.Start(CompressionProcess)
        StartProcess.WaitForExit()
        If CheckBox2.Checked Then
            If CheckBox3.Checked Then
                Dim SnapshotFileStream As New FileStream(SnapshotFileName & FileNameExtension, FileMode.Open, FileAccess.Read)
                Dim MD5Hash As MD5 = MD5.Create
                Dim MD5HashToString As String = ""
                MD5Hash.ComputeHash(SnapshotFileStream)
                For Each b In MD5Hash.Hash
                    MD5HashToString += b.ToString("x2")
                Next
                My.Computer.FileSystem.WriteAllText(SnapshotFileName & FileNameExtension & ".md5", MD5HashToString & " *" & Path.GetFileName(SnapshotFileName) & FileNameExtension, False)
                SnapshotFileStream.Close()
            End If
            If CheckBox4.Checked Then
                Dim SnapshotFileStream As New FileStream(SnapshotFileName & FileNameExtension, FileMode.Open, FileAccess.Read)
                Dim SHA256Hash As SHA256 = SHA256Managed.Create()
                Dim SHA256HashToString As String = ""
                SHA256Hash.ComputeHash(SnapshotFileStream)
                For Each b In SHA256Hash.Hash
                    SHA256HashToString += b.ToString("x2")
                Next
                'MsgBox(Convert.ToString(SHA256HashToString))
                My.Computer.FileSystem.WriteAllText(SnapshotFileName & FileNameExtension & ".sha256", SHA256HashToString & " *" & Path.GetFileName(SnapshotFileName) & FileNameExtension, False)
                SnapshotFileStream.Close()
            End If
            If CheckBox5.Checked Then
                Dim SnapshotFileStream As New FileStream(SnapshotFileName & FileNameExtension, FileMode.Open, FileAccess.Read)
                Dim SHA512Hash As SHA512 = SHA512Managed.Create()
                Dim SHA512HashToString As String = ""
                SHA512Hash.ComputeHash(SnapshotFileStream)
                For Each b In SHA512Hash.Hash
                    SHA512HashToString += b.ToString("x2")
                Next
                'MsgBox(Convert.ToString(SHA512HashToString))
                My.Computer.FileSystem.WriteAllText(SnapshotFileName & FileNameExtension & ".sha512", SHA512HashToString & " *" & Path.GetFileName(SnapshotFileName) & FileNameExtension, False)
                SnapshotFileStream.Close()
            End If
        End If
        'Uploads the snapshot file
        If CheckBox1.Checked Then
            FileToUpload = SnapshotFileName & FileNameExtension
            While UploadCompleted = False
                Try
                    Label11.Text = String.Format("{0:N2} MB", My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024)
                    ProgressBar1.Maximum = My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024
                    Dim FileMetadata As New Data.File
                    FileMetadata.Name = My.Computer.FileSystem.GetName(FileToUpload)
                    Dim FileFolder As New List(Of String)
                    If String.IsNullOrEmpty(DriveFolderID) = False Then
                        FileFolder.Add(DriveFolderID)
                    Else
                        FileFolder.Add("root")
                    End If
                    FileMetadata.Parents = FileFolder
                    FileUploadUri = Nothing
                    Dim UploadStream As New FileStream(FileToUpload, System.IO.FileMode.Open, System.IO.FileAccess.Read)
                    FileMetadata.ModifiedTime = IO.File.GetLastWriteTimeUtc(FileToUpload)
                    Dim UploadFile As FilesResource.CreateMediaUpload = service.Files.Create(FileMetadata, UploadStream, "")
                    UploadFile.ChunkSize = ResumableUpload.MinimumChunkSize * 4
                    AddHandler UploadFile.ProgressChanged, New Action(Of IUploadProgress)(AddressOf Upload_ProgressChanged)
                    AddHandler UploadFile.ResponseReceived, New Action(Of Data.File)(AddressOf Upload_ResponseReceived)
                    AddHandler UploadFile.UploadSessionData, AddressOf Upload_UploadSessionData
                    UploadCancellationToken = New CancellationToken
                    starttime = DateTime.Now
                    If FileUploadUri = Nothing Then
                        Await UploadFile.UploadAsync(UploadCancellationToken)
                    Else
                        Await UploadFile.ResumeAsync(FileUploadUri, UploadCancellationToken)
                    End If
                Catch ex As Exception
                    UploadCompleted = False
                End Try
            End While
            UploadCompleted = False
            'If selected, upload the MD5 hash
            If CheckBox3.Checked Then
                FileToUpload = SnapshotFileName & FileNameExtension & ".md5"
                While UploadCompleted = False
                    Try
                        Label11.Text = String.Format("{0:N2} MB", My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024)
                        ProgressBar1.Maximum = My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024
                        Dim FileMetadata As New Data.File
                        FileMetadata.Name = My.Computer.FileSystem.GetName(FileToUpload)
                        Dim FileFolder As New List(Of String)
                        If String.IsNullOrEmpty(DriveFolderID) = False Then
                            FileFolder.Add(DriveFolderID)
                        Else
                            FileFolder.Add("root")
                        End If
                        FileMetadata.Parents = FileFolder
                        FileUploadUri = Nothing
                        Dim UploadStream As New FileStream(FileToUpload, System.IO.FileMode.Open, System.IO.FileAccess.Read)
                        FileMetadata.ModifiedTime = IO.File.GetLastWriteTimeUtc(FileToUpload)
                        Dim UploadFile As FilesResource.CreateMediaUpload = service.Files.Create(FileMetadata, UploadStream, "")
                        UploadFile.ChunkSize = ResumableUpload.MinimumChunkSize * 4
                        AddHandler UploadFile.ProgressChanged, New Action(Of IUploadProgress)(AddressOf Upload_ProgressChanged)
                        AddHandler UploadFile.ResponseReceived, New Action(Of Data.File)(AddressOf Upload_ResponseReceived)
                        AddHandler UploadFile.UploadSessionData, AddressOf Upload_UploadSessionData
                        UploadCancellationToken = New CancellationToken
                        starttime = DateTime.Now
                        If FileUploadUri = Nothing Then
                            Await UploadFile.UploadAsync(UploadCancellationToken)
                        Else
                            Await UploadFile.ResumeAsync(FileUploadUri, UploadCancellationToken)
                        End If
                    Catch ex As Exception
                        UploadCompleted = False
                    End Try
                End While
                UploadCompleted = False
            End If
            If CheckBox4.Checked Then
                FileToUpload = SnapshotFileName & FileNameExtension & ".sha256"
                While UploadCompleted = False
                    Try
                        Label11.Text = String.Format("{0:N2} MB", My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024)
                        ProgressBar1.Maximum = My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024
                        Dim FileMetadata As New Data.File
                        FileMetadata.Name = My.Computer.FileSystem.GetName(FileToUpload)
                        Dim FileFolder As New List(Of String)
                        If String.IsNullOrEmpty(DriveFolderID) = False Then
                            FileFolder.Add(DriveFolderID)
                        Else
                            FileFolder.Add("root")
                        End If
                        FileMetadata.Parents = FileFolder
                        FileUploadUri = Nothing
                        Dim UploadStream As New FileStream(FileToUpload, System.IO.FileMode.Open, System.IO.FileAccess.Read)
                        FileMetadata.ModifiedTime = IO.File.GetLastWriteTimeUtc(FileToUpload)
                        Dim UploadFile As FilesResource.CreateMediaUpload = service.Files.Create(FileMetadata, UploadStream, "")
                        UploadFile.ChunkSize = ResumableUpload.MinimumChunkSize * 4
                        AddHandler UploadFile.ProgressChanged, New Action(Of IUploadProgress)(AddressOf Upload_ProgressChanged)
                        AddHandler UploadFile.ResponseReceived, New Action(Of Data.File)(AddressOf Upload_ResponseReceived)
                        AddHandler UploadFile.UploadSessionData, AddressOf Upload_UploadSessionData
                        UploadCancellationToken = New CancellationToken
                        starttime = DateTime.Now
                        If FileUploadUri = Nothing Then
                            Await UploadFile.UploadAsync(UploadCancellationToken)
                        Else
                            Await UploadFile.ResumeAsync(FileUploadUri, UploadCancellationToken)
                        End If
                    Catch ex As Exception
                        UploadCompleted = False
                    End Try
                End While
                UploadCompleted = False
            End If
            If CheckBox5.Checked Then
                FileToUpload = SnapshotFileName & FileNameExtension & ".sha512"
                While UploadCompleted = False
                    Try
                        Label11.Text = String.Format("{0:N2} MB", My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024)
                        ProgressBar1.Maximum = My.Computer.FileSystem.GetFileInfo(FileToUpload).Length / 1024 / 1024
                        Dim FileMetadata As New Data.File
                        FileMetadata.Name = My.Computer.FileSystem.GetName(FileToUpload)
                        Dim FileFolder As New List(Of String)
                        If String.IsNullOrEmpty(DriveFolderID) = False Then
                            FileFolder.Add(DriveFolderID)
                        Else
                            FileFolder.Add("root")
                        End If
                        FileMetadata.Parents = FileFolder
                        FileUploadUri = Nothing
                        Dim UploadStream As New FileStream(FileToUpload, System.IO.FileMode.Open, System.IO.FileAccess.Read)
                        FileMetadata.ModifiedTime = IO.File.GetLastWriteTimeUtc(FileToUpload)
                        Dim UploadFile As FilesResource.CreateMediaUpload = service.Files.Create(FileMetadata, UploadStream, "")
                        UploadFile.ChunkSize = ResumableUpload.MinimumChunkSize * 4
                        AddHandler UploadFile.ProgressChanged, New Action(Of IUploadProgress)(AddressOf Upload_ProgressChanged)
                        AddHandler UploadFile.ResponseReceived, New Action(Of Data.File)(AddressOf Upload_ResponseReceived)
                        AddHandler UploadFile.UploadSessionData, AddressOf Upload_UploadSessionData
                        UploadCancellationToken = New CancellationToken
                        starttime = DateTime.Now
                        If FileUploadUri = Nothing Then
                            Await UploadFile.UploadAsync(UploadCancellationToken)
                        Else
                            Await UploadFile.ResumeAsync(FileUploadUri, UploadCancellationToken)
                        End If
                    Catch ex As Exception
                        UploadCompleted = False
                    End Try
                End While
                UploadCompleted = False
            End If
        End If
        'Backup should now be complete and we will launch the wallet again!
        If My.Computer.FileSystem.FileExists("C:\Program Files (x86)\GridcoinResearch\gridcoinresearch.exe") Then
            Process.Start("C:\Program Files (x86)\GridcoinResearch\gridcoinresearch.exe")
        ElseIf My.Computer.FileSystem.FileExists("C:\Program Files\GridcoinResearch\gridcoinresearch.exe") Then
            Process.Start("C:\Program Files\GridcoinResearch\gridcoinresearch.exe")
        Else
            MsgBox("GridcoinResearch.exe could not be found. Please launch the wallet manually")
        End If
    End Sub
    Shared BytesSentText As Long
    Shared UploadStatusText As String
    Private Sub Upload_ProgressChanged(uploadStatusInfo As IUploadProgress)
        Select Case uploadStatusInfo.Status
            Case UploadStatus.Completed
                UploadCompleted = True
                UploadStatusText = "Completed!!"
                BytesSentText = My.Computer.FileSystem.GetFileInfo(FileToUpload).Length
                UpdateBytesSent()
            Case UploadStatus.Starting
                UploadStatusText = "Starting..."
                UpdateBytesSent()
            Case UploadStatus.Uploading
                UploadCompleted = False
                BytesSentText = uploadStatusInfo.BytesSent
                UploadStatusText = "Uploading..."
                timespent = DateTime.Now - starttime
                Try
                    secondsremaining = (timespent.TotalSeconds / ProgressBar1.Value * (ProgressBar1.Maximum - ProgressBar1.Value))
                Catch
                    secondsremaining = 0
                End Try
                UpdateBytesSent()
            Case UploadStatus.Failed
                UploadCompleted = True
                UploadStatusText = "Retrying..."
                UpdateBytesSent()
                Thread.Sleep(1000)
        End Select
    End Sub

    Private Sub Upload_ResponseReceived(file As Data.File)
        UploadStatusText = "Completed!!"
        UploadCompleted = True
        BytesSentText = My.Computer.FileSystem.GetFileInfo(FileToUpload).Length
        UpdateBytesSent()
    End Sub

    Private Sub Upload_UploadSessionData(ByVal uploadSessionData As IUploadSessionData)
        FileUploadUri = New Uri(uploadSessionData.UploadUri.AbsoluteUri)
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        CreateSnapshot() 'When reached the specified amount of hours, the snapshot will be created again
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked Then
            Dim credential As UserCredential
            If My.Computer.FileSystem.FileExists("client_secret.json") Then
                Using stream = New FileStream("client_secret.json", FileMode.Open, FileAccess.Read)
                    Dim credPath As String = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)
                    credPath = Path.Combine(credPath, ".credentials/GoogleDriveUploaderTool.json")
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, Scopes, "user", CancellationToken.None, New FileDataStore(credPath, True)).Result
                End Using
                ' Create Drive API service.
                Dim Initializer As New BaseClientService.Initializer()
                Initializer.HttpClientInitializer = credential
                Initializer.ApplicationName = ApplicationName
                service = New DriveService(Initializer)
                service.HttpClient.Timeout = TimeSpan.FromSeconds(120)
                My.Settings.Upload = True
                TextBox2.Enabled = True
                Button3.Enabled = True
            Else
                My.Settings.Upload = False
                TextBox2.Enabled = False
                Button3.Enabled = False
                MsgBox("client_secret.json not found. This file is needed to enable Google Drive Uploading. Please create a Google Drive API Key in the Google Cloud Console and download this file.")
                CheckBox1.Checked = False
            End If
        Else
            My.Settings.Upload = False
            TextBox2.Enabled = False
            Button3.Enabled = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        If CheckBox3.Checked = True Then
            My.Settings.MD5 = True
        Else
            My.Settings.MD5 = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        NumericUpDown1.Value = My.Settings.Timer
        If My.Settings.Format = "zip" Then RadioButton1.Checked = True
        If My.Settings.Format = "7z" Then RadioButton2.Checked = True
        If My.Settings.CreateChecksum = True Then CheckBox2.Checked = True
        If My.Settings.MD5 = True Then CheckBox3.Checked = True
        If My.Settings.SHA256 = True Then CheckBox4.Checked = True
        If My.Settings.SHA512 = True Then CheckBox5.Checked = True
        If String.IsNullOrEmpty(My.Settings.UploadFolder) = False Then TextBox2.Text = My.Settings.UploadFolder
        If String.IsNullOrEmpty(My.Settings.UploadFolderID) = False Then DriveFolderID = My.Settings.UploadFolderID
    End Sub

    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        My.Settings.Format = "zip"
        My.Settings.Save()
        If String.IsNullOrEmpty(TextBox1.Text) = False Then
            TextBox1.Text = Path.GetDirectoryName(TextBox1.Text) & "\" & Path.GetFileNameWithoutExtension(TextBox1.Text) & ".zip"
        End If
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged
        My.Settings.Format = "7z"
        My.Settings.Save()
        If String.IsNullOrEmpty(TextBox1.Text) = False Then
            TextBox1.Text = Path.GetDirectoryName(TextBox1.Text) & "\" & Path.GetFileNameWithoutExtension(TextBox1.Text) & ".7z"
        End If
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        If CheckBox2.Checked Then
            My.Settings.CreateChecksum = True
        Else
            My.Settings.CreateChecksum = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged
        If CheckBox4.Checked Then
            My.Settings.SHA256 = True
        Else
            My.Settings.SHA256 = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged
        If CheckBox5.Checked Then
            My.Settings.SHA512 = True
        Else
            My.Settings.SHA512 = False
        End If
        My.Settings.Save()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        SearchFolder.ShowDialog()
    End Sub
    Private Sub UpdateBytesSent()
        If Label12.InvokeRequired Then
            Dim method As MethodInvoker = New MethodInvoker(AddressOf UpdateBytesSent)
            Invoke(method)
        End If
        If Label13.InvokeRequired Then
            Dim method As MethodInvoker = New MethodInvoker(AddressOf UpdateBytesSent)
            Invoke(method)
        End If
        If Label14.InvokeRequired Then
            Dim method As MethodInvoker = New MethodInvoker(AddressOf UpdateBytesSent)
            Invoke(method)
        End If
        If Label17.InvokeRequired Then
            Dim method As MethodInvoker = New MethodInvoker(AddressOf UpdateBytesSent)
            Invoke(method)
        End If
        If ProgressBar1.InvokeRequired Then
            Dim method As MethodInvoker = New MethodInvoker(AddressOf UpdateBytesSent)
            Invoke(method)
        End If
        Label12.Text = String.Format("{0:N2} MB", BytesSentText / 1024 / 1024)
        Label13.Text = UploadStatusText
        Try
            ProgressBar1.Value = BytesSentText / 1024 / 1024
        Catch
        End Try
        Label14.Text = String.Format("{0:F2}%", ((ProgressBar1.Value / ProgressBar1.Maximum) * 100))
        Dim timeFormatted As TimeSpan = TimeSpan.FromSeconds(secondsremaining)
        Label17.Text = String.Format("{0}:{1:mm}:{1:ss}", CInt(Math.Truncate(timeFormatted.TotalHours)), timeFormatted)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Donations.ShowDialog()
    End Sub
End Class
