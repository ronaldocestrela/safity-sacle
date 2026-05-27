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
- SPA em `src/Web`: React, TypeScript, Vite, React Router, Vitest — **sectors**, security-guards, indisponibilidades e escalas na UI — ver `roadmap.md`

### Estrutura do projeto

```text
src/
 ├── Api
 ├── Application
 ├── Domain
 ├── Infrastructure
 ├── Web          # SPA React (Vite)
 └── Tests
```

## Status de implementacao

### Fases concluidas (backend)

- [x] Fase 0 - Bootstrap e padroes
- [x] Fase 1 - Persistencia e identidade
- [x] Fase 2 - Modulo de segurancas
- [x] Fase 3 - Modulo de indisponibilidades
- [x] Fase 4 - Motor de geracao de escala
- [x] Fase 5 - Consultas de escala e historico

### Frontend (`src/Web`)

- [x] Fase F0 - Bootstrap e convencoes (Vite, ESLint/Prettier, smoke API, CORS em Development)
- [x] Fase F1 estendida — cadastro público `/signup`, sessão com `tenantId` no JWT
- [x] Fase F2 - Seguranças na UI (`/app/security-guards`; `Admin` gerencia, `Supervisor` consulta)
- [x] Fase F3 - Indisponibilidades na UI (`/app/unavailable-days`; `Admin`: CRUD via calendário + **SAVE RESTRICTIONS**; `Supervisor`: consulta)
- [x] Fase F4 - Escalas na UI (`/app/schedules`; consulta mês/ano; **`Admin`** geração mensal; mensagens claras quando a API devolve **`ScheduleCoverageFailed`** na geração; listagem por item com **setor**); **telas de setores** (`/app/sectors`; vagas por dia **`requiredGuardsPerDay`**)

### Fases pendentes (frontend)

- [ ] Fase F5 — ver `roadmap.md` (qualidade, UX)

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
- **CORS** configuravel (`Cors:Origins`); em Development inclui `http://localhost:4863` para o dev server do `Web`;
- SPA em **`src/Web`** (React + Vite): `/login` e **`/signup`** (cadastro de empresa), JWT em `sessionStorage` com **`tenantId`** derivado do token, área `/app` com shell e rotas por perfil; **`/app/sectors`** (gestão de setores e vagas diárias), **`/app/security-guards`** (inclui setores por segurança), **`/app/unavailable-days`** e **`/app/schedules`** (lista mostra **setor** por atribuição; falha na geração exibe **`message`** retornada pela API); **`/app`** dashboard com detalhe do dia mostrando setor; proxy `/api` ou `VITE_API_BASE_URL`, home com smoke de `/api/health`, porta dev **4863**;
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
- Node.js **20.19+** ou **22.12+** (para `src/Web`: Vite 8 / Vitest 4)

## Frontend (`src/Web`)

SPA **React + TypeScript + Vite** com **React Router**, **ESLint**, **Prettier** e **Vitest** com **happy-dom** (evita conflitos ESM com a cadeia `jsdom` + CSS nos testes).

### Fase F1 (auth na UI)

- **Login:** `/login` → `POST /api/auth/login`, JWT em `sessionStorage` (expira → limpa sessão e volta ao login). Token inclui **`tenant_id`** para isolamento multitenant nas APIs seguintes.
- **Cadastro de empresa:** `/signup` → `POST /api/tenants/register` (**anônimo**); após criar tenant e usuário Admin, fluxo sugere voltar ao login com o mesmo e-mail.
- **Área autenticada:** `/app` com **barra inferior de navegação** (Dashboard, **Sectors**, Guards, Availability, Schedules), header com e-mail / perfil / logout nas telas shell; telas **Sectors**, **Guards**, **Availability** e **Schedules** (`/app/schedules`) usam header próprio estilo Stitch.
- **Referências Google Stitch usadas como base:** tela **Login de Acesso** (`projects/9334796298126275303/screens/1837019a956541aabb147945bb4378ad`), shell desktop histórico **Shell Administrativo SafetyScale** (`projects/9334796298126275303/screens/7b68e9354acb499f835e008c52c21c57`), **BottomNavBar** da tela MOBILE **Gestão de Seguranças** (`projects/9334796298126275303/screens/1a430c771b494c85baf12207c805be74`), e **Regras de Escala** / aba Schedules (`projects/9334796298126275303/screens/e1026c6a3524415ca5f749c9496b2f5e`) — ícones **Material Symbols**.

### Fase F2 (seguranças na UI)

