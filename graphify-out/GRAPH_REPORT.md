# Graph Report - Omniroute  (2026-09-28)

## Corpus Check
- Corpus is ~16,047 words - fits in a single context window. You may not need a graph.

## Summary
- 660 nodes · 1227 edges · 42 communities (38 shown, 4 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 59 edges (avg confidence: 0.83)
- Token cost: 72,726 input · 0 output

## Community Hubs (Navigation)
- Namespaces & File Imports
- EcoFlow Cloud Login API
- Settings Screen
- Device Telemetry Model
- History Database
- Station List Screen
- Delta 2 JSON Family
- Local Storage & Exe Build
- App Settings Model
- Delta 3 Protobuf Protocol
- Monitor Service Core
- Notification Rules
- Device Details Layout
- MQTT Link & Topics
- Station Models & Detection
- Normalized Device State
- MQTT Connect & Subscribe
- Repository Data Hub
- Control Factories
- Encrypted Credentials
- Param Readers & GetState
- App Shell & Crash Log
- Choice Control
- Build & NuGet Dependencies
- History Stats Model
- Message Parsing & Topic Kinds
- Slider Control
- Monitor Connect Loop
- MQTT JSON Format
- Device Details Logic
- Quota Requests
- Delta Pro 3 Protocol
- Toggle Control
- Protobuf Flattening
- Monitor Tick & Quotas
- MSIX Visual Assets
- Project Context & Design
- Unverified Model Protocols
- Settings Persistence
- Proto Schemas & Codegen
- MVVM TODO

## God Nodes (most connected - your core abstractions)
1. `Device` - 41 edges
2. `MonitorService` - 36 edges
3. `DeviceParams` - 30 edges
4. `Repository` - 29 edges
5. `MqttLink` - 26 edges
6. `DeviceState` - 25 edges
7. `OutgoingMessage` - 21 edges
8. `ChoiceControl` - 19 edges
9. `NotificationService` - 19 edges
10. `HistoryEntry` - 18 edges

## Surprising Connections (you probably didn't know these)
- `GetControls (station control elements)` --conceptually_related_to--> `IDeviceProtocol`  [INFERRED]
  docs/PROGRESS.md → Protocol/DeviceProtocol.cs
- `Fresh login on every MQTT connection (no token persistence)` --conceptually_related_to--> `EcoflowCloud`  [INFERRED]
  CLAUDE.md → Api/EcoflowCloud.cs
- `Station list sync via EcoFlow Developer API` --conceptually_related_to--> `EcoflowOpenApi`  [INFERRED]
  README.md → Api/EcoflowOpenApi.cs
- `TODO: history charts` --conceptually_related_to--> `HistoryDbContext`  [INFERRED]
  docs/PROGRESS.md → Data/HistoryDbContext.cs
- `Delta3Max appended to end of DeviceModel enum` --conceptually_related_to--> `LocalStore`  [INFERRED]
  docs/PROGRESS.md → Data/LocalStore.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **UWP/MSIX App Visual Asset Set (lightning bolt branding)** — assets_splashscreen_splashscreen, assets_square150x150logo_square150x150logo, assets_square44x44logo_square44x44logo, assets_storelogo_storelogo, assets_wide310x150logo_wide310x150logo [INFERRED 0.85]
- **Delta2Family JSON protocol implementations** — protocol_delta2protocol_omniroute_protocol_delta2protocol, protocol_delta2protocol_omniroute_protocol_delta2maxprotocol, protocol_legacyjsonprotocols_omniroute_protocol_deltamaxprotocol, protocol_legacyjsonprotocols_omniroute_protocol_river2maxprotocol, protocol_delta2protocol_omniroute_protocol_delta2family [EXTRACTED 1.00]
- **Protobuf telemetry pipeline (schemas, ProtoCodec, Delta3/DeltaPro3)** — protocol_proto_readme_ef_delta3_proto, protocol_proto_readme_ef_dp3_proto, protocol_protocodec_omniroute_protocol_protocodec, protocol_delta3protocol_omniroute_protocol_delta3protocol, protocol_deltapro3protocol_omniroute_protocol_deltapro3protocol [EXTRACTED 1.00]
- **MQTT monitoring pipeline** — api_ecoflowcloud_omniroute_api_ecoflowcloud, api_mqttlink_omniroute_api_mqttlink, services_monitorservice_omniroute_services_monitorservice, protocol_jsonmessages_omniroute_protocol_jsonmessages, models_device_omniroute_models_device, services_notificationservice_omniroute_services_notificationservice [INFERRED 0.85]

## Communities (42 total, 4 thin omitted)

### Community 0 - "Namespaces & File Imports"
Cohesion: 0.08
Nodes (39): communitytoolkit_mvvm_componentmodel, Omniroute.Protocol, Omniroute.Services, Omniroute.Api, Omniroute.Models, Omniroute.Views, Omniroute, Omniroute.Data (+31 more)

### Community 1 - "EcoFlow Cloud Login API"
Cohesion: 0.06
Nodes (32): HttpClient, JsonElement, Task, EcoflowCloud, EcoflowException, IsAuthError, MqttCredentials, Session (+24 more)

### Community 2 - "Settings Screen"
Cohesion: 0.09
Nodes (24): Page, SelectionChangedEventArgs, AccessKeyBox, EmailText, FullChargeToggle, KeysProgress, LowBatteryToggle, NotificationsToggle (+16 more)

### Community 3 - "Device Telemetry Model"
Cohesion: 0.07
Nodes (29): Device updates only on UI thread via DispatcherQueue, DateTime, Device, BatteryLevel, BatteryPercent, BatteryText, BatteryWatts, Cycles (+21 more)

### Community 4 - "History Database"
Cohesion: 0.09
Nodes (20): DateTime, List, Task, HistoryDbContext, History, DbContext, DbContextOptionsBuilder, DbSet (+12 more)

### Community 5 - "Station List Screen"
Cohesion: 0.12
Nodes (15): ObservableCollection, DevicesRepeater, DevicesScrollViewer, EmptyPanel, LoadingPanel, Page, StatusText, RoutedEventArgs (+7 more)

### Community 6 - "Delta 2 JSON Family"
Cohesion: 0.18
Nodes (15): Dictionary, IEnumerable, List, Delta2Family, SolarKeys, Delta2Protocol, SolarKeys, IControl (+7 more)

### Community 7 - "Local Storage & Exe Build"
Cohesion: 0.13
Nodes (10): Unpackaged self-contained exe (WindowsPackageType=None), IEnumerable, List, DeviceStore, Dictionary, LocalStore, Default, SettingsStore (+2 more)

### Community 8 - "App Settings Model"
Cohesion: 0.10
Nodes (20): AppSettings, AccessKey, LowBatteryThreshold, NotificationsEnabled, NotifyOnFullCharge, NotifyOnLowBattery, NotifyOnOffline, NotifyOnPowerLoss (+12 more)

### Community 9 - "Delta 3 Protobuf Protocol"
Cohesion: 0.16
Nodes (7): Delta3SetCommand, Frame, HashSet, Delta3Protocol, List, Frame, ProtoCodec

### Community 10 - "Monitor Service Core"
Cohesion: 0.15
Nodes (12): DispatcherQueue, TODO: keep monitoring after window close, CancellationTokenSource, DateTime, Dictionary, HashSet, TimeSpan, MonitorService (+4 more)

### Community 11 - "Notification Rules"
Cohesion: 0.18
Nodes (9): AlertMemory, AppNotificationActivatedEventArgs, AppNotificationBuilder, AppNotificationManager, TODO: clicking notification opens station, Windows toast notifications (power loss/restore, low/full charge), Dictionary, AlertMemory (+1 more)

### Community 12 - "Device Details Layout"
Cohesion: 0.16
Nodes (16): BatteryLevelText, BatteryProgress, CyclesText, DeviceModelText, DeviceNameText, InputText, OnlineIndicator, OutputText (+8 more)

### Community 13 - "MQTT Link & Topics"
Cohesion: 0.12
Nodes (13): Action, CancellationTokenSource, HashSet, TimeSpan, MqttLink, IsConnected, EcoFlow MQTT topic scheme, GetControls (station control elements) (+5 more)

### Community 14 - "Station Models & Detection"
Cohesion: 0.14
Nodes (13): Model detection by serial-number prefix, DeviceModel, Delta2, Delta2Max, Delta3, Delta3Max, Delta3Plus, DeltaMax (+5 more)

### Community 15 - "Normalized Device State"
Cohesion: 0.12
Nodes (16): DeviceState, AcInVolt, AcInW, AcOutW, BatteryTempC, ChargeRemainMin, Cycles, DcOutW (+8 more)

### Community 16 - "MQTT Connect & Subscribe"
Cohesion: 0.19
Nodes (6): CancellationToken, IEnumerable, Task, IReadOnlyCollection, MqttApplicationMessageReceivedEventArgs, MqttClientConnectedEventArgs

### Community 17 - "Repository Data Hub"
Cohesion: 0.19
Nodes (11): DateTime, List, Task, Repository, ActiveDevices, Credentials, Devices, IsLoggedIn (+3 more)

### Community 18 - "Control Factories"
Cohesion: 0.19
Nodes (10): List, ControlSection, Backup, Charging, Outputs, System, Func, Label (+2 more)

### Community 19 - "Encrypted Credentials"
Cohesion: 0.19
Nodes (9): CredentialStore, DPAPI encryption of credentials and Developer API keys, Credentials, AccessKey, ApiHost, Email, HasDeveloperKeys, Password (+1 more)

### Community 20 - "Param Readers & GetState"
Cohesion: 0.24
Nodes (3): Dictionary, DeviceParams, DeviceParamsExtensions

### Community 21 - "App Shell & Crash Log"
Cohesion: 0.15
Nodes (9): Application, Exception, App, MainWindow, Repository, RootFrame, Window, MainWindow (+1 more)

### Community 22 - "Choice Control"
Cohesion: 0.17
Nodes (11): Label, List, Value, ChoiceControl, Command, Id, Label, Optimistic (+3 more)

### Community 23 - "Build & NuGet Dependencies"
Cohesion: 0.18
Nodes (11): net8.0-windows10.0.22621.0, CommunityToolkit.Mvvm (8.3.2), Google.Protobuf (3.28.3), Microsoft.EntityFrameworkCore.Sqlite (8.0.10), Microsoft.Windows.SDK.BuildTools (10.0.22621.3233), Microsoft.WindowsAppSDK (1.5.240802000), MQTTnet (4.3.7.1207), System.Security.Cryptography.ProtectedData (8.0.0) (+3 more)

### Community 24 - "History Stats Model"
Cohesion: 0.18
Nodes (11): DateTime, TimeSpan, HistoryStats, AverageInput, AverageOutput, EndTime, PowerOutageCount, StartTime (+3 more)

### Community 25 - "Message Parsing & Topic Kinds"
Cohesion: 0.22
Nodes (5): TopicKind, Data, GetReply, SetReply, IEnumerable

### Community 26 - "Slider Control"
Cohesion: 0.18
Nodes (11): SliderControl, Command, Id, Label, Max, Min, Optimistic, Read (+3 more)

### Community 27 - "Monitor Connect Loop"
Cohesion: 0.25
Nodes (3): LaunchActivatedEventArgs, CancellationToken, IEnumerable

### Community 28 - "MQTT JSON Format"
Cohesion: 0.36
Nodes (4): MQTT JSON format (params / data.quotaMap), JsonElement, List, JsonMessages

### Community 29 - "Device Details Logic"
Cohesion: 0.33
Nodes (3): PropertyChangedEventArgs, NavigationEventArgs, DeviceDetailsPage

### Community 30 - "Quota Requests"
Cohesion: 0.32
Nodes (3): IDeviceProtocol, OutgoingMessage, Payload

### Community 31 - "Delta Pro 3 Protocol"
Cohesion: 0.50
Nodes (3): HashSet, List, DeltaPro3Protocol

### Community 32 - "Toggle Control"
Cohesion: 0.25
Nodes (8): Func, ToggleControl, Command, Id, Label, Optimistic, Read, Section

### Community 33 - "Protobuf Flattening"
Cohesion: 0.38
Nodes (3): FieldDescriptor, IMessage, MessageParser

### Community 35 - "MSIX Visual Assets"
Cohesion: 0.33
Nodes (6): Lightning Bolt Brand Mark (white on green), SplashScreen (green background, white lightning bolt), Square150x150Logo (medium tile), Square44x44Logo (app list / taskbar icon), StoreLogo (package/store icon), Wide310x150Logo (wide tile)

### Community 36 - "Project Context & Design"
Cohesion: 0.33
Nodes (5): Omniroute Claude Context (CLAUDE.md), PowerHub Android App (reference implementation), Fire-once notification rules with hysteresis, MQTT reconnect with escalating backoff (5s to 5min), Protocols

### Community 37 - "Unverified Model Protocols"
Cohesion: 0.40
Nodes (5): TODO: verify on real stations (Delta Max, River 2 Max, Delta Pro 3, Delta 2 Max), Delta2MaxProtocol, SolarKeys, River2MaxProtocol, SolarKeys

### Community 39 - "Proto Schemas & Codegen"
Cohesion: 0.67
Nodes (4): ef_delta3.proto schema, ef_dp3.proto schema, tolwi/hassio-ecoflow-cloud (Apache-2.0), Pre-generated C# protobuf classes (protoc)

## Knowledge Gaps
- **180 isolated node(s):** `IsAuthError`, `IsConnected`, `MainWindow`, `Repository`, `History` (+175 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 270 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Device` connect `Device Telemetry Model` to `Namespaces & File Imports`, `Monitor Tick & Quotas`, `Station List Screen`, `Local Storage & Exe Build`, `Monitor Service Core`, `Notification Rules`, `Station Models & Detection`, `Repository Data Hub`, `Device Details Logic`?**
  _High betweenness centrality (0.180) - this node is a cross-community bridge._
- **Why does `Repository` connect `Repository Data Hub` to `Namespaces & File Imports`, `EcoFlow Cloud Login API`, `Device Telemetry Model`, `Project Context & Design`, `History Database`, `Settings Persistence`, `Local Storage & Exe Build`, `App Settings Model`, `Station Models & Detection`, `Encrypted Credentials`, `App Shell & Crash Log`?**
  _High betweenness centrality (0.161) - this node is a cross-community bridge._
- **Why does `MonitorService` connect `Monitor Service Core` to `Namespaces & File Imports`, `EcoFlow Cloud Login API`, `Monitor Tick & Quotas`, `Device Telemetry Model`, `Project Context & Design`, `MQTT Link & Topics`, `MQTT Connect & Subscribe`, `Monitor Connect Loop`?**
  _High betweenness centrality (0.105) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `MonitorService` (e.g. with `EcoFlow MQTT topic scheme` and `TODO: keep monitoring after window close`) actually correct?**
  _`MonitorService` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `IsAuthError`, `IsConnected`, `MainWindow` to the rest of the system?**
  _180 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Namespaces & File Imports` be split into smaller, more focused modules?**
  _Cohesion score 0.07598371777476255 - nodes in this community are weakly interconnected._
- **Should `EcoFlow Cloud Login API` be split into smaller, more focused modules?**
  _Cohesion score 0.06448202959830866 - nodes in this community are weakly interconnected._