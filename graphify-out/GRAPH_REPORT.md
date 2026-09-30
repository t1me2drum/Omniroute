# Graph Report - PowerHub  (2026-09-30)

## Corpus Check
- Corpus is ~29,162 words - fits in a single context window. You may not need a graph.

## Summary
- 1069 nodes · 2084 edges · 49 communities (41 shown, 8 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 91 edges (avg confidence: 0.83)
- Token cost: 57,568 input · 0 output

## Community Hubs (Navigation)
- Project Namespaces & Entry
- Device Details Page UI
- Formatting & State Logic
- History Database & Charts
- MQTT Message Parsing
- Devices List Page
- Station Tile & SoC Ring
- EcoFlow Cloud Login API
- Device Telemetry Model
- Command Sending & Optimism
- Win32 Tray Icon
- Project Docs & Conventions
- App Lifecycle & Single Instance
- Delta 2 Family Protocols
- App Settings Model
- MQTT Link Connection
- Delta 3 Protobuf Protocol
- Toggle Controls
- Settings Page UI
- Local Store & Migration
- Device Model Registry
- Protocol Interface & Choices
- Settings Page Handlers
- DPAPI Credential Store
- Repository Device Sync
- Async Repository Operations
- Main Project Packages
- Slider Controls
- Settings Sliders
- Notification Service
- Protobuf Protocol Tests
- Check Build Packages
- XAML Stub Generator
- Diagnostic Log
- Delta Pro 3 Protocol
- Test Project Packages
- Monitor Connect Loop
- Notification Toggles
- App Logo Assets
- History Query API
- Connection State Enum
- Theme & Layout Selection
- Alerts & Background Features
- Autostart Registry
- Settings Persistence
- Main Window Frame
- Reconnect & Re-login Policy
- Linux Check Script

## God Nodes (most connected - your core abstractions)
1. `Device` - 65 edges
2. `DeviceState` - 43 edges
3. `DeviceParams` - 39 edges
4. `MonitorService` - 38 edges
5. `Repository` - 33 edges
6. `Page` - 33 edges
7. `Page` - 32 edges
8. `DeviceDetailsPage` - 31 edges
9. `TrayIcon` - 30 edges
10. `StationTile` - 30 edges

## Surprising Connections (you probably didn't know these)
- `ProtoCodec (HeaderMessage frames, XOR pdata, reflection field unwrap, SetCommand)` --shares_data_with--> `Protobuf schemas ef_delta3.proto and ef_dp3.proto`  [INFERRED]
  docs/PROGRESS.md → Protocol/Proto/README.md
- `MonitorService` --references--> `MqttLink`  [EXTRACTED]
  Services/MonitorService.cs → Api/MqttLink.cs
- `App` --inherits--> `Application`  [EXTRACTED]
  App.xaml.cs → App.xaml
- `App` --references--> `Repository`  [EXTRACTED]
  App.xaml.cs → Data/Repository.cs
- `App` --references--> `TrayIcon`  [EXTRACTED]
  App.xaml.cs → Services/TrayIcon.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **UWP/MSIX App Visual Asset Set (lightning bolt branding)** — assets_splashscreen_splashscreen, assets_square150x150logo_square150x150logo, assets_square44x44logo_square44x44logo, assets_storelogo_storelogo, assets_wide310x150logo_wide310x150logo [INFERRED 0.85]
- **MQTT monitoring flow (topics, message parsing, polling, reconnect)** — claude_mqtt_topics, docs_progress_mqtt_message_format_fix, docs_progress_latestquotas_polling, docs_progress_reconnect_backoff, claude_fresh_login_per_connect [INFERRED 0.85]
- **Protobuf protocol stack for Delta 3 / Delta Pro 3** — protocol_proto_readme_protobuf_schemas, protocol_proto_readme_pregenerated_csharp, docs_progress_protocodec, readme_supported_models [INFERRED 0.85]
- **Omniroute to PowerHub rename and data migration** — claude_omniroute_legacy_name, docs_progress_legacy_folder_migration, claude_localstore_appdata, readme_dpapi_credentials [EXTRACTED 1.00]

## Communities (49 total, 8 thin omitted)

### Community 0 - "Project Namespaces & Entry"
Cohesion: 0.06
Nodes (53): communitytoolkit_mvvm_componentmodel, PowerHub.Protocol, PowerHub.Services, PowerHub.Data, PowerHub.Api, PowerHub.Models, PowerHub.Tests, PowerHub.Views (+45 more)

### Community 1 - "Device Details Page UI"
Cohesion: 0.05
Nodes (48): Grid, AcInText, AcOutText, CommandErrorBar, ControlsPanel, CyclesText, DcOutText, DeviceModelText (+40 more)

### Community 2 - "Formatting & State Logic"
Cohesion: 0.05
Nodes (34): CultureInfo, Format, DeviceState, AcInVolt, AcInW, AcOutW, BatteryTempC, ChargeRemainMin (+26 more)

### Community 3 - "History Database & Charts"
Cohesion: 0.05
Nodes (42): Border, HistoryDbContext, History, DbContext, DbContextOptionsBuilder, DbSet, Hours, ModelBuilder (+34 more)

### Community 4 - "MQTT Message Parsing"
Cohesion: 0.08
Nodes (20): Dictionary, FieldDescriptor, IMessage, MessageParser, DeviceParams, TopicKind, Data, GetReply (+12 more)

### Community 5 - "Devices List Page"
Cohesion: 0.07
Nodes (28): DragItemsCompletedEventArgs, ItemClickEventArgs, ListViewBase, ObservableCollection, SyncResult, ConnectionBar, DevicesList, EmptyPanel (+20 more)

### Community 6 - "Station Tile & SoC Ring"
Cohesion: 0.05
Nodes (38): Button, Control, Ellipse, FontIcon, Geometry, Grid, Id, Path (+30 more)

### Community 7 - "EcoFlow Cloud Login API"
Cohesion: 0.07
Nodes (32): HttpClient, JsonElement, Task, EcoflowCloud, EcoflowException, IsAuthError, MqttCredentials, Session (+24 more)

### Community 8 - "Device Telemetry Model"
Cohesion: 0.05
Nodes (36): DateTime, Device, BatteryLevel, BatteryPercent, BatteryText, CardGridAlertText, CardPowerText, CardStatusText (+28 more)

### Community 9 - "Command Sending & Optimism"
Cohesion: 0.10
Nodes (16): Command, Optimistic, CancellationTokenSource, DateTime, Dictionary, DispatcherQueue, Func, HashSet (+8 more)

### Community 10 - "Win32 Tray Icon"
Cohesion: 0.15
Nodes (11): Guid, NOTIFYICONDATA, POINT, DllImport, IntPtr, NOTIFYICONDATA, POINT, TrayIcon (+3 more)

### Community 11 - "Project Docs & Conventions"
Cohesion: 0.08
Nodes (31): Android PowerHub app (Ecoflow-mon-android, reference implementation), graphify knowledge graph workflow (graphify-out, query before reading), Linux verification (dotnet test + tools/check-build/check.sh with genstubs.py), %LocalAppData%\PowerHub data folder (settings.json, history.db, diag.log), EcoFlow MQTT topics (/app/device/property/{sn}, thing/property/get|set, get_reply|set_reply), Cross-computer context kept in CLAUDE.md and docs/PROGRESS.md, Omniroute (legacy project name before 0.4.0), PowerHub (WinUI 3 EcoFlow client) (+23 more)

### Community 12 - "App Lifecycle & Single Instance"
Cohesion: 0.10
Nodes (16): Application, DispatcherQueue, Exception, App, IsExiting, MainWindow, Repository, AppWindow (+8 more)

### Community 13 - "Delta 2 Family Protocols"
Cohesion: 0.14
Nodes (19): Dictionary, IEnumerable, List, Delta2Family, SolarKeys, Delta2MaxProtocol, SolarKeys, Delta2Protocol (+11 more)

### Community 14 - "App Settings Model"
Cohesion: 0.08
Nodes (27): AppSettings, AccessKey, CloseToTray, Layout, LowBatteryThreshold, NotificationsEnabled, NotifyOnFullCharge, NotifyOnLowBattery (+19 more)

### Community 15 - "MQTT Link Connection"
Cohesion: 0.11
Nodes (16): Action, CancellationToken, CancellationTokenSource, HashSet, IEnumerable, Task, TimeSpan, MqttLink (+8 more)

### Community 16 - "Delta 3 Protobuf Protocol"
Cohesion: 0.15
Nodes (7): Delta3SetCommand, Frame, HashSet, List, Delta3Protocol, OutgoingMessage, Payload

### Community 17 - "Toggle Controls"
Cohesion: 0.11
Nodes (19): Func, ControlSection, Backup, Charging, Outputs, System, ToggleControl, Command (+11 more)

### Community 18 - "Settings Page UI"
Cohesion: 0.12
Nodes (20): AccessKeyBox, ConnectionBar, DiagText, EmailText, HiddenCard, HiddenList, KeysMessage, KeysProgress (+12 more)

### Community 19 - "Local Store & Migration"
Cohesion: 0.15
Nodes (7): List, DeviceStore, Dictionary, LocalStore, Default, SettingsStore, Lazy

### Community 20 - "Device Model Registry"
Cohesion: 0.15
Nodes (11): DeviceModel, Delta2, Delta2Max, Delta3, Delta3Max, Delta3Plus, DeltaMax, DeltaPro3 (+3 more)

### Community 21 - "Protocol Interface & Choices"
Cohesion: 0.12
Nodes (12): Label, List, Value, ChoiceControl, Command, Id, Label, Optimistic (+4 more)

### Community 22 - "Settings Page Handlers"
Cohesion: 0.21
Nodes (6): ClearKeysButton, CloseToTrayToggle, SaveKeysButton, RoutedEventArgs, SettingsPage, Button

### Community 23 - "DPAPI Credential Store"
Cohesion: 0.19
Nodes (8): CredentialStore, Credentials, AccessKey, ApiHost, Email, HasDeveloperKeys, Password, SecretKey

### Community 24 - "Repository Device Sync"
Cohesion: 0.18
Nodes (9): IEnumerable, Repository, ActiveDevices, Credentials, Devices, HiddenDevices, IsLoggedIn, Settings (+1 more)

### Community 25 - "Async Repository Operations"
Cohesion: 0.27
Nodes (3): Task, Task, SyncResult

### Community 26 - "Main Project Packages"
Cohesion: 0.18
Nodes (11): net8.0-windows10.0.22621.0, CommunityToolkit.Mvvm (8.3.2), Google.Protobuf (3.28.3), Microsoft.EntityFrameworkCore.Sqlite (8.0.10), Microsoft.Windows.SDK.BuildTools (10.0.22621.3233), Microsoft.WindowsAppSDK (1.5.240802000), MQTTnet (4.3.7.1207), System.Security.Cryptography.ProtectedData (8.0.0) (+3 more)

### Community 27 - "Slider Controls"
Cohesion: 0.18
Nodes (11): SliderControl, Command, Id, Label, Max, Min, Optimistic, Read (+3 more)

### Community 28 - "Settings Sliders"
Cohesion: 0.25
Nodes (5): RangeBaseValueChangedEventArgs, LowBatterySlider, WeakGridSlider, Visibility, Slider

### Community 29 - "Notification Service"
Cohesion: 0.24
Nodes (6): AlertMemory, AppNotificationActivatedEventArgs, AppNotificationManager, Dictionary, AlertMemory, NotificationService

### Community 31 - "Check Build Packages"
Cohesion: 0.20
Nodes (9): net8.0-windows10.0.22621.0, CommunityToolkit.Mvvm (8.3.2), Google.Protobuf (3.28.3), Microsoft.EntityFrameworkCore.Sqlite (8.0.10), Microsoft.Windows.SDK.BuildTools (10.0.22621.3233), Microsoft.WindowsAppSDK (1.5.240802000), MQTTnet (4.3.7.1207), System.Security.Cryptography.ProtectedData (8.0.0) (+1 more)

### Community 32 - "XAML Stub Generator"
Cohesion: 0.25
Nodes (8): glob, os, re, sys, ctype(), Заглушки замість XAML-компілятора: поля x:Name, InitializeComponent, перевірка…, walk(), xml_etree_elementtree

### Community 34 - "Delta Pro 3 Protocol"
Cohesion: 0.50
Nodes (3): HashSet, List, DeltaPro3Protocol

### Community 35 - "Test Project Packages"
Cohesion: 0.29
Nodes (6): net8.0, Microsoft.NET.Test.Sdk (17.11.1), xunit (2.9.2), xunit.runner.visualstudio (2.8.2), Google.Protobuf (3.28.3), Microsoft.NET.Sdk

### Community 37 - "Notification Toggles"
Cohesion: 0.48
Nodes (6): FullChargeToggle, LowBatteryToggle, NotificationsToggle, OfflineToggle, PowerLossToggle, ToggleSwitch

### Community 38 - "App Logo Assets"
Cohesion: 0.33
Nodes (6): Lightning Bolt Brand Mark (white on green), SplashScreen (green background, white lightning bolt), Square150x150Logo (medium tile), Square44x44Logo (app list / taskbar icon), StoreLogo (package/store icon), Wide310x150Logo (wide tile)

### Community 39 - "History Query API"
Cohesion: 0.33
Nodes (4): DateTime, List, DateTime, List

### Community 40 - "Connection State Enum"
Cohesion: 0.33
Nodes (6): ConnectionState, Connected, Connecting, Failed, Reconnecting, Stopped

### Community 41 - "Theme & Layout Selection"
Cohesion: 0.40
Nodes (4): LayoutButtons, ThemeButtons, SelectionChangedEventArgs, RadioButtons

### Community 42 - "Alerts & Background Features"
Cohesion: 0.50
Nodes (5): Notification rules fire once with hysteresis (AlertEngine port, 5 V weak-grid hysteresis), Background mode: tray icon, close-to-tray, autostart, Battery state by energy flow (charging/discharging/idle/full), Weak grid detection (voltage below 180 V threshold), Windows notifications (power lost/restored, weak voltage, low/full charge, offline)

### Community 45 - "Main Window Frame"
Cohesion: 0.50
Nodes (3): RootFrame, Window, Frame

## Knowledge Gaps
- **234 isolated node(s):** `IsAuthError`, `IsConnected`, `MainWindow`, `Repository`, `IsExiting` (+229 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 412 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Device` connect `Device Telemetry Model` to `Project Namespaces & Entry`, `Device Details Page UI`, `Formatting & State Logic`, `Devices List Page`, `Station Tile & SoC Ring`, `Command Sending & Optimism`, `Local Store & Migration`, `Device Model Registry`, `Repository Device Sync`, `Notification Service`?**
  _High betweenness centrality (0.222) - this node is a cross-community bridge._
- **Why does `Repository` connect `Repository Device Sync` to `Project Namespaces & Entry`, `History Query API`, `Device Telemetry Model`, `App Lifecycle & Single Instance`, `Settings Persistence`, `App Settings Model`, `Local Store & Migration`, `Device Model Registry`, `DPAPI Credential Store`, `Async Repository Operations`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **Why does `DeviceDetailsPage` connect `Device Details Page UI` to `Project Namespaces & Entry`, `Device Telemetry Model`, `MQTT Message Parsing`, `EcoFlow Cloud Login API`?**
  _High betweenness centrality (0.118) - this node is a cross-community bridge._
- **Are the 14 inferred relationships involving `DeviceState` (e.g. with `.Charging_UsesOnlyChargeEstimate()` and `.Discharging_UsesOnlyDischargeEstimate()`) actually correct?**
  _`DeviceState` has 14 INFERRED edges - model-reasoned connections that need verification._
- **Are the 6 inferred relationships involving `DeviceParams` (e.g. with `.Delta2AcToggle_BuildsDocumentedCommand()` and `.Delta2WithoutGridVoltage_HasNoGrid()`) actually correct?**
  _`DeviceParams` has 6 INFERRED edges - model-reasoned connections that need verification._
- **What connects `IsAuthError`, `IsConnected`, `MainWindow` to the rest of the system?**
  _234 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Project Namespaces & Entry` be split into smaller, more focused modules?**
  _Cohesion score 0.06131320064058568 - nodes in this community are weakly interconnected._