- **Rota:** `/app/security-guards` protegida — `Supervisor`: somente lista e filtro (`GET /api/security-guards`); `Admin`: criar, editar (`PUT`), inativar (`PATCH .../inactive`) e reativar (`PATCH .../active`).
- Validacoes FluentValidation aparecem na API como HTTP **400** (corpo JSON com lista `errors`).
- **Stitch (`user-stitch`, projeto SafetyScale Web, id `9334796298126275303`):** antes do merge/publicacao, gere ou revise uma tela de **listagem + formulario segurancas** no Stitch e cole o caminho da tela no PR (ex.: `projects/9334796298126275303/screens/<screenId>`), como ja feito na Fase F1. O codigo desta fase segue CSS Modules em `features/security-guards`. Referencia MOBILE de listagem: **Gestao de Segurancas** (`projects/9334796298126275303/screens/1a430c771b494c85baf12207c805be74`).

### Fase F3 (indisponibilidades na UI)

- **Rota:** `/app/unavailable-days` — `Supervisor`: `GET /api/security-guards/{id}/unavailable-days`; `Admin`: idem + `POST /api/security-guards/{id}/unavailable-days`, `DELETE /api/unavailable-days/{id}` (alterações só após **SAVE RESTRICTIONS**).
- Layout alinhado ao mock MOBILE **Cadastro de Indisponibilidade**: `projects/9334796298126275303/screens/7e28e88d0da14a70b894a9586c58ee62`.

### Fase F4 (escalas na UI)

- **Rota:** `/app/schedules` — `Supervisor` e `Admin`: `GET /api/schedules/month/{month}/year/{year}`; `Admin`: `POST /api/schedules/generate`. Cada item da lista inclui **`sectorName`** quando a escala existe; erro **`400`** com corpo **`code`**: `ScheduleCoverageFailed` exibe **`message`** devolvida pela API ao usuário.
- **Telas relacionadas:** `/app/sectors` (CRUD/setores + **`requiredGuardsPerDay`**), `/app/security-guards` (**`PUT`** setores por segurança quando **Admin**) — mesmo domínio de elegibilidade da geração.
- Layout alinhado ao mock MOBILE **Regras de Escala**: `projects/9334796298126275303/screens/e1026c6a3524415ca5f749c9496b2f5e` (tokens Sentinel Command; lista de plantões integrada à API).

### Novas telas e Google Stitch (padrão)

1. **Antes de codar** uma tela administrativa nova (login, listagem, formulário, fluxo composto), gerar ou revisar a referência no MCP **`user-stitch`**, projeto Stitch **SafetyScale Web** (`projectId` e fluxo completo em [`agents.md`](agents.md)).
2. O **prompt** deve citar perfil (`Admin` / `Supervisor`), endpoints da API, loading, empty state, erros e comportamento por perfil.
3. Depois da referência aceita, implementar em `src/features/...` / `shared/` conforme [`agents.md`](agents.md).
4. Na **descrição do PR**, indicar qual tela Stitch serviu de base, quando fizer sentido.

Exceções (ex.: ajuste pontual em componente existente sem nova composição de tela) estão descritas em [`agents.md`](agents.md).

### Variáveis de ambiente

Copie `src/Web/.env.example` para `src/Web/.env` e ajuste:

- **`VITE_API_BASE_URL`**: em desenvolvimento, deixe **vazio** para o browser chamar `/api/...` no mesmo host do Vite; o servidor de dev **encaminha** esses pedidos para a API (por padrão `http://localhost:5003`, perfil `http` do `dotnet run`). Se subir a API só em HTTPS local (`dotnet run --launch-profile https`), defina no `.env` do Web: `VITE_DEV_API_PROXY_TARGET=https://localhost:7104`.
- **`VITE_SMOKE_LOGIN_EMAIL`** / **`VITE_SMOKE_LOGIN_PASSWORD`** (opcional): credenciais de smoke na home (ex.: usuário admin de desenvolvimento). Sem elas, a home ainda confirma que a API responde em `/api/health` com 401 (esperado sem token).

### Rodar o Web contra a API local

1. Suba a API (por padrão `dotnet run` usa **HTTP** em `http://localhost:5003`; para HTTPS também em `https://localhost:7104`, use `dotnet run --project src/Api/SafetyScale.Api.csproj --launch-profile https` — ver `src/Api/Properties/launchSettings.json`):

   ```bash
   dotnet run --project src/Api/SafetyScale.Api.csproj
   ```

2. Em outro terminal:

   ```bash
   cd src/Web
   npm install
   npm run dev
   ```

