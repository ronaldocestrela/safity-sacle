# Deploy com Jenkins (Docker Compose local)

Este guia descreve o fluxo definido pelo [`Jenkinsfile`](Jenkinsfile) para **deploy no mesmo servidor onde o Jenkins roda**, usando [`docker-compose.prod.yml`](docker-compose.prod.yml). O arquivo `.env` de produção **não vai para o Git**: o pipeline monta esse arquivo em tempo de execução com credenciais guardadas no Jenkins.

**Frontend em produção (B10):** serviço **`web`** serve **Blazor WASM** (`src/Web.Blazor/Dockerfile` + Nginx). Testes React permanecem **não bloqueantes** até B11.

## Premissas

- **Jenkins executa no servidor de destino** (no mesmo host em que o Docker Compose deve subir os contêineres).
- **Agent** com:
  - **Docker** + **Docker Compose plugin** (`docker compose`).
  - **.NET SDK 10** (estágio **Backend Tests**).
  - **Node.js** + **npm** (estágio **Frontend Tests** React — opcional/unstable até B11).
  - **curl** — health check e [`scripts/verify-blazor-deploy.sh`](scripts/verify-blazor-deploy.sh).
  - Permissão do usuário do Jenkins para usar o Docker.
- Testes backend (`dotnet test`) usam **Testcontainers** — Docker funcional durante **Backend Tests**.

## Visão geral do fluxo

1. Checkout do repositório.
2. Valida Docker e Compose.
3. **`dotnet test`** (inclui bUnit `src/Tests/Web.Blazor/`) — **bloqueante**.
4. Testes npm React — **allow-failure** (`UNSTABLE`, não falha o build).
5. Gera `.env` a partir de credenciais Jenkins.
6. `docker compose up -d --build` (Blazor + API + SQL Server).
7. Verify: `/api/health` + assets WASM Blazor.
8. Remove `.env` no `post`.

Smoke manual pós-deploy: [`docs/smoke-cutover-checklist.md`](docs/smoke-cutover-checklist.md).  
Cutover/rollback: [`docs/cutover-runbook.md`](docs/cutover-runbook.md).

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

Inclui **todos** os testes em [`src/Tests/Web.Blazor/`](src/Tests/Web.Blazor/) (~59 bUnit). Falha **interrompe** o pipeline.

### 4. `Frontend Tests (React — allow failure until B11)`

```bash
npm ci --prefix src/Web
npm run test --prefix src/Web
```

Envolvido em `catchError` — falha marca estágio **UNSTABLE**, build continua.

### 5. `Prepare Env`

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

Use **`-`** como sentinel em credenciais opcionais (`CORS_ORIGINS`, `API_BASE_URL`) se o Jenkins não aceitar valor vazio.

**Migração de credencial:** renomeie `safetyscale-vite-api-base-url` → `safetyscale-api-base-url` (mesmo valor; vazio = same-origin).

### 6. `Deploy`

```bash
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans
```

- **`web`**: build `src/Web.Blazor/Dockerfile`; `API_BASE_URL` vazio ⇒ `ApiBaseUrl` vazio ⇒ `/api` relativo via Nginx.
- **`api`**: `Cors__OriginsCsv` ← `CORS_ORIGINS`.
- Volume **`sqlserver-data`** persiste dados.

### 7. `Verify`

- Health: `http://127.0.0.1:${WEB_PORT}/api/health` → **401** ou **200**.
- [`scripts/verify-blazor-deploy.sh`](scripts/verify-blazor-deploy.sh): `/`, `_framework/*.js`.

### 8. `post { always }`

Remove `.env` do workspace.

---

## Staging (antes do cutover prod)

```bash
cp .env.example .env.staging
# Ajustar portas (WEB_PORT=8080, API_PORT=8082, SQLSERVER_PORT=1434)

docker compose -f docker-compose.staging.yml --env-file .env.staging up -d --build
BLAZOR_VERIFY_BASE_URL=http://127.0.0.1:8080 ./scripts/verify-blazor-deploy.sh
```

Executar checklist completo em [`docs/smoke-cutover-checklist.md`](docs/smoke-cutover-checklist.md).

---

## Referências

| Peça | Ficheiro |
|------|-----------|
| Pipeline | [`Jenkinsfile`](Jenkinsfile) |
| Compose prod | [`docker-compose.prod.yml`](docker-compose.prod.yml) |
| Compose staging | [`docker-compose.staging.yml`](docker-compose.staging.yml) |
| Dockerfile Blazor | [`src/Web.Blazor/Dockerfile`](src/Web.Blazor/Dockerfile) |
| Nginx Blazor | [`src/Web.Blazor/nginx.conf`](src/Web.Blazor/nginx.conf) |
| Testes bUnit gate | [`scripts/test-blazor.sh`](scripts/test-blazor.sh) |
| Exemplo variáveis | [`.env.example`](.env.example) |
