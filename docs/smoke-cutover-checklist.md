# Checklist de fumaça — cutover Blazor (B10.4)

Executar **antes do cutover em produção** (staging) e **novamente após o cutover**. Registrar evidências (data, executor, ambiente, resultado) na seção **Registro de execução** ao final.

**Pré-requisitos**

- Stack `docker compose -f docker-compose.prod.yml` (ou staging) com serviço **`web`** servindo Blazor WASM.
- API acessível via proxy `/api` no mesmo host (`ApiBaseUrl` vazio) **ou** via split-origin com `CORS_ORIGINS` configurado.
- Credenciais seed de desenvolvimento disponíveis apenas em ambientes não-prod; em produção use tenant criado via `/signup`.

---

## Gate automatizado (antes do smoke manual)

```bash
./scripts/test-blazor.sh          # bUnit mínimo (auth, guards, 4 módulos)
./scripts/verify-blazor-deploy.sh # HTTP: /, /api/health, asset WASM
```

---

## Smoke manual — perfil **Admin**

| # | Fluxo | Passos | OK |
|---|--------|--------|-----|
| A1 | Cadastro tenant | `/signup` → criar empresa → login com admin criado | [ ] |
| A2 | Login seed (dev/staging) | `admin@local.com` / senha seed → área `/app` | [ ] |
| A3 | Logout | Logout → `/app` redireciona para login | [ ] |
| A4 | Token expirado | Limpar `sessionStorage` ou aguardar expiração → redirect login com feedback | [ ] |
| A5 | Setores | `/app/sectors` — CRUD, filtros, ativar/inativar, `requiredGuardsPerDay` | [ ] |
| A6 | Seguranças | `/app/security-guards` — filtros, criar/editar, vínculos setores, inativar/reativar | [ ] |
| A7 | Indisponibilidades | `/app/unavailable-days` — marcar dias, **SAVE RESTRICTIONS** persiste | [ ] |
| A8 | Escalas | `/app/schedules` — consulta mês/ano; **Gerar escala**; erro `ScheduleCoverageFailed` legível | [ ] |
| A9 | Navegação | Bottom nav e ícone ativo corretos em todas as telas `/app/*` | [ ] |

---

## Smoke manual — perfil **Supervisor**

| # | Fluxo | Passos | OK |
|---|--------|--------|-----|
| S1 | Login | `supervisor@safetyscale.local` (dev) ou supervisor do tenant de teste | [ ] |
| S2 | Setores | `/app/sectors` — somente leitura (sem criar/editar/inativar) | [ ] |
| S3 | Seguranças | `/app/security-guards` — lista/filtros; sem ações Admin | [ ] |
| S4 | Indisponibilidades | `/app/unavailable-days` — consulta; sem save batch | [ ] |
| S5 | Escalas | `/app/schedules` — consulta; sem botão gerar | [ ] |
| S6 | Navegação | Bottom nav consistente com Admin | [ ] |

---

## Isolamento multitenant (manual)

| # | Fluxo | Passos | OK |
|---|--------|--------|-----|
| M1 | Tenant A vs B | Login tenant A → anotar dados visíveis; login tenant B → não ver dados de A | [ ] |

---

## Registro de execução

| Ambiente | Data | Executor | Admin (A1–A9) | Supervisor (S1–S6) | Multitenant (M1) | Observações |
|----------|------|----------|---------------|----------------------|------------------|-------------|
| Staging | | | | | | |
| Produção (pós-cutover) | | | | | | |

---

## Critério de aprovação

- Gate automatizado verde.
- **100%** dos itens aplicáveis marcados OK em staging **e** pós-cutover.
- Nenhum incidente **P1/P2** aberto atribuído à UI Blazor antes de encerrar B10.

Ver também: [`docs/cutover-runbook.md`](cutover-runbook.md).