3. Abra `http://localhost:4863` (porta fixa do Vite neste repositório). A página inicial executa o smoke da API (health e, se configurado, login). Use **Ir para login** ou acesse `/login` para autenticar (Fase F1).

### Scripts úteis (`src/Web`)

| Comando              | Descrição        |
|----------------------|------------------|
| `npm run dev`        | Servidor Vite    |
| `npm run build`      | Build produção   |
| `npm run test`       | Vitest (CI)      |
| `npm run lint`       | ESLint           |
| `npm run format`     | Prettier write   |

> **Produção:** defina `VITE_API_BASE_URL` com a URL pública da API e preencha `Cors:Origins` na API com a origem exata do frontend (esquema + host + porta). Em dev o proxy do Vite ainda pode ser usado sem CORS.

## Configuracao

As principais configuracoes estao em `src/Api/appsettings.json` e `src/Api/appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiryMinutes`
- `Cors:Origins` — lista de origens do browser autorizadas (ex.: `http://localhost:4863` em Development). Vazio desativa o middleware CORS.

> Importante: a chave JWT atual e somente para desenvolvimento. Troque em ambiente real.

## Como executar

### 1) Restaurar e compilar

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
docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=Your_Strong_LocalDev_Pwd1' \
  -p 1433:1433 --name safetyscale-sql -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

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

Frontend (`src/Web`), com dependencias instaladas:

```bash
cd src/Web && npm run test
```

## Docker Compose (producao)

Artefatos:

- Compose: [`docker-compose.prod.yml`](docker-compose.prod.yml)
- API: [`Dockerfile`](Dockerfile)
- Frontend (Nginx + build Vite): [`src/Web/Dockerfile`](src/Web/Dockerfile), [`src/Web/nginx.conf`](src/Web/nginx.conf)
- Variaveis de exemplo: [`.env.example`](.env.example) (copie para `.env` na raiz; nao commitar `.env`)

O servico **`web`** publica **HTTP na porta configurada por `WEB_PORT` (padrao 80)** e encaminha `/api/*` para a API (`api:8080`) na rede interna. O servico **`api`** tambem pode ser acessado **diretamente no host** na porta **`API_PORT`** (padrao **8081**), mapeamento **`${API_PORT:-8081}:8080`**. O **`sqlserver`** expoe **`SQLSERVER_PORT`** no host (padrao **1433**), formato **`${SQLSERVER_PORT:-1433}:1433`**; dentro da Compose a API usa **`sqlserver:1433`**. Persistencia do SQL Server via volume Docker **`sqlserver-data`**.

- **`VITE_API_BASE_URL`** (`${VITE_API_BASE_URL:-}`): **build-arg** da imagem do front [**`src/Web/Dockerfile`**](src/Web/Dockerfile). Define a base absoluta chamada pela SPA (**`import.meta.env.VITE_API_BASE_URL`**). **Vazio** ⇒ requests relativos **`/api/...`** (mesma origem atraves do Nginx). Preencha (ex.: `https://api.sua-instancia`) se o SPA for servido por origem diferente da API sem proxy reverso comum — e configure **`CORS_ORIGINS`** de acordo na API.

- **`CORS_ORIGINS`** (`${CORS_ORIGINS:-}`): vira **`Cors__OriginsCsv`** na API (lista CSV de origens; ver [`appsettings.json`](src/Api/appsettings.json)). Vazio ⇒ array vazio ⇒ **middleware CORS nao registra**, adequado quando o navegador so fala **`/api` na mesma origem**. Se o SPA chamar **`VITE_API_BASE_URL`** direto contra a API (`API_PORT`/URL publica da API), preencha com a origem da SPA (**esquema + host + porta**), pode haver mais de uma separadas por vírgula.

A cada subida da API, **migrations EF** aplicam-se automaticamente.

**Ambiente Production:** usuarios **`Admin`** de desenvolvimento nao sao seeded; onboarding de empresa via **`/signup`** na SPA (`POST /api/tenants/register`).

Fluxo recomendado (na maquina com Docker instalado):

```bash
cp .env.example .env
# editar .env — senhas e segredo JWT fortes em producao real

docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

Verifique **`http://localhost:${WEB_PORT:-80}/api/health`** vindo do Nginx (ou apenas `/api/health` com `WEB_PORT=80`). Opcionalmente, via API exposta diretamente: **`http://localhost:${API_PORT:-8081}/api/health`** (mesmas regras HTTP da API). Coloque terminacao **TLS** (Traefik / cloud LB / Ingress) à frente do `web` quando for expor à Internet — a Compose entrega apenas HTTP entre contêineres.

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
