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

### Estrutura do projeto

```text
src/
 ├── Api
 ├── Application
 ├── Domain
 ├── Infrastructure
 └── Tests
```

## Status de implementacao

### Fases concluidas

- [x] Fase 0 - Bootstrap e padroes
- [x] Fase 1 - Persistencia e identidade

### Fases pendentes

- [ ] Fase 2 - Modulo de segurancas
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
- endpoint de login JWT e rota protegida por role.

## Entidades implementadas

- `SecurityGuard`
- `UnavailableDay`
- `MonthlySchedule`
- `ScheduleItem`

## Endpoints disponiveis atualmente

- `POST /api/auth/login`
- `GET /api/health` (requer role `Admin` ou `Supervisor`)

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

## Configuracao

As principais configuracoes estao em `src/Api/appsettings.json` e `src/Api/appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiryMinutes`

> Importante: a chave JWT atual e somente para desenvolvimento. Troque em ambiente real.

## Como executar

### 1) Restaurar e compilar

```bash
dotnet restore SafetyScale.slnx
dotnet build SafetyScale.slnx
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
  "email": "admin@safetyscale.local",
  "password": "Admin@12345"
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
dotnet test SafetyScale.slnx
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
