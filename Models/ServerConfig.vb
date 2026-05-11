Public Class ServerConfig

    Public Property Name As String
    Public Property EndpointUrl As String

    Public Property UserName As String = ""
    Public Property Password As String = ""
    Public Property SecurityMode As String = "None"

    ' The variable that will be subscribed and watched for value = 1
    Public Property TriggerNodeId As String

    ' Node IDs whose values are extracted as top-level payload fields (excluded from nodes array)
    Public Property SwoIdentNode As String = ""
    Public Property EquipIdNode As String = ""

    ' The variables to READ when trigger becomes 1
    Public Property NodesToReadOnTrigger As List(Of NodeConfig) = New List(Of NodeConfig)()

End Class