# Omniroute — контекст для Claude

WinUI 3 клієнт для Windows 11 для моніторингу й керування зарядними станціями EcoFlow.
Порт Android-застосунку PowerHub: https://github.com/t1me2drum/Ecoflow-mon-android
(коли логіка протоколів неясна, еталоном є він).

## Стек
- .NET 8, `net8.0-windows10.0.22621.0`, WinUI 3 / Windows App SDK 1.5, платформи x64 та ARM64
- Збирається як звичайний .exe без MSIX (`WindowsPackageType=None`) з вбудованим Windows App SDK (`WindowsAppSDKSelfContained`): не потрібні сертифікат, режим розробника і встановлений Windows App Runtime
- MQTTnet 4.3.x (телеметрія й команди), EF Core Sqlite (історія), Google.Protobuf, CommunityToolkit.Mvvm
- UI українською, коментарі в коді українською

## Збирання
```bash
dotnet build -c Release -p:Platform=x64
```
Результат: `bin/x64/Release/net8.0-windows10.0.22621.0/win-x64/Omniroute.exe` (запускати разом з усією папкою).
Дані застосунку лежать у `%LocalAppData%\Omniroute\`: `settings.json` (налаштування, станції, зашифровані облікові дані), `history.db`, `crash.log`.
Через те що пакета немає, `ApplicationData`/`Windows.Storage` не використовуємо, лише `Data/LocalStore`.

## Структура
- `Api/` — `EcoflowCloud` (вхід, REST), `EcoflowOpenApi` (Developer API з підписом HMAC), `MqttLink` (MQTT-клієнт з власним перепідключенням)
- `Protocol/` — `IDeviceProtocol`, реєстр `Protocols.For(model)` і протоколи, перенесені з Android-версії один в один:
  `Delta2Family` → `Delta2Protocol`, `Delta2MaxProtocol`, `DeltaMaxProtocol`, `River2MaxProtocol` (JSON); `Delta3Protocol` (Standard/Max) і `DeltaPro3Protocol` (protobuf через `ProtoCodec`). Схеми — `Protocol/Proto/*.proto`, C#-код із них заздалегідь згенеровано в `Protocol/Proto/Generated` (див. README там)
- `Data/` — `Repository` (потокобезпечний, подія `DevicesChanged`, синхронізація станцій), `CredentialStore` (DPAPI: email, пароль, сервер, ключі Developer API), `DeviceStore`, `SettingsStore`, `HistoryDbContext` (SQLite, новий контекст на кожну операцію)
- `Services/` — `MonitorService` (з'єднання, телеметрія, історія, статус), `NotificationService` (правила сповіщень `Evaluate` + toast)
- `Views/` — Login, Devices, DeviceDetails, Settings. Логіка поки лежить у code-behind; `ViewModels/` порожня

## Як влаштований моніторинг
- MQTT-топіки станції: `/app/device/property/{sn}` (телеметрія), `/app/{userId}/{sn}/thing/property/get|set` (запити) і `…/get_reply|set_reply`
- Формат JSON: телеметрія у `params`, відповідь `latestQuotas` у `data.quotaMap`
- Потоки: MQTT-колбеки й таймер працюють у фонових потоках; властивості `Device` (INotifyPropertyChanged, прив'язані до UI) змінюються **тільки** через DispatcherQueue в UI-потоці
- Токен і облікові дані MQTT не зберігаються: при кожному підключенні виконується новий вхід

## Домовленості
- Протокол кожної моделі реалізує окремий клас `IDeviceProtocol`; модель визначається за префіксом серійного номера (таблиця в README)
- Облікові дані не логувати й не комітити. `*.db` і `*.pfx` мають бути в `.gitignore`
- Прогрес і наступні кроки ведемо в `docs/PROGRESS.md`. Наприкінці сесії треба оновити його і зробити коміт із пушем

## Робота на кількох комп'ютерах
Історія сесій Claude між комп'ютерами не синхронізується, тому весь контекст проєкту має бути в цьому файлі та в `docs/PROGRESS.md`.

## Граф знань (graphify)
- `graphify-out/` лежить у репозиторії: `graph.json`, `graph.html` (відкривається в браузері), `GRAPH_REPORT.md`, семантичний кеш документів
- На питання про архітектуру спершу відповідати через `graphify query "<питання>"`, а не читати файли підряд
- Після помітних змін у коді оновити граф: `/graphify . --update`. Згенерований protoc-код виключено через `.graphifyignore`
