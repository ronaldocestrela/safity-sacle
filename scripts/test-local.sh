#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="$ROOT_DIR/SafetyScale.sln"
BACKEND_TEST_PROJECT="$ROOT_DIR/src/Tests/SafetyScale.Tests.csproj"

log() {
  printf '\n==> %s\n' "$1"
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    printf 'Erro: comando "%s" nao encontrado no PATH.\n' "$1" >&2
    exit 127
  fi
}

require_command dotnet

log "Restaurando dependencias do backend (.NET solution)"
dotnet restore "$SOLUTION"

log "Executando testes do backend (inclui bUnit Web.Blazor)"
dotnet test "$BACKEND_TEST_PROJECT" --configuration Release --no-restore

log "Todos os testes passaram"
