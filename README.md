# SafetyScale - Sistema de Escala de Segurancas

Sistema monolitico modular para gerenciamento e geracao automatica de escalas mensais de segurancas, com foco em confiabilidade, distribuicao justa de plantoes e alta testabilidade.

## Objetivo

O projeto foi definido para:

- cadastrar e inativar segurancas;
- cadastrar indisponibilidades;
- gerar escala mensal automaticamente;
- balancear finais de semana de forma justa;
- preservar historico de escalas;
- manter base pronta para evolucao.

## Arquitetura e stack obrigatorias

### Principios

- Monolito modular
- Clean Architecture
- CQRS
- SOLID
- Repository Pattern
- Result Pattern
- TDD obrigatorio

### Stack tecnica

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Identity
- JWT Bearer Authentication
- FluentValidation
- MediatR
- Serilog
- xUnit + FluentAssertions
- **SPA em `src/Web.Blazor`:** Blazor WebAssembly (.NET 10) — **frontend oficial**

### Estrutura do projeto

```text
src/
 ├── Api
 ├── Application
 ├── Domain
 ├── Infrastructure
 ├── Tests
 └── Web.Blazor       # SPA Blazor WASM (frontend oficial)
```

## Status de implementacao

### Fases concluidas (backend)

- [x] Fase 0 - Bootstrap e padroes
- [x] Fase 1 - Persistencia e identidade
- [x] Fase 2 - Modulo de segurancas
- [x] Fase 3 - Modulo de indisponibilidades
- [x] Fase 4 - Motor de geracao de escala
- [x] Fase 5 - Consultas de escala e historico

### Frontend (`src/Web.Blazor`)

- [x] Migração Blazor B0–B11 concluída (ver [`roadmap-blazor-migration.md`](roadmap-blazor-migration.md))
- [x] Paridade funcional F0–F4 (auth, setores, seguranças, indisponibilidades, escalas)

### Fases pendentes (backend)

- [ ] Fase 6 - Endurecimento e entrega (Docker, revisao final, documentacao operacional)

## O que ja esta funcionando

- estrutura de camadas (`Api`, `Application`, `Domain`, `Infrastructure`, `Tests`);
- bootstrap de DI entre camadas;
- middleware global de excecoes;
- logging com Serilog;
- EF Core com SQL Server;
- ASP.NET Identity com roles `Admin` e `Supervisor`; usuários com `TenantId` e `DisplayName`;
- autenticacao JWT com claim **`tenant_id`** (tenant do usuário);
- middleware de resolução de tenant após autenticação;
- seed automatico de roles no startup;
- seed de admin em ambiente Development (**e usuario `Supervisor` apenas em Development** para testes de permissao);
- migration inicial criada e aplicada automaticamente no startup;
- modulo de indisponibilidades (`UnavailableDays`) com CQRS + FluentValidation: `POST` / `GET` por seguranca, `DELETE` por id; `Admin` cadastra/remove, `Supervisor` consulta lista;
- modulo de **setores** (`Sector`): cadastro/atualização/inativação, **`requiredGuardsPerDay`** (vagas por dia na geração, mínimo 1), vínculo N:N `SecurityGuardSector`; novo segurança ativo pode ser ligado ao setor padrão (**`Primary`**) para já entrar no pool da escala;
- modulo de escalas mensais: geração preenche **todas as posições por dia**, somando as vagas de cada setor **ativo** com `requiredGuardsPerDay ≥ 1`; **um segurança no máximo uma vez por dia**; só entram seguranças **ativos**, **vinculados ao setor** e **não indisponíveis** naquela data; `POST /api/schedules/generate` (`Admin` apenas): `409` se mês/ano já existe; **`400`** se não houver seguranças ativos, **`400`** sem setores configurados para carga (**`NoWorkloadSectorsConfigured`**, corpo genérico) ou **`400`** com **`ScheduleCoverageFailureResponse`** quando não for possível cobrir um dia (**`code`**: `ScheduleCoverageFailed`; **`message`**: texto legível em português com a data **`dd/MM/yyyy`**; **`failedDate`**: `yyyy-MM-dd`); **consultas/histórico** (`GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}`): itens ordenados por data com **`sectorId`**, **`sectorName`**, segurança (nome e `IsActive`);
- serialização JSON da API em **camelCase** e leitura com **nome de propriedade case-insensitive** (`AddJsonOptions`), alinhando contratos ao SPA e a clientes JSON;
- integracao: `TestWebApplicationFactory` sobe **SQL Server** via **Testcontainers** (container compartilhado) e usa **nome de database unico por instancia** (isolamento paralelo + limpeza `DROP DATABASE` ao descartar a factory);
- tratamento de `ValidationException` com retorno HTTP `400`;
- SPA em **`src/Web.Blazor`**: Blazor WASM — frontend oficial (`/login`, `/signup`, `/app/*`, JWT em `sessionStorage`, porta dev **4864**).
- **CORS** configuravel (`Cors:Origins`); em Development inclui `http://localhost:4864` (Blazor WASM);
- testes unitarios e de integracao do backend (**incl.** registro de tenant e isolamento multitenant onde aplicável) passando.

