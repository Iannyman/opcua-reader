# OPC UA Reader

A multi-server OPC UA client with a dark-themed WinForms dashboard. Connects to multiple PLCs simultaneously, monitors trigger variables via subscriptions, reads a configurable set of nodes when the trigger fires, and inserts the results into SQL Server via stored procedure.

Built with VB.NET, .NET Framework 4.8, and the [OPC Foundation UA .NET Standard](https://github.com/OPCFoundation/UA-.NETStandard) library.

## Features

- **Multi-server** — connect to any number of OPC UA servers in parallel, each with its own tab
- **Trigger-based reading** — subscribes to a trigger node and batch-reads configured nodes when the value becomes 1
- **SQL Server integration** — builds a JSON payload from read values and calls a stored procedure (`dbo.DC_insert_ZSB_data`) after each trigger, with status shown in the status bar
- **Auto-reconnect** — monitors server health via keep-alive and reconnects automatically on failure
- **Per-server control** — connect, disconnect, reconnect, and clear logs independently per PLC
- **Custom dark scrollbar** — themed scrollbar that matches the dark UI
- **JSON configuration** — all server endpoints, node IDs, SQL connection, and display names configured via `settings.json`

## Requirements

- Visual Studio 2017+ with .NET Framework 4.8
- OPC UA servers accessible over the network
- SQL Server (Express or higher) with the `DC_insert_ZSB_data` stored procedure

## Getting Started

1. Clone the repository
2. Open `opcua_reader.sln` in Visual Studio
3. Build the solution (Debug or Release)
4. Copy `settings.json.example` to `settings.json` and edit it to match your setup
5. Run the application

## Configuration

All configuration lives in `settings.json` (placed next to the executable). Copy the example file and fill in your values:

```json
{
  "SqlConnectionString": "Server=YOUR_SERVER\\SQLEXPRESS,1433;Database=DATABASE_NAME;User Id=YOUR_USER;Password=YOUR_PASSWORD;",
  "Servers": [
    {
      "Name": "PLC_Line_1",
      "EndpointUrl": "opc.tcp://192.168.1.100:4840",
      "TriggerNodeId": "ns=4;i=16",
      "SwoIdentNode": "ns=4;i=3",
      "EquipIdNode": "ns=4;i=5",
      "NodesToReadOnTrigger": [
        { "NodeId": "ns=4;i=2", "DisplayName": "DMC" },
        { "NodeId": "ns=4;i=3", "DisplayName": "PartNr" },
        { "NodeId": "ns=4;i=4", "DisplayName": "TimeStamp" },
        { "NodeId": "ns=4;i=5", "DisplayName": "Station" },
        { "NodeId": "ns=4;i=6", "DisplayName": "Torque1" },
        { "NodeId": "ns=4;i=7", "DisplayName": "Angle1" },
        { "NodeId": "ns=4;i=8", "DisplayName": "Torque2" },
        { "NodeId": "ns=4;i=9", "DisplayName": "Angle2" },
        { "NodeId": "ns=4;i=10", "DisplayName": "Angle lower limit" },
        { "NodeId": "ns=4;i=11", "DisplayName": "Angle upper limit" },
        { "NodeId": "ns=4;i=12", "DisplayName": "Result" },
        { "NodeId": "ns=4;i=13", "DisplayName": "Status NOK" },
        { "NodeId": "ns=4;i=18", "DisplayName": "OrderNr" }
      ]
    },
    {
      "Name": "PLC_Line_2",
      "EndpointUrl": "opc.tcp://192.168.1.101:4840",
      "TriggerNodeId": "ns=4;i=16",
      "SwoIdentNode": "ns=4;i=3",
      "EquipIdNode": "ns=4;i=5",
      "NodesToReadOnTrigger": [
        { "NodeId": "ns=4;i=2", "DisplayName": "DMC" },
        { "NodeId": "ns=4;i=3", "DisplayName": "PartNr" },
        { "NodeId": "ns=4;i=4", "DisplayName": "TimeStamp" },
        { "NodeId": "ns=4;i=5", "DisplayName": "Station" },
        { "NodeId": "ns=4;i=6", "DisplayName": "Torque1" },
        { "NodeId": "ns=4;i=7", "DisplayName": "Angle1" },
        { "NodeId": "ns=4;i=8", "DisplayName": "Torque2" },
        { "NodeId": "ns=4;i=9", "DisplayName": "Angle2" },
        { "NodeId": "ns=4;i=10", "DisplayName": "Angle lower limit" },
        { "NodeId": "ns=4;i=11", "DisplayName": "Angle upper limit" },
        { "NodeId": "ns=4;i=12", "DisplayName": "Result" },
        { "NodeId": "ns=4;i=13", "DisplayName": "Status NOK" },
        { "NodeId": "ns=4;i=18", "DisplayName": "OrderNr" }
      ]
    }
  ]
}
```

| Field                  | Description                                                                                                                 |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `SqlConnectionString`  | ADO.NET connection string for the SQL Server database                                                                       |
| `Name`                 | Display name shown in the tab and overview grid                                                                             |
| `EndpointUrl`          | OPC UA server endpoint                                                                                                      |
| `TriggerNodeId`        | Node ID to subscribe to; reading triggers when its value becomes `1`                                                        |
| `SwoIdentNode`         | Node ID whose value is extracted as `SWO_IDENT` in the SQL payload                                                          |
| `EquipIdNode`          | Node ID whose value is extracted as `EQUIP_ID` in the SQL payload                                                           |
| `NodesToReadOnTrigger` | Array of nodes to batch-read on trigger. Nodes matching `SwoIdentNode` or `EquipIdNode` are excluded from the `nodes` array |

### SQL Payload Format

When a trigger fires, the app builds this JSON and passes it to `dbo.DC_insert_ZSB_data`:

```json
{
  "PLC_ID": "PLC_Line_1",
  "SWO_IDENT": "304-497",
  "EQUIP_ID": "23017",
  "nodes": [
    { "nodeId": "ns=4;i=2", "value": "19191D8B4F6426118304495" },
    { "nodeId": "ns=4;i=4", "value": "2026/5/8 13:10:2" },
    { "nodeId": "ns=4;i=6", "value": "2500.000" },
    { "nodeId": "ns=4;i=7", "value": "0.000" },
    { "nodeId": "ns=4;i=8", "value": "1048.000" },
    { "nodeId": "ns=4;i=9", "value": "1.930" },
    { "nodeId": "ns=4;i=10", "value": "992.000" },
    { "nodeId": "ns=4;i=11", "value": "2.010" },
    { "nodeId": "ns=4;i=12", "value": "OK" },
    { "nodeId": "ns=4;i=13", "value": "0" },
    { "nodeId": "ns=4;i=18", "value": "106056900" }
  ]
}
```

The stored procedure returns `{"success":1,"message":"..."}`. Results and errors are logged to the server's log tab.

## Architecture

```
MainForm (UI — MaterialSkin dark theme)
  ├─ SqlService (SQL Server connection + stored procedure calls)
  └─ OpcUaManager (orchestrator — manages multiple connections)
       └─ OpcUaConnection (per-server OPC UA session + subscription)
            └─ ServerConfig / NodeConfig (data models from settings.json)
```

- `Core/SqlService.vb` — opens a SqlConnection and calls `dbo.DC_insert_ZSB_data` with a JSON payload; tests connection on startup
- `Core/OpcUaManager.vb` — manages a dictionary of named connections, aggregates connect/disconnect, forwards `DataTriggered` events to the UI
- `Core/OpcUaConnection.vb` — handles session lifecycle, endpoint discovery, subscription setup, trigger monitoring, and batch node reads
- `Controls/ThemedListBox.vb` — custom `UserControl` wrapping a `ListBox` with a dark-themed scrollbar
- `MainForm.vb` — dashboard UI with overview grid, per-server log tabs, and SQL status indicator

## Dependencies

- [OPC Foundation UA .NET Standard](https://www.nuget.org/packages/OPCFoundation.NetStandard.Opc.Ua/) 1.5.378
- [MaterialSkin.2](https://www.nuget.org/packages/MaterialSkin.2/) 2.3.1
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) 13.0.4
- [BouncyCastle.Cryptography](https://www.nuget.org/packages/BouncyCastle.Cryptography/) 2.6.2
- `System.Data.SqlClient` (built-in with .NET Framework 4.8)

## License

This project is provided as-is for internal use.
