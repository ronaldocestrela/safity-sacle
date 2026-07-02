# Deploy com Jenkins (Docker Compose local)

Este guia descreve o fluxo definido pelo [`Jenkinsfile`](Jenkinsfile) para **deploy no mesmo servidor onde o Jenkins roda**, usando [`docker-compose.prod.yml`](docker-compose.prod.yml). O arquivo `.env` de produção **não vai para o Git**: o pipeline monta esse arquivo em tempo de execução com credenciais guardadas no Jenkins.

**Frontend (B11):** serviço **`web`** serve **Blazor WASM** exclusivamente. **Sem Node/npm** no pipeline.

## Premissas

- **Jenkins executa no servidor de destino** (no mesmo host em que o Docker Compose deve subir os contêineres).
- **Agent** com:
  - **Docker** + **Docker Compose plugin** (`docker compose`).
  - **.NET SDK 10** (estágio **Backend Tests**).
  - **curl** — health check e [`scripts/verify-blazor-deploy.sh`](scripts/verify-blazor-deploy.sh).
  - Permissão do usuário do Jenkins para usar o Docker.
- Testes backend (`dotnet test`) usam **Testcontainers** — Docker funcional durante **Backend Tests**.

## Visão geral do fluxo

1. Checkout do repositório.
2. Valida Docker e Compose.
3. **`dotnet test`** (inclui bUnit `src/Tests/Web.Blazor/`) — **bloqueante**.
4. Gera `.env` a partir de credenciais Jenkins.
5. `docker compose up -d --build` (Blazor + API + SQL Server).
6. Verify: `/api/health` + assets WASM Blazor.
7. Remove `.env` no `post`.

Smoke manual pós-deploy: [`docs/smoke-cutover-checklist.md`](docs/smoke-cutover-checklist.md).

---

## Estágios do pipeline

### 1. `Checkout`

`checkout scm` — raiz do clone.

### 2. `Validate Docker`

Verifica `docker` e `docker compose`.

### 3. `Backend Tests`

```bash
dotnet restore SafetyScale.sln
dotnet build SafetyScale.sln --configuration Release --no-restore
dotnet test src/Tests/SafetyScale.Tests.csproj --configuration Release --no-build
```

Inclui **todos** os testes em [`src/Tests/Web.Blazor/`](src/Tests/Web.Blazor/). Falha **interrompe** o pipeline.

### 4. `Prepare Env`

Credenciais → `./.env` via `writeFile` (seguro para caracteres especiais). **`chmod 600`**.

| Chave `.env` | Credencial Jenkins |
|--------------|-------------------|
| `MSSQL_SA_PASSWORD` | `safetyscale-mssql-sa-password` |
| `SQLSERVER_PORT` | `safetyscale-sqlserver-port` |
| `JWT_KEY` | `safetyscale-jwt-key` |
| `JWT_ISSUER` | `safetyscale-jwt-issuer` |
| `JWT_AUDIENCE` | `safetyscale-jwt-audience` |
| `SAFETYSCALE_DB_NAME` | `safetyscale-db-name` |
| `API_PORT` | `safetyscale-api-port` |
| `WEB_PORT` | `safetyscale-web-port` |
| `CORS_ORIGINS` | `safetyscale-cors-origins` |
| `API_BASE_URL` | `safetyscale-api-base-url` |
| `PUBLIC_WEB_BASE_URL` | `safetyscale-public-web-base-url` |
| `SMTP_*` | `safetyscale-smtp-*` |
| `BOOTSTRAP_USER_*` | `safetyscale-bootstrap-user-*` |
| `STRIPE_SECRET_KEY` | `safetyscale-stripe-secret-key` |
| `STRIPE_WEBHOOK_SECRET` | `safetyscale-stripe-webhook-secret` |
| `STRIPE_API_VERSION` | `safetyscale-stripe-api-version` |

Use **`-`** como sentinel em credenciais opcionais (`CORS_ORIGINS`, `API_BASE_URL`, `STRIPE_*`) se o Jenkins não aceitar valor vazio.

Para billing Stripe em produção, configure `STRIPE_SECRET_KEY` (`sk_...` ou `rk_...`) e `STRIPE_WEBHOOK_SECRET` (`whsec_...`). O webhook no Stripe Dashboard deve apontar para `https://<seu-dominio>/api/stripe/webhook` com API version **`2026-06-24.dahlia`**. Ver [`docs/stripe-billing.md`](docs/stripe-billing.md).

### 5. `Deploy`

```bash
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans
```

- **`web`**: build `src/Web.Blazor/Dockerfile`; `API_BASE_URL` vazio ⇒ `/api` relativo via Nginx.
- **`api`**: `Cors__OriginsCsv` ← `CORS_ORIGINS`.

### 6. `Verify`

- Health: `http://127.0.0.1:${WEB_PORT}/api/health` → **401** ou **200**.
- [`scripts/verify-blazor-deploy.sh`](scripts/verify-blazor-deploy.sh): `/`, `_framework/*.js`.

### 7. `post { always }`

Remove `.env` do workspace.

---

## Referências

| Peça | Ficheiro |
|------|-----------|
| Pipeline | [`Jenkinsfile`](Jenkinsfile) |
| Compose prod | [`docker-compose.prod.yml`](docker-compose.prod.yml) |
| Dockerfile Blazor | [`src/Web.Blazor/Dockerfile`](src/Web.Blazor/Dockerfile) |
| Testes bUnit gate | [`scripts/test-blazor.sh`](scripts/test-blazor.sh) |
| Exemplo variáveis | [`.env.example`](.env.example) |