## Entidades implementadas

- `Tenant` (organização / tenant lógico; `Slug` único)
- **`Sector`** (com `TenantId`; **`RequiredGuardsPerDay`** — posições a preencher por dia na escala quando o setor entra como carga; ativo/inativo)
- **`SecurityGuardSector`** (vínculo segurança ↔ setor; define elegibilidade do segurança às vagas daquele setor)
- `SecurityGuard` (com `TenantId`)
- `UnavailableDay` (com `TenantId`)
- `MonthlySchedule` (com `TenantId`)
- **`ScheduleItem`** (com `TenantId`; **`SectorId`** referencia qual setor a posição cobre)
- Índices relevantes por tenant: unicidade **`(TenantId, SecurityGuardId, Date)`** em itens da escala (um segurança por dia na escala atual); índice auxiliar **`(TenantId, SectorId, Date)`**.

Usuários Identity (`AppUser`) possuem `TenantId` (um tenant por usuário) e `DisplayName` (nome exibível do administrador).

## Multitenancy (isolamento lógico no banco compartilhado)

- **Modelo:** uma linha em `Tenants` por organização; todas as entidades de negócio carregam `TenantId`.
- **API autenticada:** após o login, o JWT inclui a claim **`tenant_id`**. O middleware resolve o tenant no request; o `ApplicationDbContext` aplica **filtros globais** nas entidades de domínio para restringir leituras ao tenant atual.
- **`SaveChanges`:** em requests autenticados com tenant resolvido, novas linhas recebem `TenantId` automaticamente; alterações cruzando tenant são bloqueadas.
- **`AppUser`:** **não** usa filtro global do EF (compatibilidade com `UserManager` / login); o vínculo ao tenant continua por coluna `TenantId` e pela claim do JWT nas APIs de negócio.
- **Seed / migrations:** existe tenant padrão (`slug` `default`) na migration de isolamento; usuários de desenvolvimento são associados a esse tenant. Novas instalações podem criar empresas adicionais pelo fluxo público abaixo.

## Cadastro público de empresa (tenant + admin)

- Endpoint anônimo: **`POST /api/tenants/register`** — cria `Tenant`, gera `Slug` único a partir do nome (com sufixo `-2`, `-3`, … se necessário), cria usuário **Admin** com as credenciais informadas e role `Admin`.
- **SPA:** rota pública **`/signup`** (`RegisterTenantPage`); após sucesso redireciona para **`/login`** com mensagem e e-mail pré-preenchido. Link **“Cadastrar minha empresa”** na tela de login.
- **Riscos operacionais:** endpoint público — em produção considerar rate limiting, CAPTCHA ou fluxo de aprovação (ver `roadmap.md`).

## Endpoints disponiveis atualmente

