Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Opc.Ua
Imports Opc.Ua.Client

Public Class OpcUaConnection

    Private _session As ISession
    Private _config As ApplicationConfiguration
    Private _subscription As Subscription
    Private _isReconnecting As Boolean = False
    Private ReadOnly _logger As FileLogger

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
        _config = Await CreateApplicationConfigurationAsync()

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

                If intVal = 1 Then
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

            If intVal = 1 Then
                Task.Run(Async Function()
                             Try
                                 Dim readResults = Await ReadMultipleNodesAsync(ServerConfig.NodesToReadOnTrigger)
                                 RaiseEvent DataTriggered(ServerConfig.Name, readResults)
                             Catch ex As Exception
                                 Log($"Trigger handler error: {ex.Message}")
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
                    Log("Trigger reset to 0.")
                Else
                    Log($"Reset failed: {response.Results(0)}")
                End If
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
        If Not IsConnected Then Return
        If sender.KeepAliveStopped OrElse StatusCode.IsBad(e.Status) Then
            IsConnected = False
            RaiseEvent ConnectionLost(ServerConfig.Name)
            Task.Run(Async Function()
                         Await AutoReconnectLoopAsync()
                     End Function)
        End If
    End Sub

    Private Async Function AutoReconnectLoopAsync() As Task
        If _isReconnecting Then Return
        _isReconnecting = True
        Try
            While _isReconnecting
                Try
                    Await ReconnectAsync()
                    If IsConnected Then
                        RaiseEvent ConnectionRestored(ServerConfig.Name)
                        Return
                    End If
                Catch
                End Try
                Await Task.Delay(5000)
            End While
        Finally
            _isReconnecting = False
        End Try
    End Function

    ' ─────────────────────────────────────────
    ' DISCONNECT
    ' ─────────────────────────────────────────
    Public Async Function DisconnectAsync() As Task
        _isReconnecting = False
        Try
            If _subscription IsNot Nothing Then
                ' Disable publishing to reduce server-side notifications during shutdown
                Try
                    _subscription.PublishingEnabled = False
                    Await _subscription.ApplyChangesAsync()
                Catch exPub As Exception
                    Log($"Failed to disable publishing: {exPub}")
                End Try

                Try
                    Await _subscription.DeleteAsync(True)
                Catch ex As TaskCanceledException
                    ' ignore cancellation during shutdown
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
                    Await _session.CloseAsync()
                Catch ex As TaskCanceledException
                    ' ignore cancellation during shutdown
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
    End Function

    ' ─────────────────────────────────────────
    ' RECONNECT
    ' ─────────────────────────────────────────
    Public Async Function ReconnectAsync() As Task
        Await DisconnectAsync()
        Await ConnectAsync()
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