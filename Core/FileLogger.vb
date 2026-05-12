Imports System.IO
Imports System.Text

Public Class FileLogger
    Implements IDisposable

    Private Class ServerLogWriter
        Implements IDisposable

        Private ReadOnly _lock As New Object()
        Private _writer As StreamWriter
        Private _currentDate As Date
        Private ReadOnly _logDirectory As String

        Private _dirOk As Boolean = False

        Public Sub New(baseLogPath As String, serverName As String)
            _logDirectory = System.IO.Path.Combine(baseLogPath, SanitizeFolderName(serverName))
            Try
                System.IO.Directory.CreateDirectory(_logDirectory)
                _dirOk = True
                EnsureWriter()
            Catch
            End Try
        End Sub

        Private Shared Function SanitizeFolderName(name As String) As String
            Dim sb As New StringBuilder(name)
            For Each c In System.IO.Path.GetInvalidFileNameChars()
                sb.Replace(c, "_"c)
            Next
            For Each c In System.IO.Path.GetInvalidPathChars()
                sb.Replace(c, "_"c)
            Next
            Dim result = sb.ToString().Trim()
            result = result.TrimEnd("."c, " "c)
            If String.IsNullOrEmpty(result) Then result = "UnknownServer"
            Return result
        End Function

        Private Sub EnsureWriter()
            Dim today = Date.Today
            If _writer Is Nothing OrElse _currentDate <> today Then
                If _writer IsNot Nothing Then
                    Try
                        _writer.Flush()
                        _writer.Close()
                        _writer.Dispose()
                    Catch
                    End Try
                End If
                Dim fileName = today.ToString("yyyy-MM-dd") & ".log"
                Dim fullPath = System.IO.Path.Combine(_logDirectory, fileName)
                _writer = New StreamWriter(fullPath, append:=True, encoding:=Encoding.UTF8) With {
                    .AutoFlush = False
                }
                _currentDate = today
            End If
        End Sub

        Public Sub WriteEntry(message As String)
            If Not _dirOk Then Return
            SyncLock _lock
                Try
                    EnsureWriter()
                    _writer.WriteLine(message)
                    _writer.Flush()
                Catch
                End Try
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            SyncLock _lock
                Try
                    If _writer IsNot Nothing Then
                        _writer.Flush()
                        _writer.Close()
                        _writer.Dispose()
                        _writer = Nothing
                    End If
                Catch
                End Try
            End SyncLock
        End Sub
    End Class

    Private ReadOnly _writers As New Dictionary(Of String, ServerLogWriter)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _lock As New Object()
    Private ReadOnly _baseLogPath As String
    Private _cleanupTimer As System.Timers.Timer

    Public Sub New()
        _baseLogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
        Try
            System.IO.Directory.CreateDirectory(_baseLogPath)
            CleanupOldLogs()
        Catch
        End Try

        _cleanupTimer = New System.Timers.Timer(24 * 60 * 60 * 1000)
        AddHandler _cleanupTimer.Elapsed, Sub() CleanupOldLogs()
        _cleanupTimer.Start()
    End Sub

    Private Sub CleanupOldLogs()
        Try
            Dim cutoff = Date.Now.AddDays(-30)
            For Each logDir In System.IO.Directory.GetDirectories(_baseLogPath)
                For Each logFile In System.IO.Directory.GetFiles(logDir, "*.log")
                    If System.IO.File.GetLastWriteTime(logFile) < cutoff Then
                        Try
                            System.IO.File.Delete(logFile)
                        Catch
                        End Try
                    End If
                Next
                Try
                    If System.IO.Directory.GetFileSystemEntries(logDir).Length = 0 Then
                        System.IO.Directory.Delete(logDir)
                    End If
                Catch
                End Try
            Next
        Catch
        End Try
    End Sub

    Public Sub EnsureServer(serverName As String)
        SyncLock _lock
            If Not _writers.ContainsKey(serverName) Then
                _writers(serverName) = New ServerLogWriter(_baseLogPath, serverName)
            End If
        End SyncLock
    End Sub

    Public Sub Log(serverName As String, message As String)
        Try
            Dim writer As ServerLogWriter = Nothing
            SyncLock _lock
                If Not _writers.ContainsKey(serverName) Then
                    _writers(serverName) = New ServerLogWriter(_baseLogPath, serverName)
                End If
                writer = _writers(serverName)
            End SyncLock
            Dim timestamp = Date.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            writer.WriteEntry($"[{timestamp}] {message}")
        Catch
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            _cleanupTimer?.Stop()
            _cleanupTimer?.Dispose()
        Catch
        End Try
        SyncLock _lock
            For Each kv In _writers
                Try
                    kv.Value.Dispose()
                Catch
                End Try
            Next
            _writers.Clear()
        End SyncLock
    End Sub
End Class