- `POST /api/auth/login`
- `POST /api/tenants/register` (**anônimo**; `201` tenant + admin; `400` validação/senha; `409` e-mail já usado ou falha ao gerar slug)
- `GET /api/health` (requer role `Admin` ou `Supervisor`)
- `POST /api/security-guards` (requer role `Admin`)
- `GET /api/security-guards` (requer role `Admin` ou `Supervisor`)
- `PUT /api/security-guards/{id}` (requer role `Admin`)
- `PATCH /api/security-guards/{id}/inactive` (requer role `Admin`)
- `PATCH /api/security-guards/{id}/active` (requer role `Admin`)
- **`PUT /api/security-guards/{id}/sectors`** (requer role `Admin`; lista de ids de setores ativos — substitui vínculos do segurança)
- `POST /api/security-guards/{id}/unavailable-days` (requer role `Admin`; `201` em sucesso, `404` seguranca inexistente, `400` seguranca inativo, `409` data duplicada para o mesmo seguranca)
- `GET /api/security-guards/{id}/unavailable-days` (requer role `Admin` ou `Supervisor`)
- `DELETE /api/unavailable-days/{id}` (requer role `Admin`)
- **`POST /api/sectors`** (requer role `Admin`; `requiredGuardsPerDay` opcional, padrão 1)
- **`GET /api/sectors`** (`Admin` ou `Supervisor`; query `?isActive=`)
- **`PUT /api/sectors/{id}`** (`Admin`)
- **`PATCH /api/sectors/{id}/inactive`** e **`PATCH /api/sectors/{id}/active`** (`Admin`)
- `POST /api/schedules/generate` (requer role `Admin`; `201` em sucesso com `Location` apontando para `GET .../{id}`; `409` mes/ano duplicado; **`400`** sem guardas ativos (corpo sem payload detalhado); **`400`** sem setores ativos com carga configurada / sem pool elegível suficiente (**validação de aplicação — corpo simples**); **`400`** quando a geração não cobre um dia: JSON **`code`**: `ScheduleCoverageFailed`, **`message`** (português), **`failedDate`** — ver [`ScheduleCoverageFailureResponse`](src/Api/Contracts/Schedules/ScheduleCoverageFailureResponse.cs))
- `GET /api/schedules/{id}` (requer role `Admin` ou `Supervisor`; `404` se id inexistente)
- `GET /api/schedules/month/{month}/year/{year}` (requer role `Admin` ou `Supervisor`; `404` se nao houver escala gerada para o periodo)

## Pendencias na API relacionadas ao roadmap

- **Fase 6 (backend):** entrega endurecida (Docker/`docker-compose`, padronizacao final de observabilidade, checklist operacional — ver [`roadmap.md`](roadmap.md)).

- .NET SDK 10

## Frontend (`src/Web.Blazor`)

SPA **Blazor WebAssembly** (.NET 10) — frontend oficial. Detalhes em [`src/Web.Blazor/README.md`](src/Web.Blazor/README.md) e [`docs/frontend-blazor-conventions.md`](docs/frontend-blazor-conventions.md).

**Fluxos:** `/login`, `/signup`, área `/app/*` (setores, seguranças, indisponibilidades, escalas). JWT em `sessionStorage` (`safetyscale.auth.session`).

**React legado (arquivado):** [`archive/legacy-react-web/`](archive/legacy-react-web/) — referência histórica apenas.

### Rodar contra a API local

**Recomendado:**

```bash
./scripts/dev-blazor.sh
```

Abra `http://localhost:4864`. A API deve estar em `http://localhost:5003` (CORS habilitado em Development).

**Manual:** `dotnet run --project src/Api/SafetyScale.Api.csproj` + `dotnet run --project src/Web.Blazor/SafetyScale.Web.Blazor.csproj`.

### Scripts úteis (raiz)

