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
require_command npm

log "Restaurando dependencias do backend (.NET solution)"
dotnet restore "$SOLUTION"

log "Executando testes do backend"
dotnet test "$BACKEND_TEST_PROJECT" --configuration Release --no-restore

if [ ! -d "$WEB_DIR/node_modules" ]; then
  log "Instalando dependencias do frontend"
  npm ci --prefix "$WEB_DIR"
fi

log "Executando testes do frontend"
npm run test --prefix "$WEB_DIR"

log "Todos os testes passaram"
