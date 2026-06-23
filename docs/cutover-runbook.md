# Runbook — cutover Blazor (B10.4)

Procedimento para deploy em **staging**, cutover em **produção**, monitoramento e **rollback**.

---

## 1. Staging

### Subir stack de staging

```bash
cp .env.example .env.staging
# Editar senhas/JWT fortes; WEB_PORT distinto (ex.: 8080) se prod usa 80

docker compose -f docker-compose.staging.yml --env-file .env.staging up -d --build
```

- Rede Compose: `safetyscale-staging-net` (isolada de prod).
- Serviço **`web`**: build a partir de `src/Web.Blazor` (Blazor WASM + Nginx).
- **`ApiBaseUrl`**: vazio no compose padrão (same-origin via `/api`).

### Validar staging

```bash
set -a && . ./.env.staging && set +a
./scripts/verify-blazor-deploy.sh
```

Executar [`docs/smoke-cutover-checklist.md`](smoke-cutover-checklist.md) completo; anexar evidências no registro.

---

## 2. Cutover produção

**Janela sugerida:** horário de baixo tráfego; comunicar stakeholders.

### Pré-cutover

1. CI Jenkins verde com estágio **Backend Tests** (`dotnet test` incluindo bUnit).
2. Smoke checklist 100% OK em staging.
3. Backup/snapshot do volume SQL se política exigir (dados não mudam no cutover de front).
4. Anotar imagem/tag atual do serviço **`web`** React para rollback.

### Deploy

```bash
# Via Jenkins (recomendado) ou manual na máquina de destino:
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans
```

O serviço **`web`** passa a servir **Blazor WASM** (`src/Web.Blazor/Dockerfile`).

### Pós-cutover imediato (≤ 30 min)

```bash
set -a && . ./.env && set +a
./scripts/verify-blazor-deploy.sh
```

- Executar smoke checklist produção (Admin + Supervisor).
- Verificar login, uma operação CRUD Admin e consulta Supervisor.

---

## 3. Monitoramento (24–48 h)

| Sinal | Onde | Ação |
|-------|------|------|
| HTTP **5xx** no `/api/*` | Logs API / Nginx | Investigar stack trace; rollback se P1 |
| Pico **401/403** inesperado | Logs API | JWT/CORS/roles; comparar com baseline |
| Falha carregar `_framework/*.wasm` | Nginx / browser console | MIME/cache — ver `src/Web.Blazor/nginx.conf` |
| Erros JS Blazor | Browser console | Triagem P1/P2; hotfix ou rollback |

**Comandos úteis:**

```bash
docker compose -f docker-compose.prod.yml logs -f api web --tail=200
docker compose -f docker-compose.prod.yml ps
```

---

## 4. Rollback

Se regressão **P1/P2** na UI Blazor:

1. Reverter serviço **`web`** para build React anterior (tag/imagem conhecida) **ou** checkout git anterior ao merge B10 e rebuild:

   ```bash
   # Exemplo: rebuild web a partir de commit/tag com React ainda no compose
   git checkout <tag-pre-b10> -- docker-compose.prod.yml src/Web/Dockerfile
   docker compose -f docker-compose.prod.yml up -d --build web
   ```

2. Confirmar smoke mínimo React (login + `/app/schedules`).
3. Abrir incidente; corrigir Blazor em branch; repetir staging antes de novo cutover.

> **Nota:** B11 remove React definitivamente. Rollback só é possível enquanto imagens/commits React estiverem disponíveis.

---

## 5. Encerramento B10

- Monitoramento 24–48 h sem P1/P2 de UI.
- Atualizar [`roadmap-blazor-migration.md`](../roadmap-blazor-migration.md) com data de cutover.
- Iniciar período de observação B11 (mínimo 1 sprint).