| Script | Descrição |
|--------|-----------|
| [`scripts/dev-blazor.sh`](scripts/dev-blazor.sh) | API + Blazor WASM (4864) |
| [`scripts/test-local.sh`](scripts/test-local.sh) | Testes .NET (inclui bUnit Blazor) |
| [`scripts/test-blazor.sh`](scripts/test-blazor.sh) | Gate bUnit Web.Blazor |
| [`scripts/verify-blazor-deploy.sh`](scripts/verify-blazor-deploy.sh) | Verificação HTTP pós-deploy |

> **Produção:** `ApiBaseUrl` vazio no build Blazor ⇒ `/api` via Nginx. Split-origin: `API_BASE_URL` + `CORS_ORIGINS`.

## Configuracao

As principais configuracoes estao em `src/Api/appsettings.json` e `src/Api/appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiryMinutes`
- `Cors:Origins` — lista de origens do browser autorizadas (ex.: `http://localhost:4864` em Development). Vazio desativa o middleware CORS.

> Importante: a chave JWT atual e somente para desenvolvimento. Troque em ambiente real.

## Como executar

### 1) Restaurar e compilar

Solução completa (recomendado — inclui API, testes e Blazor WASM):

```bash
dotnet restore SafetyScale.sln
dotnet build SafetyScale.sln
```

Somente a API:

```bash
dotnet restore src/Api/SafetyScale.Api.csproj
dotnet build src/Api/SafetyScale.Api.csproj
```

### 2) Subir a API

```bash
dotnet run --project src/Api/SafetyScale.Api.csproj
```

No startup da API, automaticamente:

- migrations sao aplicadas;
- roles `Admin` e `Supervisor` sao garantidas;
- existe tenant **`default`** (seed de isolamento multitenant); usuario admin dev e criado ou associado conforme configuracao atual.

### 3) Swagger

Swagger fica habilitado em ambiente de desenvolvimento.

## Autenticacao e autorizacao

### Login

`POST /api/auth/login`

Payload:

```json
{
  "email": "admin@local.com",
  "password": "Mudar@13"
}
```

Resposta de sucesso:

```json
{
  "token": "<jwt>"
}
```

### Uso do token

Enviar no header:

```text
Authorization: Bearer <jwt>
```

O JWT codifica as claims padrão de Identity (**sub**, e-mail etc.) mais **`tenant_id`** (GUID da organização). O frontend mantém esse valor em **`AuthSession.tenantId`** após login.

### Credenciais de desenvolvimento (seed)

- Email: `admin@local.com`
- Senha: `Mudar@13`

Usuario adicional legado:

- Email: `admin@safetyscale.local`
- Senha: `Admin@12345`

Usuario **Supervisor** (apenas Development — role `Supervisor` para testes e integracao):

- Email: `supervisor@safetyscale.local`
- Senha: `Supervisor@12345`

## Banco de dados e migrations

- Banco: **SQL Server** (local ou contêiner; ver `appsettings`)
- ORM: Entity Framework Core
- Migrations em `src/Infrastructure/Persistence/Migrations`: baseline consolidada **`InitialSqlServerSchema`** (provider SQL Server; substitui migrations antigas do SQLite).

Execucao local com Docker (alinha porta/senha padrao de `appsettings*.json`; ajuste a senha antes de usar em producao):

```bash
docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=changeme_UseStrongPw1!' \
  -p 1433:1433 --name safetyscale-sql -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

Ou suba apenas o SQL via Compose (usa a mesma senha de `.env.example`):

```bash
cp .env.example .env
docker compose -f docker-compose.prod.yml up -d sqlserver
```

**Importante:** a senha do `sa` fica gravada no volume Docker na primeira subida. Se voce alterar `MSSQL_SA_PASSWORD` depois, recrie o volume (`docker compose down -v`) ou mantenha a senha original em `appsettings.Development.json`.

Para dev local no WSL, a connection string usa `127.0.0.1` (evita problemas de handshake com `localhost`) e `Encrypt=False`.

Criar nova migration:

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/Infrastructure/SafetyScale.Infrastructure.csproj \
  --startup-project src/Api/SafetyScale.Api.csproj \
  --output-dir Persistence/Migrations
```

