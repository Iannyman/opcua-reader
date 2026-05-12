Imports System.Linq
Imports Opc.Ua
Imports Opc.Ua.Client

Public Class OpcUaManager

    Private _connections As New Dictionary(Of String, OpcUaConnection)()
    Private ReadOnly _logger As FileLogger

    Public Sub New(logger As FileLogger)
        _logger = logger
    End Sub

    Public Event DataTriggered(serverName As String, readValues As Dictionary(Of String, DataValue))
    Public Event ConnectionLost(serverName As String)
    Public Event ConnectionRestored(serverName As String)

    Public Sub AddServer(config As ServerConfig)
        Dim conn = New OpcUaConnection(config, _logger)
        AddHandler conn.DataTriggered, AddressOf OnConnectionDataTriggered
        AddHandler conn.ConnectionLost, Sub(name) RaiseEvent ConnectionLost(name)
        AddHandler conn.ConnectionRestored, Sub(name) RaiseEvent ConnectionRestored(name)
        _connections(config.Name) = conn
    End Sub

    Public Function GetServerConfigs() As List(Of ServerConfig)
        Return _connections.Values.Select(Function(c) c.ServerConfig).ToList()
    End Function

    Private Sub OnConnectionDataTriggered(serverName As String,
                                          readValues As Dictionary(Of String, DataValue))
        RaiseEvent DataTriggered(serverName, readValues)
    End Sub

    Public Async Function ConnectAllAsync() As Task
        Dim exceptions As New List(Of Exception)()
        For Each c In _connections.Values
            Try
                Await c.ConnectAsync()
            Catch ex As Exception
                _logger?.Log(c.ServerConfig.Name, $"Connect error: {ex}")
                exceptions.Add(ex)
            End Try
        Next
        If exceptions.Count > 0 Then
            Throw New AggregateException(exceptions)
        End If
    End Function

    Public Async Function DisconnectAllAsync() As Task
        Dim exceptions As New List(Of Exception)()
        For Each c In _connections.Values
            Try
                Await c.DisconnectAsync()
            Catch ex As Exception
                _logger?.Log(c.ServerConfig.Name, $"Disconnect error: {ex}")
                exceptions.Add(ex)
            End Try
        Next
        If exceptions.Count > 0 Then
            Throw New AggregateException(exceptions)
        End If
    End Function

    Public Function GetConnection(serverName As String) As OpcUaConnection
        If _connections.ContainsKey(serverName) Then Return _connections(serverName)
        Return Nothing
    End Function

    Public Async Function ConnectServerAsync(serverName As String) As Task
        Dim conn = GetConnection(serverName)
        If conn Is Nothing Then Throw New Exception($"Server '{serverName}' not found.")
        Await conn.ConnectAsync()
    End Function

    Public Async Function DisconnectServerAsync(serverName As String) As Task
        Dim conn = GetConnection(serverName)
        If conn Is Nothing Then Throw New Exception($"Server '{serverName}' not found.")
        Await conn.DisconnectAsync()
    End Function

    Public Async Function ReconnectServerAsync(serverName As String) As Task
        Dim conn = GetConnection(serverName)
        If conn Is Nothing Then Throw New Exception($"Server '{serverName}' not found.")
        Await conn.ReconnectAsync()
    End Function

End Class