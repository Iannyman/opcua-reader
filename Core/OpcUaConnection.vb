Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Opc.Ua
Imports Opc.Ua.Client

Public Class OpcUaConnection

    Private _session As ISession
    Private _config As ApplicationConfiguration
    Private _subscription As Subscription
    Private _reconnectHandler As SessionReconnectHandler
    Private ReadOnly _handlerLock As New Object()
    Private ReadOnly _gate As New SemaphoreSlim(1, 1)
    Private _isProcessingTrigger As Integer = 0 ' 0=false, 1=true — use Interlocked
    Private ReadOnly _logger As FileLogger

    ' Auto-reconnect timing (ms). ReconnectPeriod is the initial/backoff base;
    ' ReconnectMaxPeriod caps the exponential backoff used by SessionReconnectHandler.
    Private Const ReconnectPeriod As Integer = 5000
    Private Const ReconnectMaxPeriod As Integer = 60000

    ' Hard deadline (ms) for graceful disconnect teardown. On a dead channel the
    ' close/delete service calls can otherwise each wait OperationTimeout (15s);
    ' this bounds manual Reconnect/Disconnect/shutdown so the UI never stalls.
    Private Const DisconnectTimeoutMs As Integer = 3000

    Public Property ServerConfig As ServerConfig
    Public Property IsConnected As Boolean = False

    Public Event DataTriggered(serverName As String, readValues As Dictionary(Of String, DataValue))
    Public Event ConnectionLost(serverName As String)
    Public Event ConnectionRestored(serverName As String)

    Public Sub New(serverConfig As ServerConfig, logger As FileLogger)
        Me.ServerConfig = serverConfig
        _logger = logger
    End Sub

    Private Sub Log(message As String)
        Try
            _logger?.Log(ServerConfig.Name, message)
        Catch
        End Try
    End Sub

    ' ─────────────────────────────────────────
    ' CONNECT
    ' ─────────────────────────────────────────
    Public Async Function ConnectAsync() As Task
        Await _gate.WaitAsync()
        Try
            Await ConnectCoreAsync()
        Finally
            _gate.Release()
        End Try
    End Function

    Private Async Function ConnectCoreAsync() As Task
        If _config Is Nothing Then
            _config = Await CreateApplicationConfigurationAsync()
        End If

        ' Step1 - discover endpoints
        Dim uri = New Uri(ServerConfig.EndpointUrl)
        Dim discoveryClient As DiscoveryClient = Nothing
        Dim endpoints As EndpointDescriptionCollection = Nothing

        discoveryClient = Await DiscoveryClient.CreateAsync(uri, Nothing, DiagnosticsMasks.None, CancellationToken.None)

        Try
            endpoints = Await discoveryClient.GetEndpointsAsync(Nothing)
        Finally
            Try
                discoveryClient.CloseAsync(CancellationToken.None).GetAwaiter().GetResult()
            Catch
                discoveryClient.Dispose()
            End Try
        End Try

        If endpoints Is Nothing OrElse endpoints.Count = 0 Then
            Throw New Exception($"No endpoints found at {ServerConfig.EndpointUrl}")
        End If

        ' Pick endpoint with lowest security (None) - change index or filter as needed
        Dim endpointDescription = endpoints.OrderBy(Function(ep) ep.SecurityLevel).FirstOrDefault()

        ' Step2 - build ConfiguredEndpoint
        Dim endpointConfig = EndpointConfiguration.Create(_config)
        Dim endpoint = New ConfiguredEndpoint(Nothing, endpointDescription, endpointConfig)

        ' Step3 - create session
        ' Use ISessionFactory.CreateAsync (replaces obsolete Session.Create)
        Dim factory As ISessionFactory = New DefaultSessionFactory(Nothing)
        _session = Await factory.CreateAsync(
            _config,
            endpoint,
            False,
            False,
            ServerConfig.Name,
            60000UI,
            Nothing,
            Nothing)

        IsConnected = (_session IsNot Nothing) AndAlso _session.Connected

        If IsConnected Then
            _session.KeepAliveInterval = 5000
            ' Reactivate the same session on reconnect so the PLC keeps a single
            ' session (the S7-1200 allows only ~2-4). TransferSubscriptions is left
            ' OFF because the S7-1200 doesn't support it; after a reconnect the
            ' subscription is re-established explicitly in EnsureSubscriptionAsync,
            ' which avoids racing the SDK for subscription ownership.
            _session.TransferSubscriptionsOnReconnect = False
            AddHandler _session.KeepAlive, AddressOf OnKeepAlive
            Log($"Connected to {ServerConfig.EndpointUrl}")
            Await CheckTriggerInitialStateAsync()
            Await SetupSubscriptionAsync()
            Log($"Subscription created: trigger={ServerConfig.TriggerNodeId}, interval=500ms")
        End If
    End Function

    ' ─────────────────────────────────────────
    ' INITIAL TRIGGER CHECK
    ' ─────────────────────────────────────────
    Private Async Function CheckTriggerInitialStateAsync() As Task
        Try
            Dim nodesToRead = New ReadValueIdCollection()
            nodesToRead.Add(New ReadValueId() With {
                .NodeId = New NodeId(ServerConfig.TriggerNodeId),
                .AttributeId = Attributes.Value
            })

            Dim response = Await _session.ReadAsync(Nothing, 0, TimestampsToReturn.Both, nodesToRead, CancellationToken.None)

            If response IsNot Nothing AndAlso response.Results IsNot Nothing AndAlso response.Results.Count > 0 Then
                Dim val = response.Results(0).Value
                Dim intVal As Integer = 0
                Try
                    If val IsNot Nothing Then intVal = Convert.ToInt32(val)
                Catch
                End Try

                If intVal = 1 AndAlso Interlocked.CompareExchange(_isProcessingTrigger, 1, 0) = 0 Then
                    Dim readResults = Await ReadMultipleNodesAsync(ServerConfig.NodesToReadOnTrigger)
                    RaiseEvent DataTriggered(ServerConfig.Name, readResults)
                End If
            End If
        Catch ex As Exception
            Log($"Initial trigger check error: {ex.Message}")
        End Try
    End Function

    ' ─────────────────────────────────────────
    ' SUBSCRIPTION SETUP
    ' ─────────────────────────────────────────
    Private Async Function SetupSubscriptionAsync() As Task
        _subscription = New Subscription(CType(Nothing, ITelemetryContext)) With {
            .DisplayName = $"Sub_{ServerConfig.Name}",
            .PublishingInterval = 500
        }

        ' Optionally copy default subscription settings from session
        Try
            If _session.DefaultSubscription IsNot Nothing Then
                _subscription.PublishingEnabled = _session.DefaultSubscription.PublishingEnabled
                _subscription.Priority = _session.DefaultSubscription.Priority
            End If
        Catch
        End Try

        _session.AddSubscription(_subscription)
        Await _subscription.CreateAsync()

        Dim triggerItem As New MonitoredItem(CType(Nothing, ITelemetryContext)) With {
            .DisplayName = "TriggerVariable",
            .StartNodeId = New NodeId(ServerConfig.TriggerNodeId),
            .AttributeId = Attributes.Value,
            .SamplingInterval = 500,
            .QueueSize = 1,
            .DiscardOldest = True
        }

        AddHandler triggerItem.Notification, AddressOf OnTriggerNotification

        _subscription.AddItems({triggerItem})
        Await _subscription.ApplyChangesAsync()
    End Function

    ' ─────────────────────────────────────────
    ' SUBSCRIPTION CALLBACK
    ' ─────────────────────────────────────────
    Private Sub OnTriggerNotification(item As MonitoredItem, e As MonitoredItemNotificationEventArgs)
        Try
            Dim notification = TryCast(e.NotificationValue, MonitoredItemNotification)
            If notification Is Nothing Then Return

            Dim val = notification.Value.Value
            Dim intVal As Integer = 0
            Try
                If val IsNot Nothing Then intVal = Convert.ToInt32(val)
            Catch
            End Try

            If intVal = 1 AndAlso Interlocked.CompareExchange(_isProcessingTrigger, 1, 0) = 0 Then
                Task.Run(Async Function()
                             Try
                                 Dim readResults = Await ReadMultipleNodesAsync(ServerConfig.NodesToReadOnTrigger)
                                 RaiseEvent DataTriggered(ServerConfig.Name, readResults)
                             Catch ex As Exception
                                 Log($"Trigger handler error: {ex.Message}")
                                 Interlocked.Exchange(_isProcessingTrigger, 0)
                             End Try
                         End Function)
            End If

        Catch ex As Exception
            Log($"Notification error: {ex.Message}")
        End Try
    End Sub

    ' ─────────────────────────────────────────
    ' WRITE 0 TO TRIGGER VARIABLE
    ' ─────────────────────────────────────────
    Public Async Function WriteTriggerResetAsync() As Task
        Try
            ' Read current value to determine data type
            Dim nodesToRead = New ReadValueIdCollection()
            nodesToRead.Add(New ReadValueId() With {
                .NodeId = New NodeId(ServerConfig.TriggerNodeId),
                .AttributeId = Attributes.Value
            })

            Dim readResp = Await _session.ReadAsync(Nothing, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None)
            Dim currentValue As Object = Nothing
            If readResp IsNot Nothing AndAlso readResp.Results IsNot Nothing AndAlso readResp.Results.Count > 0 Then
                currentValue = readResp.Results(0).Value
            End If

            ' Determine value to write matching the node's type
            Dim valueToWrite As Object = 0
            Try
                If currentValue Is Nothing Then
                    valueToWrite = CInt(0)
                ElseIf TypeOf currentValue Is Boolean Then
                    valueToWrite = False
                Else
                    Dim targetType = currentValue.GetType()
                    ' If targetType is enum, use underlying type
                    If targetType.IsEnum Then
                        targetType = System.Enum.GetUnderlyingType(targetType)
                    End If
                    valueToWrite = Convert.ChangeType(0, targetType)
                End If
            Catch
                ' Fallback to Int32 zero
                valueToWrite = CInt(0)
            End Try

            Dim nodeToWrite = New WriteValueCollection()
            Dim write = New WriteValue()
            write.NodeId = New NodeId(ServerConfig.TriggerNodeId)
            write.AttributeId = Attributes.Value
            Dim dv = New DataValue()
            dv.Value = valueToWrite
            dv.StatusCode = StatusCodes.Good
            write.Value = dv

            nodeToWrite.Add(write)

            Dim response = Await _session.WriteAsync(Nothing, nodeToWrite, CancellationToken.None)

            If response IsNot Nothing AndAlso response.Results IsNot Nothing AndAlso response.Results.Count > 0 Then
                If StatusCode.IsGood(response.Results(0)) Then
                    Interlocked.Exchange(_isProcessingTrigger, 0)
                    Log("Trigger reset to 0.")
                Else
                    Log($"Reset failed: {response.Results(0)}")
                End If
            Else
                Log("Empty write response — trigger not reset.")
            End If

        Catch ex As Exception
            Log($"Write error: {ex.Message}")
        End Try
    End Function

    ' ─────────────────────────────────────────
    ' READ MULTIPLE NODES
    ' ─────────────────────────────────────────
    Public Async Function ReadMultipleNodesAsync(nodes As List(Of NodeConfig)) As Task(Of Dictionary(Of String, DataValue))
        Dim results = New Dictionary(Of String, DataValue)()
        If Not IsConnected OrElse nodes Is Nothing OrElse nodes.Count = 0 Then Return results

        Dim nodesToRead = New ReadValueIdCollection()
        For Each n In nodes
            nodesToRead.Add(New ReadValueId() With {
                .NodeId = New NodeId(n.NodeId),
                .AttributeId = Attributes.Value
            })
        Next

        Dim response = Await _session.ReadAsync(
            Nothing, 0, TimestampsToReturn.Both, nodesToRead, CancellationToken.None)

        If response IsNot Nothing AndAlso response.Results IsNot Nothing Then
            Dim maxIndex As Integer = nodes.Count - 1
            Dim responseMax As Integer = response.Results.Count - 1
            If responseMax < maxIndex Then
                maxIndex = responseMax
            End If
            For i As Integer = 0 To maxIndex
                Dim label = If(String.IsNullOrEmpty(nodes(i).DisplayName), nodes(i).NodeId, nodes(i).DisplayName)
                results(label) = response.Results(i)
            Next
        End If

        Return results
    End Function

    ' ─────────────────────────────────────────
    ' KEEP-ALIVE WATCHDOG
    ' ─────────────────────────────────────────
    Private Sub OnKeepAlive(sender As ISession, e As KeepAliveEventArgs)
        If sender.KeepAliveStopped OrElse StatusCode.IsBad(e.Status) Then
            Dim wasConnected = IsConnected
            IsConnected = False
            ' Notify the UI only once per outage, not on every dead keep-alive tick.
            If wasConnected Then
                RaiseEvent ConnectionLost(ServerConfig.Name)
            End If
            ' Self-healing: if no reconnect cycle is running (e.g. a previous
            ' cycle ended without restoring the session), start a new one.
            ' StartReconnect is a no-op while a cycle is already active, so this
            ' is what eventually rescues a client stuck on "Reconnecting".
            StartReconnect()
        End If
    End Sub

    ' ─────────────────────────────────────────
    ' AUTO RECONNECT
    ' Reactivates the existing session over a new secure channel via the SDK
    ' SessionReconnectHandler, so the PLC keeps a single session instead of
    ' accumulating duplicates on every drop (the S7-1200 allows only ~2-4).
    ' The subscription is re-bound explicitly in EnsureSubscriptionAsync after a
    ' reconnect, since the S7-1200 has no TransferSubscriptions service.
    ' ─────────────────────────────────────────
    Private Sub StartReconnect()
        SyncLock _handlerLock
            If _reconnectHandler IsNot Nothing Then Return
            If _session Is Nothing Then Return
            Try
                _reconnectHandler = New SessionReconnectHandler(
                    CType(Nothing, ITelemetryContext), True, ReconnectMaxPeriod)
                _reconnectHandler.BeginReconnect(_session, ReconnectPeriod, AddressOf OnReconnectComplete)
                Log($"Auto-reconnect started (period={ReconnectPeriod}ms, max={ReconnectMaxPeriod}ms).")
            Catch ex As Exception
                Log($"Failed to start auto-reconnect: {ex.Message}")
                CancelReconnectHandler()
            End Try
        End SyncLock
    End Sub

    Private Async Sub OnReconnectComplete(sender As Object, e As EventArgs)
        Dim restored As Boolean = False

        SyncLock _handlerLock
            Dim handler = TryCast(sender, SessionReconnectHandler)
            ' Ignore callbacks from a handler we already discarded.
            If handler Is Nothing OrElse handler IsNot _reconnectHandler Then Return

            Dim newSession = _reconnectHandler.Session
            CancelReconnectHandler()

            If newSession IsNot Nothing Then
                _session = newSession
                RemoveHandler _session.KeepAlive, AddressOf OnKeepAlive
                AddHandler _session.KeepAlive, AddressOf OnKeepAlive
                IsConnected = _session.Connected
                restored = IsConnected
            End If
        End SyncLock

        If Not restored Then
            Log("Auto-reconnect completed without a live session; re-arming reconnect cycle.")
            StartReconnect()
            Return
        End If

        ' The session was reactivated. On a transient drop the existing subscription
        ' is still bound to the same session object and keeps working; on a longer
        ' outage the session was recreated, so rebuild the subscription on it. The
        ' S7-1200 has no TransferSubscriptions, so this is how we keep triggers alive.
        Try
            Await EnsureSubscriptionAsync()
        Catch ex As Exception
            Log($"Subscription reconciliation after reconnect failed: {ex.Message}")
        End Try

        Log("Auto-reconnect succeeded — session reactivated, subscription active.")
        RaiseEvent ConnectionRestored(ServerConfig.Name)
    End Sub

    ' After a reconnect, makes sure exactly one active subscription is bound to the
    ' current session. No-op on the common reactivation path (same session object);
    ' rebuilds the subscription when the session was recreated.
    Private Async Function EnsureSubscriptionAsync() As Task
        If _subscription IsNot Nothing AndAlso
           _subscription.Session Is _session AndAlso _subscription.Created Then
            Return
        End If

        If _subscription IsNot Nothing Then
            Try
                _subscription.Dispose()
            Catch
            End Try
            _subscription = Nothing
        End If

        Await SetupSubscriptionAsync()
        Log("Subscription re-established on reactivated session.")
    End Function

    ' Cancels and disposes any in-flight reconnect handler. Safe to call under
    ' _handlerLock (SyncLock/Monitor is reentrant on the calling thread).
    Private Sub CancelReconnectHandler()
        SyncLock _handlerLock
            If _reconnectHandler Is Nothing Then Return
            Try
                _reconnectHandler.CancelReconnect()
            Catch
            End Try
            Try
                _reconnectHandler.Dispose()
            Catch
            End Try
            _reconnectHandler = Nothing
        End SyncLock
    End Sub

    ' ─────────────────────────────────────────
    ' DISCONNECT
    ' ─────────────────────────────────────────
    Public Async Function DisconnectAsync() As Task
        Await _gate.WaitAsync()
        Try
            Await DisconnectCoreAsync()
        Finally
            _gate.Release()
        End Try
    End Function

    Private Async Function DisconnectCoreAsync() As Task
        CancelReconnectHandler()
        Using cts = New CancellationTokenSource(DisconnectTimeoutMs)
            Try
                If _subscription IsNot Nothing Then
                    ' Disable publishing to reduce server-side notifications during shutdown
                    Try
                        _subscription.PublishingEnabled = False
                        Await _subscription.ApplyChangesAsync(cts.Token)
                    Catch exPub As Exception
                        Log($"Failed to disable publishing: {exPub}")
                    End Try

                    Try
                        Await _subscription.DeleteAsync(True, cts.Token)
                    Catch ex As OperationCanceledException
                        ' teardown timed out (e.g. dead channel) — fall through to dispose
                    Catch ex As ServiceResultException
                        Log($"Subscription delete error: {ex}")
                    Catch ex As Exception
                        Log($"Subscription delete error: {ex}")
                    Finally
                        Try
                            _subscription.Dispose()
                        Catch
                        End Try
                        _subscription = Nothing
                    End Try
                End If

                If _session IsNot Nothing Then
                    Try
                        RemoveHandler _session.KeepAlive, AddressOf OnKeepAlive
                    Catch
                    End Try
                    Try
                        Await _session.CloseAsync(0, True, cts.Token)
                    Catch ex As OperationCanceledException
                        ' teardown timed out (e.g. dead channel) — fall through to dispose
                    Catch ex As ServiceResultException
                        Log($"Session close error: {ex}")
                    Catch ex As Exception
                        Log($"Session close error: {ex}")
                    Finally
                        Try
                            _session.Dispose()
                        Catch
                        End Try
                        _session = Nothing
                    End Try
                End If
            Catch ex As Exception
                Log($"Disconnect error: {ex}")
            Finally
                IsConnected = False
            End Try
        End Using
    End Function

    ' ─────────────────────────────────────────
    ' RECONNECT
    ' ─────────────────────────────────────────
    Public Async Function ReconnectAsync() As Task
        Await _gate.WaitAsync()
        Try
            CancelReconnectHandler()
            Await DisconnectCoreAsync()
            Await ConnectCoreAsync()
        Finally
            _gate.Release()
        End Try
    End Function

    Private Async Function CreateApplicationConfigurationAsync() As Task(Of ApplicationConfiguration)
        Dim config = New ApplicationConfiguration() With {
            .ApplicationName = "MultiServer OPC UA Client",
            .ApplicationType = ApplicationType.Client,
            .SecurityConfiguration = New SecurityConfiguration() With {
                .ApplicationCertificate = New CertificateIdentifier() With {
                    .StoreType = "Directory",
                    .StorePath = "pki/own",
                    .SubjectName = $"CN=MultiServer OPC UA Client, O={Environment.MachineName}"
                },
                .TrustedPeerCertificates = New CertificateTrustList() With {
                    .StoreType = "Directory",
                    .StorePath = "pki/trusted"
                },
                .TrustedIssuerCertificates = New CertificateTrustList() With {
                    .StoreType = "Directory",
                    .StorePath = "pki/issuer"
                },
                .RejectedCertificateStore = New CertificateStoreIdentifier() With {
                    .StoreType = "Directory",
                    .StorePath = "pki/rejected"
                },
                .AutoAcceptUntrustedCertificates = True,
                .AddAppCertToTrustedStore = True
            },
            .TransportConfigurations = New TransportConfigurationCollection(),
            .TransportQuotas = New TransportQuotas() With {.OperationTimeout = 15000},
            .ClientConfiguration = New ClientConfiguration() With {.DefaultSessionTimeout = 60000}
        }
        Await config.ValidateAsync(ApplicationType.Client)
        Return config
    End Function

End Class