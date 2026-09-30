#!/bin/sh
# Перевірка C#-коду без Windows (Linux, хмарні сесії): XAML-компілятор тут не працює,
# тож genstubs.py генерує заглушки полів x:Name і перевірки обробників подій та шляхів x:Bind,
# а Check.csproj компілює весь код проти справжніх збірок Windows App SDK.
# Потрібен .NET 8 SDK. Сам XAML (розмітка, x:Bind до властивостей цілей) не перевіряється.
set -e
cd "$(dirname "$0")"
mkdir -p Stubs
python3 genstubs.py ../.. Stubs
printf 'namespace PowerHub;\npublic static class __Program { [System.STAThread] public static void Main() { } }\n' > Stubs/Program.g.cs
# Успіх — рядок «Check -> …PowerHubCheck.dll» без «error CS». Помилки MSB4062 після нього очікувані:
# це кроки пакування (PRI/AppX), для яких потрібен Windows-інструментарій
dotnet build -c Release -nologo -clp:NoSummary 2>&1 | grep -E "error|warning CS|PowerHubCheck.dll" || true
