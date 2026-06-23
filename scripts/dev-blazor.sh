#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJECT="$ROOT_DIR/src/Api/SafetyScale.Api.csproj"
BLAZOR_PROJECT="$ROOT_DIR/src/Web.Blazor/SafetyScale.Web.Blazor.csproj"
API_URL="http://localhost:5003"
BLAZOR_URL="http://localhost:4864"
API_PID=""

log() {
  printf '\n==> %s\n' "$1"
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    printf 'Erro: comando "%s" nao encontrado no PATH.\n' "$1" >&2
    exit 127
  fi
}

cleanup() {
  if [[ -n "$API_PID" ]] && kill -0 "$API_PID" 2>/dev/null; then
    log "Encerrando API (PID $API_PID)"
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi
}

wait_for_api() {
  local max_attempts="${1:-45}"
  local attempt=0

  while (( attempt < max_attempts )); do
    if curl -s -o /dev/null -w '' "$API_URL/api/health" 2>/dev/null; then
      return 0
    fi

    if ! kill -0 "$API_PID" 2>/dev/null; then
      printf 'Erro: processo da API encerrou antes de ficar disponivel.\n' >&2
      return 1
    fi

    sleep 1
    (( attempt++ )) || true
  done

  printf 'Erro: API nao respondeu em %s apos %ss.\n' "$API_URL" "$max_attempts" >&2
  return 1
}

trap cleanup EXIT INT TERM

require_command dotnet
require_command curl

log "Subindo API em $API_URL (background)"
dotnet run --project "$API_PROJECT" &
API_PID=$!

log "Aguardando API ficar disponivel"
wait_for_api

log "Subindo Blazor WASM em $BLAZOR_URL (foreground; Ctrl+C encerra API e Blazor)"
printf 'Abra %s apos o build WASM concluir.\n' "$BLAZOR_URL"
dotnet run --project "$BLAZOR_PROJECT"
