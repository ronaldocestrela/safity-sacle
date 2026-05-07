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
- SPA em `src/Web`: React, TypeScript, Vite, React Router, Vitest (Fase F0 do frontend concluida; ver `roadmap.md`)

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

### Frontend (`src/Web`)

- [x] Fase F0 - Bootstrap e convencoes (Vite, ESLint/Prettier, smoke API, CORS em Development)
- [x] Fase F1 - Autenticacao JWT na UI, `sessionStorage`, rotas por perfil (`Admin` / `Supervisor`)

### Fases pendentes (frontend)

- [ ] Fases F2 a F5 — ver `roadmap.md` (modulos de negocio, qualidade)

### Fases pendentes (backend)

- [ ] Fase 3 - Modulo de indisponibilidades
- [ ] Fase 4 - Motor de geracao de escala
- [ ] Fase 5 - Consultas de escala e historico
- [ ] Fase 6 - Endurecimento e entrega

## O que ja esta funcionando

- estrutura de camadas (`Api`, `Application`, `Domain`, `Infrastructure`, `Tests`);
- bootstrap de DI entre camadas;
- middleware global de excecoes;
- logging com Serilog;
- EF Core com SQLite;
- ASP.NET Identity com roles `Admin` e `Supervisor`;
- autenticacao JWT;
- seed automatico de roles no startup;
- seed de admin em ambiente Development;
- migration inicial criada e aplicada automaticamente no startup;
- modulo de segurancas com CQRS + FluentValidation;
- tratamento de `ValidationException` com retorno HTTP `400`;
- **CORS** configuravel (`Cors:Origins`); em Development inclui `http://localhost:4863` para o dev server do `Web`;
- SPA em **`src/Web`** (React + Vite): `/login` com JWT em `sessionStorage`, área `/app` com shell e rotas por perfil, proxy `/api` ou `VITE_API_BASE_URL`, home com smoke de `/api/health`, porta dev **4863**;
- testes unitarios e de integracao da Fase 2 (backend) passando.

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

## Endpoints alvo do projeto (roadmap)

### Security Guards

- `POST /api/security-guards`
- `GET /api/security-guards`
- `PUT /api/security-guards/{id}`
- `PATCH /api/security-guards/{id}/inactive`

### Unavailable Days

- `POST /api/security-guards/{id}/unavailable-days`
- `DELETE /api/unavailable-days/{id}`
- `GET /api/security-guards/{id}/unavailable-days`

### Schedules

- `POST /api/schedules/generate`
- `GET /api/schedules/{id}`
- `GET /api/schedules/month/{month}/year/{year}`

## Requisitos locais

- .NET SDK 10
- Node.js **20.19+** ou **22.12+** (para `src/Web`: Vite 8 / Vitest 4)

## Frontend (`src/Web`)

SPA **React + TypeScript + Vite** com **React Router**, **ESLint**, **Prettier** e **Vitest** com **happy-dom** (evita conflitos ESM com a cadeia `jsdom` + CSS nos testes).

### Fase F1 (auth na UI)

- **Login:** `/login` → `POST /api/auth/login`, JWT em `sessionStorage` (expira → limpa sessão e volta ao login).
- **Área autenticada:** `/app` com shell (sidebar + header), logout, links condicionais por perfil; placeholders em `/app/security-guards`, `/app/unavailable-days`, `/app/schedules`.
- **Referências Google Stitch usadas como base:** tela **Login de Acesso** (`projects/9334796298126275303/screens/1837019a956541aabb147945bb4378ad`) e shell desktop **Shell Administrativo SafetyScale** (`projects/9334796298126275303/screens/7b68e9354acb499f835e008c52c21c57`).

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