Aplicar migrations:

```bash
dotnet ef database update \
  --project src/Infrastructure/SafetyScale.Infrastructure.csproj \
  --startup-project src/Api/SafetyScale.Api.csproj
```

## Testes

Os testes de integracao precisam de **Docker** (Testcontainers inicia uma instancia ephemeral de SQL Server).

Executar todos os testes:

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj
```

Somente unitarios/Application/Domain (sem API integration / sem Docker):

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj --filter "FullyQualifiedName!~SafetyScale.Tests.Api.Integration"
```

Frontend legado React (arquivado): ver [`archive/legacy-react-web/`](archive/legacy-react-web/).

Gate bUnit Blazor:

```bash
./scripts/test-blazor.sh
```

## Docker Compose (producao)

Artefatos:

- Compose prod: [`docker-compose.prod.yml`](docker-compose.prod.yml)
- Compose staging: [`docker-compose.staging.yml`](docker-compose.staging.yml)
- API: [`Dockerfile`](Dockerfile)
- **Frontend Blazor:** [`src/Web.Blazor/Dockerfile`](src/Web.Blazor/Dockerfile), [`src/Web.Blazor/nginx.conf`](src/Web.Blazor/nginx.conf)
- Variaveis: [`.env.example`](.env.example)
- Smoke: [`docs/smoke-cutover-checklist.md`](docs/smoke-cutover-checklist.md)
- Runbook: [`docs/cutover-runbook.md`](docs/cutover-runbook.md)

O servico **`web`** publica **HTTP na porta configurada por `WEB_PORT` (padrao 80)** e encaminha `/api/*` para a API (`api:8080`) na rede interna. O servico **`api`** tambem pode ser acessado **diretamente no host** na porta **`API_PORT`** (padrao **8081**), mapeamento **`${API_PORT:-8081}:8080`**. O **`sqlserver`** expoe **`SQLSERVER_PORT`** no host (padrao **1433**), formato **`${SQLSERVER_PORT:-1433}:1433`**; dentro da Compose a API usa **`sqlserver:1433`**. Persistencia do SQL Server via volume Docker **`sqlserver-data`**.

- **`API_BASE_URL`**: build-arg Blazor. **Vazio** ⇒ `/api` relativo via Nginx. Split-origin: preencher + **`CORS_ORIGINS`**.

- **`CORS_ORIGINS`**: vira **`Cors__OriginsCsv`** na API. Vazio ⇒ sem CORS (adequado com proxy same-origin).

A cada subida da API, **migrations EF** aplicam-se automaticamente.

**Production:** onboarding via **`/signup`** na SPA Blazor.

**Portal da plataforma:** acesse **`/platform/login`** para gerenciar tenants. Na primeira subida, defina no `.env`:

- `BOOTSTRAP_USER_EMAIL` — e-mail do operador inicial
- `BOOTSTRAP_USER_PASSWORD` — senha forte
- `BOOTSTRAP_USER_DISPLAY_NAME` — nome exibido (opcional)
- `BOOTSTRAP_USER_ROLE` — role inicial (`PlatformOwner`, `PlatformAdmin` ou `PlatformSupport`; padrao `PlatformOwner`)

O seed cria o usuario apenas se ele ainda nao existir.

```bash
cp .env.example .env
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
./scripts/verify-blazor-deploy.sh
```

Staging: `docker compose -f docker-compose.staging.yml --env-file .env.staging up -d --build`. Deploy Jenkins: [`DEPLOY-JENKINS.md`](DEPLOY-JENKINS.md).

## Regras de qualidade

- TDD obrigatorio em todas as fases;
- sem regra de negocio em controllers;
- Domain nao depende de Infrastructure;
- toda mudanca de banco com migration;
- comandos/queries/validators/handlers seguindo convencoes de nomenclatura.

## Roadmap

Detalhamento completo de fases e criterios de pronto em `roadmap.md`.

## Licenca

Definir conforme politica do projeto.
