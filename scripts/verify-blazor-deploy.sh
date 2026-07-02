#!/usr/bin/env bash
# Verificacao HTTP pos-deploy Blazor (B10.2 / B10.4).
set -Eeuo pipefail

WEB_PORT="${WEB_PORT:-80}"
BASE_URL="${BLAZOR_VERIFY_BASE_URL:-http://127.0.0.1:${WEB_PORT}}"

log() {
  printf '==> %s\n' "$1"
}

fail() {
  printf 'ERROR: %s\n' "$1" >&2
  exit 1
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    fail "comando \"$1\" nao encontrado no PATH"
  fi
}

require_command curl

check_http() {
  local path="$1"
  local expected="$2"
  local url="${BASE_URL}${path}"
  local code
  code="$(curl -s -o /dev/null -w '%{http_code}' "$url" || true)"
  if [ "$code" != "$expected" ]; then
    fail "${path} esperado HTTP ${expected}, obteve ${code:-000}"
  fi
  log "${path} OK (HTTP ${code})"
}

log "Verificando deploy Blazor em ${BASE_URL}"

check_http "/" "200"
check_http "/api/health" "401"

# Asset WASM ou bootstrap Blazor
WASM_CODE="$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/_framework/blazor.webassembly.js" || true)"
BOOT_CODE="$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/_framework/dotnet.js" || true)"

if [ "$WASM_CODE" != "200" ] && [ "$BOOT_CODE" != "200" ]; then
  fail "_framework: blazor.webassembly.js (${WASM_CODE}) e dotnet.js (${BOOT_CODE}) — esperado 200 em pelo menos um"
fi

if [ "$WASM_CODE" = "200" ]; then
  log "/_framework/blazor.webassembly.js OK (HTTP 200)"
fi
if [ "$BOOT_CODE" = "200" ]; then
  log "/_framework/dotnet.js OK (HTTP 200)"
fi

log "Verificacao Blazor concluida com sucesso"
