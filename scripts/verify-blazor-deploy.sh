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

# Assets _framework sao fingerprintados no publish (.NET 10+); descobrir paths em index.html
INDEX_HTML="$(curl -s "${BASE_URL}/" || true)"
if [ -z "$INDEX_HTML" ]; then
  fail "nao foi possivel obter index.html de ${BASE_URL}/"
fi

BLAZOR_SCRIPT="$(printf '%s' "$INDEX_HTML" | sed -n 's/.*<script src="\(_framework\/blazor\.webassembly[^"]*\.js\)".*/\1/p' | head -1)"
DOTNET_SCRIPT="$(printf '%s' "$INDEX_HTML" | sed -n 's|.*"\./_framework/dotnet\.js": "\./\(_framework/[^"]*\.js\)".*|\1|p' | head -1)"

if [ -z "$BLAZOR_SCRIPT" ]; then
  fail "index.html nao referencia _framework/blazor.webassembly*.js"
fi

check_http "/${BLAZOR_SCRIPT}" "200"

if [ -n "$DOTNET_SCRIPT" ]; then
  DOTNET_CODE="$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/${DOTNET_SCRIPT}" || true)"
  if [ "$DOTNET_CODE" = "200" ]; then
    log "/${DOTNET_SCRIPT} OK (HTTP 200)"
  else
    fail "/${DOTNET_SCRIPT} esperado HTTP 200, obteve ${DOTNET_CODE:-000}"
  fi
fi

log "Verificacao Blazor concluida com sucesso"
