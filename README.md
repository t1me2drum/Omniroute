# Omniroute — Windows-клієнт для моніторингу зарядних станцій EcoFlow

Моніторинг і керування зарядними станціями EcoFlow на Windows 11: заряд, мережа, сповіщення про відключення світла, графіки.

## Підтримувані моделі

| Модель | Префікс SN | Протокол |
|---|---|---|
| Delta 2 | `R331` | JSON |
| Delta 2 Max | `R351` | JSON |
| Delta Max | `DA` | JSON (команди `TCP`) |
| River 2 Max | `R611` | JSON |
| Delta 3 / Delta 3 Max | `P231` | protobuf |
| Delta Pro 3 | `MR51` | protobuf (телеметрія) + JSON (команди) |

## Можливості

- **Вхід** з обліковим записом EcoFlow (той самий, що в офіційному застосунку)
- **Список станцій з акаунта:** автоматична синхронізація через EcoFlow Developer API
- **Моніторинг у реальному часі** через MQTT: заряд, вхід і вихід, мережа, температура
- **Керування:** виходи AC, DC і USB, X-Boost, ліміти заряду й розряду
- **Сповіщення Windows:** зникло або з'явилося живлення, низький заряд, повний заряд
- **Графіки** заряду й потужності за різні періоди
- **Світла та темна теми** відповідно до налаштувань Windows

## Вимоги

- Windows 11 (версія 22H2 або новіша)
- .NET 8.0 або новіший
- Windows App SDK 1.5+

## Встановлення

1. Завантажте останню версію з [Releases](../../releases/latest)
2. Встановіть MSIX пакет
3. Увійдіть з email і паролем EcoFlow
4. Налаштуйте доступ до API через developer.ecoflow.com

## Збирання

### Вимоги для розробки
- Visual Studio 2022 (17.8 або новіша) з компонентами:
  - .NET Desktop Development
  - Windows App SDK C# Templates
- Windows 11 SDK (10.0.22621.0 або новіша)

### Кроки збирання
```bash
git clone https://github.com/t1me2drum/Omniroute.git
cd Omniroute
dotnet restore
dotnet build
```

## Структура проекту

```
Omniroute/
  ├── Api/              EcoFlow API клієнти (REST і MQTT)
  ├── Protocol/         Протоколи пристроїв (Delta2, Delta3, DeltaPro3)
  ├── Data/             Repository, Settings, History database
  ├── Services/         Фоновий моніторинг і сповіщення
  ├── ViewModels/       MVVM View Models
  ├── Views/            WinUI 3 екрани
  ├── Models/           Моделі даних
  └── Assets/           Ресурси (іконки, зображення)
```

## Технології

- **WinUI 3** — сучасний UI framework для Windows
- **Windows App SDK** — нативні API Windows 11
- **MVVM** — архітектурний патерн
- **Entity Framework Core** — база даних SQLite
- **MQTTnet** — MQTT клієнт
- **Google.Protobuf** — підтримка protobuf протоколів

## Ліцензія

Цей проект є портом [PowerHub Android застосунку](https://github.com/t1me2drum/Ecoflow-mon-android)
