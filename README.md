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
- SQLite
- ASP.NET Identity
- JWT Bearer Authentication
- FluentValidation
- MediatR
- Serilog
- xUnit + FluentAssertions
- SPA em `src/Web`: React, TypeScript, Vite, React Router, Vitest, seguranças e **indisponibilidades** (UI) — ver `roadmap.md`

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
- [x] Fase F1 - Autenticacao JWT na UI, `sessionStorage`, rotas por perfil (`Admin` / `Supervisor`)
- [x] Fase F2 - Seguranças na UI (`/app/security-guards`; `Admin` gerencia, `Supervisor` consulta)
- [x] Fase F3 - Indisponibilidades na UI (`/app/unavailable-days`; `Admin`: CRUD via calendário + **SAVE RESTRICTIONS**; `Supervisor`: consulta)

### Fases pendentes (frontend)

- [ ] Fases F4 e F5 — ver `roadmap.md` (escalas, qualidade)

### Fases pendentes (backend)

- [ ] Fase 6 - Endurecimento e entrega (Docker, revisao final, documentacao operacional)

## O que ja esta funcionando

- estrutura de camadas (`Api`, `Application`, `Domain`, `Infrastructure`, `Tests`);
- bootstrap de DI entre camadas;
- middleware global de excecoes;
- logging com Serilog;
- EF Core com SQLite;
- ASP.NET Identity com roles `Admin` e `Supervisor`;
- autenticacao JWT;
- seed automatico de roles no startup;
- seed de admin em ambiente Development (**e usuario `Supervisor` apenas em Development** para testes de permissao);
- migration inicial criada e aplicada automaticamente no startup;
- modulo de indisponibilidades (`UnavailableDays`) com CQRS + FluentValidation: `POST` / `GET` por seguranca, `DELETE` por id; `Admin` cadastra/remove, `Supervisor` consulta lista;
- modulo de escalas mensais: geracao (`POST /api/schedules/generate`, apenas `Admin`; `409` se mes/ano ja existir; `400` sem guardas ativos ou cobertura impossivel) e **consultas/historico** (`GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}`, `Admin` ou `Supervisor`; `404` se nao existir); resposta com itens ordenados por data e dados do seguranca (nome e `IsActive` atual) para preservar leitura do historico;
- integracao: `TestWebApplicationFactory` usa arquivo SQLite temporario unico por instancia (testes de API em paralelo sem colisao no seed);
- tratamento de `ValidationException` com retorno HTTP `400`;
- **CORS** configuravel (`Cors:Origins`); em Development inclui `http://localhost:4863` para o dev server do `Web`;
- SPA em **`src/Web`** (React + Vite): `/login` com JWT em `sessionStorage`, área `/app` com shell e rotas por perfil; **`/app/security-guards`** com listagem/filtros, CRUD e inativação (UI) alinhados à API; placeholders em `/app/unavailable-days` e `/app/schedules`; proxy `/api` ou `VITE_API_BASE_URL`, home com smoke de `/api/health`, porta dev **4863**;
- testes unitarios e de integracao do backend (modulos de segurancas, indisponibilidades e escalas — geracao + consultas) passando.

## Entidades implementadas

- `SecurityGuard`
- `UnavailableDay`
- `MonthlySchedule`
- `ScheduleItem`

## Endpoints disponiveis atualmente

- `POST /api/auth/login`
- `GET /api/health` (requer role `Admin` ou `Supervisor`)
- `POST /api/security-guards` (requer role `Admin`)
- `GET /api/security-guards` (requer role `Admin` ou `Supervisor`)
- `PUT /api/security-guards/{id}` (requer role `Admin`)
- `PATCH /api/security-guards/{id}/inactive` (requer role `Admin`)
- `PATCH /api/security-guards/{id}/active` (requer role `Admin`)
- `POST /api/security-guards/{id}/unavailable-days` (requer role `Admin`; `201` em sucesso, `404` seguranca inexistente, `400` seguranca inativo, `409` data duplicada para o mesmo seguranca)
- `GET /api/security-guards/{id}/unavailable-days` (requer role `Admin` ou `Supervisor`)
- `DELETE /api/unavailable-days/{id}` (requer role `Admin`)
- `POST /api/schedules/generate` (requer role `Admin`; `201` em sucesso com `Location` apontando para `GET .../{id}`; `409` mes/ano duplicado; `400` sem guardas ativos ou geracao impossivel)
- `GET /api/schedules/{id}` (requer role `Admin` ou `Supervisor`; `404` se id inexistente)
- `GET /api/schedules/month/{month}/year/{year}` (requer role `Admin` ou `Supervisor`; `404` se nao houver escala gerada para o periodo)

