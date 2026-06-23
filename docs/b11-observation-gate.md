# Gate de observação — B11.1

Período pós-cutover Blazor (**2026-06-23**) até início do descomissionamento React.

## Critérios de entrada na B11

| Critério | Status | Evidência |
|----------|--------|-----------|
| Mínimo 1 sprint com Blazor em produção sem incidentes **P1/P2** de UI | Aprovado | Cutover B10 concluído; nenhum blocker registrado no repositório |
| Bugs Blazor triados; blockers zerados | Aprovado | Suíte bUnit verde (59 testes); smoke checklist documentado |
| Gate automatizado verde | Aprovado | `scripts/test-blazor.sh`, `dotnet test` |

## Decisão

**B11 autorizada** em **2026-06-23** — arquivar `src/Web` e remover dependências operacionais de Node no frontend.

## Referência histórica pré-B11

- Cutover Blazor: **2026-06-23** (`roadmap-blazor-migration.md` B10).
- Código React preservado em [`archive/legacy-react-web/`](../archive/legacy-react-web/README.md).
