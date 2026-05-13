Imports System.IO
Imports MaterialSkin
Imports Newtonsoft.Json
Imports Opc.Ua
Public Class MainForm

    Private _manager As OpcUaManager
    Private _logger As FileLogger
    Private ReadOnly _serverContent As New Dictionary(Of String, ServerTabContent)()
    Private _sqlService As SqlService
    Private _sqlConnected As Boolean = False
    Private _shutdownReason As String = "UserClose"
    Private _lastUnhandledException As Exception = Nothing

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        AddHandler Application.ThreadException, AddressOf OnUIThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnBackgroundThreadException

        _logger = New FileLogger()
        _logger.EnsureServer("_Application")
        _manager = New OpcUaManager(_logger)

        Me.Icon = New Icon(IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"))

        ' Dark "Neon Industrial" color scheme
        Dim SkinManager = MaterialSkinManager.Instance
        SkinManager.AddFormToManage(Me)
        SkinManager.Theme = MaterialSkinManager.Themes.DARK
        SkinManager.ColorScheme = New ColorScheme(
            Primary.BlueGrey900,    ' primary
            Primary.BlueGrey800,    ' darkPrimary
            Primary.BlueGrey700,    ' lightPrimary
            Accent.Cyan700,         ' accent — neon cyan
            TextShade.WHITE)        ' text

        StyleDataGridView()

        dgvOverview.Columns.Add("colServer", "Server")
        dgvOverview.Columns.Add("colEndpoint", "Endpoint")
        dgvOverview.Columns.Add("colStatus", "Status")
        dgvOverview.Columns.Add("colLastTrigger", "Last Read")

        For Each col As DataGridViewColumn In dgvOverview.Columns
            col.FillWeight = 20.0F
            Select Case col.Name
                Case "colEndpoint" : col.FillWeight = 40.0F
                Case "colStatus" : col.FillWeight = 15.0F
                Case "colLastTrigger" : col.FillWeight = 25.0F
            End Select
        Next

        dgvOverview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvOverview.RowTemplate.Height = 30

        btnDisconnect.Enabled = False

        AddHandler _manager.DataTriggered, AddressOf OnDataTriggered
        AddHandler _manager.ConnectionLost, AddressOf OnConnectionLost
        AddHandler _manager.ConnectionRestored, AddressOf OnConnectionRestored

        TestSqlConnectionAsync()
    End Sub

    Private Sub StyleDataGridView()
        dgvOverview.EnableHeadersVisualStyles = False
        dgvOverview.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30)
        dgvOverview.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(160, 170, 180)
        dgvOverview.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F, FontStyle.Bold)
        dgvOverview.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 30, 30)
        dgvOverview.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        dgvOverview.ColumnHeadersHeight = 32

        dgvOverview.DefaultCellStyle.BackColor = Color.FromArgb(24, 24, 24)
        dgvOverview.DefaultCellStyle.ForeColor = Color.FromArgb(220, 225, 230)
        dgvOverview.DefaultCellStyle.Font = New Font("Segoe UI", 9.0F)
        dgvOverview.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 172, 193)
        dgvOverview.DefaultCellStyle.SelectionForeColor = Color.White
    End Sub

    Private Sub AddServerTab(serverName As String, endpointUrl As String)
        Dim tab As New TabPage()
        tab.Text = serverName

        Dim content As New ServerTabContent()
        content.Dock = DockStyle.Fill

        AddHandler content.btnConnect.Click, Sub() OnServerConnect(serverName)
        AddHandler content.btnDisconnect.Click, Sub() OnServerDisconnect(serverName)
        AddHandler content.btnReconnect.Click, Sub() OnServerReconnect(serverName)
        AddHandler content.btnClear.Click, Sub() OnServerClear(serverName)

        tab.Controls.Add(content)
        tabMain.TabPages.Add(tab)
        _serverContent(serverName) = content
    End Sub

    ' ─────────────────────────────────────────
    ' PER-SERVER ACTIONS
    ' ─────────────────────────────────────────
    Private Async Sub OnServerConnect(serverName As String)
        Dim c = _serverContent(serverName)
        c.btnConnect.Enabled = False
        lblStatus.Text = $"{serverName}: Connecting..."
        Try
            Await _manager.ConnectServerAsync(serverName)
            SetServerConnected(serverName, True)
            lblStatus.Text = $"{serverName}: Connected"
        Catch ex As Exception
            _logger?.Log(serverName, $"Connect failed: {ex.Message}")
            MessageBox.Show($"{serverName} connect failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            c.btnConnect.Enabled = True
        End Try
    End Sub

    Private Async Sub OnServerDisconnect(serverName As String)
        Dim c = _serverContent(serverName)
        c.btnDisconnect.Enabled = False
        lblStatus.Text = $"{serverName}: Disconnecting..."
        Try
            Await _manager.DisconnectServerAsync(serverName)
            SetServerConnected(serverName, False)
            lblStatus.Text = $"{serverName}: Disconnected"
        Catch ex As Exception
            _logger?.Log(serverName, $"Disconnect failed: {ex.Message}")
            MessageBox.Show($"{serverName} disconnect failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = $"{serverName}: Disconnect failed"
            c.btnDisconnect.Enabled = True
        End Try
    End Sub

    Private Async Sub OnServerReconnect(serverName As String)
        Dim c = _serverContent(serverName)
        c.btnReconnect.Enabled = False
        c.btnConnect.Enabled = False
        c.btnDisconnect.Enabled = False
        lblStatus.Text = $"{serverName}: Reconnecting..."
        Try
            Await _manager.ReconnectServerAsync(serverName)
            SetServerConnected(serverName, True)
            lblStatus.Text = $"{serverName}: Reconnected"
        Catch ex As Exception
            _logger?.Log(serverName, $"Reconnect failed: {ex.Message}")
            MessageBox.Show($"{serverName} reconnect failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SetServerConnected(serverName, False)
            lblStatus.Text = $"{serverName}: Reconnect failed"
        End Try
    End Sub

    Private Sub OnServerClear(serverName As String)
        If _serverContent.ContainsKey(serverName) Then
            _serverContent(serverName).logList.Items.Clear()
        End If
    End Sub

    Private Sub SetServerConnected(serverName As String, connected As Boolean)
        SetServerStatus(serverName, If(connected, "Connected", "Disconnected"))
    End Sub

    Private Sub SetServerStatus(serverName As String, status As String)
        If Not _serverContent.ContainsKey(serverName) Then Return
        Dim c = _serverContent(serverName)
        Dim connected = (status = "Connected")
        c.btnConnect.Enabled = Not connected
        c.btnDisconnect.Enabled = connected
        c.btnReconnect.Enabled = connected
        For i As Integer = 0 To dgvOverview.Rows.Count - 1
            If dgvOverview.Rows(i).Cells("colServer").Value?.ToString() = serverName Then
                dgvOverview.Rows(i).Cells("colStatus").Value = status
                Exit For
            End If
        Next
    End Sub

    ' ─────────────────────────────────────────
    ' GLOBAL ACTIONS
    ' ─────────────────────────────────────────
    Private Function LoadServerConfigs() As List(Of ServerConfig)
        Dim path = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json")
        If Not IO.File.Exists(path) Then
            Throw New FileNotFoundException($"Settings file not found: {path}")
        End If
        Dim json = IO.File.ReadAllText(path)
        Dim settings = JsonConvert.DeserializeObject(Of AppSettings)(json)
        Return settings?.Servers
    End Function

    Private Async Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        Dim configs As List(Of ServerConfig)
        Try
            configs = LoadServerConfigs()
        Catch ex As Exception
            MessageBox.Show($"Failed to load settings.json: {ex.Message}", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        For Each cfg In configs
            _manager.AddServer(cfg)
        Next

        For Each kv In _manager.GetServerConfigs()
            If Not _serverContent.ContainsKey(kv.Name) Then
                AddServerTab(kv.Name, kv.EndpointUrl)
                dgvOverview.Rows.Add(kv.Name, kv.EndpointUrl, "Disconnected", "-")
            End If
        Next

        Dim minWidth = 140 * (tabMain.TabPages.Count + 1)
        Me.MinimumSize = New Size(Math.Max(minWidth, 640), 400)

        btnConnect.Enabled = False
        btnDisconnect.Enabled = True
        lblStatus.Text = "Connecting all..."

        Try
            Await _manager.ConnectAllAsync()
            lblStatus.Text = "Connected — subscriptions active"
            For Each kv In _manager.GetServerConfigs()
                SetServerConnected(kv.Name, True)
            Next
        Catch ex As Exception
            Dim msg As String = ex.Message
            If TypeOf ex Is AggregateException Then
                msg = CType(ex, AggregateException).Flatten().InnerExceptions(0).Message
            End If
            MessageBox.Show($"Failed to connect: {msg}", "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Connection failed"
            btnConnect.Enabled = True
            btnDisconnect.Enabled = False
        End Try
    End Sub

    Private Async Sub btnDisconnect_Click(sender As Object, e As EventArgs) Handles btnDisconnect.Click
        btnDisconnect.Enabled = False
        lblStatus.Text = "Disconnecting all..."
        Try
            Await _manager.DisconnectAllAsync()
            btnConnect.Enabled = True
            lblStatus.Text = "Disconnected"
            For Each kv In _manager.GetServerConfigs()
                SetServerConnected(kv.Name, False)
            Next
        Catch ex As Exception
            Dim msg As String = ex.Message
            If TypeOf ex Is AggregateException Then
                msg = CType(ex, AggregateException).Flatten().InnerExceptions(0).Message
            End If
            MessageBox.Show($"Failed to disconnect: {msg}", "Disconnect error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Disconnect failed"
            btnConnect.Enabled = True
        End Try
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs)
        For Each c In _serverContent.Values
            c.logList.Items.Clear()
        Next
    End Sub

    ' ─────────────────────────────────────────
    ' OVERVIEW FORMATTING
    ' ─────────────────────────────────────────
    Private Sub dgvOverview_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles dgvOverview.CellFormatting
        If e.ColumnIndex < 0 OrElse e.RowIndex < 0 Then Return
        Dim colName = dgvOverview.Columns(e.ColumnIndex).Name
        If colName = "colStatus" AndAlso e.Value IsNot Nothing Then
            Select Case e.Value.ToString()
                Case "Connected"
                    e.CellStyle.ForeColor = Color.FromArgb(0, 230, 118)
                    e.CellStyle.Font = New Font("Segoe UI Semibold", 9.0F, FontStyle.Bold)
                Case "Disconnected"
                    e.CellStyle.ForeColor = Color.FromArgb(255, 82, 82)
                    e.CellStyle.Font = New Font("Segoe UI Semibold", 9.0F, FontStyle.Bold)
                Case "Reconnecting"
                    e.CellStyle.ForeColor = Color.FromArgb(255, 193, 7)
                    e.CellStyle.Font = New Font("Segoe UI Semibold", 9.0F, FontStyle.Bold)
            End Select
        ElseIf colName = "colEndpoint" AndAlso e.Value IsNot Nothing Then
            e.CellStyle.ForeColor = Color.FromArgb(120, 130, 140)
        End If
    End Sub

    ' ─────────────────────────────────────────
    ' DATA TRIGGER CALLBACK
    ' ─────────────────────────────────────────
    Private Sub OnDataTriggered(serverName As String,
                                readValues As Dictionary(Of String, DataValue))
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() OnDataTriggered(serverName, readValues))
            Return
        End If

        Dim log As ThemedListBox = Nothing
        If _serverContent.ContainsKey(serverName) Then
            log = _serverContent(serverName).logList
        Else
            Dim url As String = "(unknown)"
            For Each cfg In _manager.GetServerConfigs()
                If cfg.Name = serverName Then
                    url = cfg.EndpointUrl
                    Exit For
                End If
            Next
            AddServerTab(serverName, url)
            dgvOverview.Rows.Add(serverName, url, "Connected", "-")
            log = _serverContent(serverName).logList
        End If

        Dim timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        log.AddItem($"[{timestamp}] Read data triggerd:")
        _logger?.Log(serverName, "Read data triggered:")

        For Each kvp In readValues
            Dim valueText As String = If(kvp.Value?.Value?.ToString(), "(null)")
            log.AddItem($"  {kvp.Key}: {valueText}")
            _logger?.Log(serverName, $"  {kvp.Key}: {valueText}")
        Next

        For i As Integer = 0 To dgvOverview.Rows.Count - 1
            If dgvOverview.Rows(i).Cells("colServer").Value?.ToString() = serverName Then
                dgvOverview.Rows(i).Cells("colLastTrigger").Value = timestamp
                Exit For
            End If
        Next

        lblStatus.Text = $"Last read: {serverName} at {timestamp}"

        ' Send payload to SQL Server
        If _sqlService IsNot Nothing Then
            BuildAndSendPayloadAsync(serverName, readValues)
        End If
    End Sub

    ' ─────────────────────────────────────────
    ' CONNECTION STATE CALLBACKS
    ' ─────────────────────────────────────────
    Private Sub OnConnectionLost(serverName As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() OnConnectionLost(serverName))
            Return
        End If
        SetServerStatus(serverName, "Reconnecting")
        lblStatus.Text = $"{serverName}: Connection lost — reconnecting..."
        LogToServerTab(serverName, "Connection lost — auto-reconnecting...")
    End Sub

    Private Sub OnConnectionRestored(serverName As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() OnConnectionRestored(serverName))
            Return
        End If
        SetServerStatus(serverName, "Connected")
        lblStatus.Text = $"{serverName}: Reconnected"
        LogToServerTab(serverName, "Connection restored.")
    End Sub

    Private Sub LogToServerTab(serverName As String, message As String)
        If Not _serverContent.ContainsKey(serverName) Then Return
        Dim log = _serverContent(serverName).logList
        Dim timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        log.AddItem($"[{timestamp}] {message}")
        _logger?.Log(serverName, message)
    End Sub

    Private Async Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            _logger?.Log("_Application", $"Application shutting down. Reason: {_shutdownReason}")
            If _lastUnhandledException IsNot Nothing Then
                _logger?.Log("_Application", $"Unhandled exception: {_lastUnhandledException.GetType().Name} — {_lastUnhandledException.Message}")
                _logger?.Log("_Application", _lastUnhandledException.StackTrace)
            End If
        Catch
        End Try
        Try
            Await _manager.DisconnectAllAsync()
        Catch
        End Try
        Try
            _logger?.Dispose()
        Catch
        End Try
    End Sub

    Private Sub OnUIThreadException(sender As Object, e As System.Threading.ThreadExceptionEventArgs)
        _shutdownReason = "UnhandledError"
        _lastUnhandledException = e.Exception
        Try
            _logger?.Log("_Application", $"Unhandled UI thread exception: {e.Exception.GetType().Name} — {e.Exception.Message}")
            _logger?.Log("_Application", e.Exception.StackTrace)
        Catch
        End Try
    End Sub

    Private Sub OnBackgroundThreadException(sender As Object, e As UnhandledExceptionEventArgs)
        _shutdownReason = "UnhandledError"
        If TypeOf e.ExceptionObject Is Exception Then
            _lastUnhandledException = DirectCast(e.ExceptionObject, Exception)
            Try
                _logger?.Log("_Application", $"Unhandled background exception: {_lastUnhandledException.GetType().Name} — {_lastUnhandledException.Message}")
                _logger?.Log("_Application", _lastUnhandledException.StackTrace)
            Catch
            End Try
        End If
    End Sub

    ' ─────────────────────────────────────────
    ' SQL CONNECTION
    ' ─────────────────────────────────────────
    Private Async Sub TestSqlConnectionAsync()
        Try
            Dim path = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json")
            If Not IO.File.Exists(path) Then Return
            Dim json = IO.File.ReadAllText(path)
            Dim settings = JsonConvert.DeserializeObject(Of AppSettings)(json)
            If String.IsNullOrEmpty(settings?.SqlConnectionString) Then Return

            _sqlService = New SqlService(settings.SqlConnectionString)
            _sqlConnected = Await _sqlService.TestConnectionAsync()
        Catch
            _sqlConnected = False
        End Try

        lblSqlStatus.Text = If(_sqlConnected, "SQL: Connected", "SQL: Disconnected")
        lblSqlStatus.ForeColor = If(_sqlConnected, Color.FromArgb(0, 230, 118), Color.FromArgb(255, 82, 82))
    End Sub

    ' ─────────────────────────────────────────
    ' SQL PAYLOAD BUILD & SEND
    ' ─────────────────────────────────────────
    Private Async Sub BuildAndSendPayloadAsync(serverName As String,
                                                readValues As Dictionary(Of String, DataValue))
        Try
            Dim config = _manager.GetServerConfigs().FirstOrDefault(Function(c) c.Name = serverName)
            If config Is Nothing Then Return

            Dim swoIdent As String = ""
            Dim equipId As String = ""
            Dim nodes = New List(Of Dictionary(Of String, String))()

            For Each nodeCfg In config.NodesToReadOnTrigger
                Dim label = If(String.IsNullOrEmpty(nodeCfg.DisplayName), nodeCfg.NodeId, nodeCfg.DisplayName)
                Dim value As String = Nothing
                If readValues.ContainsKey(label) Then
                    value = If(readValues(label)?.Value?.ToString(), "")
                End If

                If nodeCfg.NodeId = config.SwoIdentNode AndAlso config.SwoIdentNode <> "" Then
                    swoIdent = value
                ElseIf nodeCfg.NodeId = config.EquipIdNode AndAlso config.EquipIdNode <> "" Then
                    equipId = value
                Else
                    nodes.Add(New Dictionary(Of String, String) From {
                        {"nodeId", nodeCfg.NodeId},
                        {"value", If(value, "")}
                    })
                End If
            Next

            Dim payloadObj = New Dictionary(Of String, Object) From {
                {"PLC_ID", serverName},
                {"SWO_IDENT", swoIdent},
                {"EQUIP_ID", equipId},
                {"nodes", nodes}
            }

            Dim payload = JsonConvert.SerializeObject(payloadObj)

            Dim result = Await _sqlService.InsertZsbDataAsync(payload)
            Dim resp = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(result)
            Dim success = If(resp IsNot Nothing AndAlso resp.ContainsKey("success"), Convert.ToInt32(resp("success")), 0)
            Dim message = If(resp IsNot Nothing AndAlso resp.ContainsKey("message"), resp("message")?.ToString(), result)

            If success = 1 Then
                LogToServerTab(serverName, $"SQL OK: {message}")
                ' Uncomment to reset the trigger to 0 after a successful SQL insert:
                ' Dim conn = _manager.GetConnection(serverName)
                ' If conn IsNot Nothing Then Await conn.WriteTriggerResetAsync()
            Else
                LogToServerTab(serverName, $"SQL ERROR: {message}")
            End If
        Catch ex As Exception
            LogToServerTab(serverName, $"SQL ERROR: {ex.Message}")
        End Try
    End Sub

End Class

Public Class AppSettings
    Public Property SqlConnectionString As String = ""
    Public Property Servers As New List(Of ServerConfig)()
End Class
