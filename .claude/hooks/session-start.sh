#!/bin/bash
# Підготовка хмарної сесії Claude Code: graphify (граф знань) і .NET 8 SDK (тести й tools/check-build).
# Локально (Windows) нічого не робить — там усе встановлено вручну.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# graphify: та сама версія, що й на ПК (скіл лежить у .claude/skills/graphify)
if ! command -v graphify >/dev/null 2>&1; then
  pip install --quiet --root-user-action=ignore graphifyy==0.9.72
fi

# .NET 8 SDK з архіву Ubuntu (dot.net через проксі недоступний)
if ! command -v dotnet >/dev/null 2>&1; then
  apt-get update -qq >/dev/null 2>&1 || true
  DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-8.0 >/dev/null
fi

# Пакети NuGet для тестів — заздалегідь, щоб перший dotnet test не чекав
dotnet restore "$CLAUDE_PROJECT_DIR/tests/PowerHub.Tests" --verbosity quiet >/dev/null
