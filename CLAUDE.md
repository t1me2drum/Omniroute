# Omniroute — контекст для Claude

WinUI 3 клієнт для Windows 11 для моніторингу й керування зарядними станціями EcoFlow.
Порт Android-застосунку PowerHub: https://github.com/t1me2drum/Ecoflow-mon-android
(коли логіка протоколів неясна, еталоном є він).

## Стек
- .NET 8, `net8.0-windows10.0.22621.0`, WinUI 3 / Windows App SDK 1.5, пакування MSIX, платформи x64 та ARM64
- MQTTnet 4.3.x (телеметрія й команди), EF Core Sqlite (історія), Google.Protobuf, CommunityToolkit.Mvvm
- UI українською, коментарі в коді українською

## Збирання
```bash
dotnet restore
dotnet build -p:Platform=x64
```
Потрібна Visual Studio 2022 17.8+ з Windows App SDK або Windows 11 SDK 10.0.22621.

## Структура
- `Api/` — `EcoflowCloud` (вхід, REST), `EcoflowOpenApi` (Developer API з підписом HMAC), `MqttLink` (MQTT-клієнт з власним перепідключенням)
- `Protocol/` — `IDeviceProtocol` + реалізації для кожної моделі (поки тільки `Delta2Protocol`), `JsonMessages`, `ProtocolHelpers`
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
