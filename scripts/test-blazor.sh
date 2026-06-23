#!/usr/bin/env bash
# Gate B10.1 — testes bUnit do Web.Blazor (auth, guards, 4 módulos).
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT="$ROOT_DIR/src/Tests/SafetyScale.Tests.csproj"
MIN_TESTS="${BLAZOR_MIN_UNIT_TESTS:-50}"

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

log "Restaurando solution"
dotnet restore "$ROOT_DIR/SafetyScale.sln"

log "Executando testes bUnit Web.Blazor"
OUTPUT="$(dotnet test "$TEST_PROJECT" \
  --configuration Release \
  --no-restore \
  --filter "FullyQualifiedName~SafetyScale.Tests.Web.Blazor" \
  --logger "console;verbosity=normal" 2>&1)" || {
  printf '%s\n' "$OUTPUT" >&2
  exit 1
}

printf '%s\n' "$OUTPUT"

PASSED="$(printf '%s\n' "$OUTPUT" | sed -n 's/.*Passed: *\([0-9]*\).*/\1/p' | tail -1)"
if [ -z "$PASSED" ] || [ "$PASSED" -lt "$MIN_TESTS" ]; then
  printf 'Erro: esperado >= %s testes Web.Blazor; obtido: %s\n' "$MIN_TESTS" "${PASSED:-0}" >&2
  exit 1
fi

log "Web.Blazor: ${PASSED} testes passaram (minimo ${MIN_TESTS})"