## Pendencias na API relacionadas ao roadmap

- **Fase 6 (backend):** entrega endurecida (Docker/`docker-compose`, padronizacao final de observabilidade, checklist operacional — ver [`roadmap.md`](roadmap.md)).

- .NET SDK 10
- Node.js **20.19+** ou **22.12+** (para `src/Web`: Vite 8 / Vitest 4)

## Frontend (`src/Web`)

SPA **React + TypeScript + Vite** com **React Router**, **ESLint**, **Prettier** e **Vitest** com **happy-dom** (evita conflitos ESM com a cadeia `jsdom` + CSS nos testes).

### Fase F1 (auth na UI)

- **Login:** `/login` → `POST /api/auth/login`, JWT em `sessionStorage` (expira → limpa sessão e volta ao login).
- **Área autenticada:** `/app` com **barra inferior de navegação** (padrão do mock **Gestão de Seguranças** no Stitch: Dashboard, Guards, Availability, Rules), header com e-mail / perfil / logout; placeholder em `/app/schedules` até a F4.
- **Referências Google Stitch usadas como base:** tela **Login de Acesso** (`projects/9334796298126275303/screens/1837019a956541aabb147945bb4378ad`), shell desktop histórico **Shell Administrativo SafetyScale** (`projects/9334796298126275303/screens/7b68e9354acb499f835e008c52c21c57`), e **BottomNavBar** da tela MOBILE **Gestão de Seguranças** (`projects/9334796298126275303/screens/1a430c771b494c85baf12207c805be74`) — ícones **Material Symbols**.

### Fase F2 (seguranças na UI)

- **Rota:** `/app/security-guards` protegida — `Supervisor`: somente lista e filtro (`GET /api/security-guards`); `Admin`: criar, editar (`PUT`), inativar (`PATCH .../inactive`) e reativar (`PATCH .../active`).
- Validacoes FluentValidation aparecem na API como HTTP **400** (corpo JSON com lista `errors`).
- **Stitch (`user-stitch`, projeto SafetyScale Web, id `9334796298126275303`):** antes do merge/publicacao, gere ou revise uma tela de **listagem + formulario segurancas** no Stitch e cole o caminho da tela no PR (ex.: `projects/9334796298126275303/screens/<screenId>`), como ja feito na Fase F1. O codigo desta fase segue CSS Modules em `features/security-guards`. Referencia MOBILE de listagem: **Gestao de Segurancas** (`projects/9334796298126275303/screens/1a430c771b494c85baf12207c805be74`).

### Fase F3 (indisponibilidades na UI)

- **Rota:** `/app/unavailable-days` — `Supervisor`: `GET /api/security-guards/{id}/unavailable-days`; `Admin`: idem + `POST /api/security-guards/{id}/unavailable-days`, `DELETE /api/unavailable-days/{id}` (alterações só após **SAVE RESTRICTIONS**).
- Layout alinhado ao mock MOBILE **Cadastro de Indisponibilidade**: `projects/9334796298126275303/screens/7e28e88d0da14a70b894a9586c58ee62`.

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
- usuario admin dev e criado se nao existir.

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

- Banco: SQLite
- ORM: Entity Framework Core
- Migration inicial: `InitialIdentityAndScheduleSchema`

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

Executar todos os testes:

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj
```

Frontend (`src/Web`), com dependencias instaladas:

```bash
cd src/Web && npm run test
```

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
