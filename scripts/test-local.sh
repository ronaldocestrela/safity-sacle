#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEB_DIR="$ROOT_DIR/src/Web"
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

if [ "${SKIP_REACT_TESTS:-0}" = "1" ]; then
  log "SKIP_REACT_TESTS=1 — testes React omitidos (gate Blazor ate B11)"
  log "Todos os testes .NET passaram"
  exit 0
fi

require_command npm

if [ ! -d "$WEB_DIR/node_modules" ]; then
  log "Instalando dependencias do frontend React (legado)"
  npm ci --prefix "$WEB_DIR"
fi

log "Executando testes do frontend React (nao bloqueante ate B11)"
if npm run test --prefix "$WEB_DIR"; then
  log "Testes React passaram"
else
  printf '\nAVISO: testes React falharam — nao bloqueia cutover Blazor (B10.3). Defina SKIP_REACT_TESTS=1 para omitir.\n' >&2
fi

log "Gate principal (.NET) concluido com sucesso"
