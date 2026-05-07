# SafetyScale - Sistema de Escala de Segurancas

Sistema monolitico modular para gerenciamento e geracao automatica de escalas mensais de segurancas, com foco em confiabilidade, distribuicao justa de plantoes e alta testabilidade.

## Objetivo

O projeto nasce para:

- cadastrar e inativar segurancas;
- cadastrar indisponibilidades;
- gerar escala mensal automaticamente;
- balancear finais de semana de forma justa;
- preservar historico de escalas;
- suportar ajustes manuais e evolucoes futuras.

## Escopo funcional (AGENTS.md)

### Modulos obrigatorios

- Segurancas (`SecurityGuard`)
- Indisponibilidades (`UnavailableDay`)
- Escalas mensais (`MonthlySchedule`)
- Itens da escala (`ScheduleItem`)
- Autenticacao e autorizacao por perfil

### Regras obrigatorias de negocio

1. Um seguranca nunca pode ser escalado em dia indisponivel.
2. Sabados e domingos devem ser balanceados entre os segurancas.
3. A distribuicao geral deve equilibrar:
   - total de plantoes;
   - total de finais de semana;
   - intervalo entre plantoes.
4. Segurancas inativos nao entram em novas escalas, mas permanecem no historico.

## Arquitetura

### Principios obrigatorios

- Monolito modular
- Clean Architecture
- CQRS
- SOLID
- Repository Pattern
- Result Pattern
- TDD obrigatorio

### Estrutura do projeto

```text
src/
 ├── Api
 ├── Application
 ├── Domain
 ├── Infrastructure
 └── Tests
```

### Responsabilidade por camada

- `Api`: controllers, middlewares, configuracoes, autenticacao, swagger e DI.
- `Application`: commands, queries, handlers, DTOs, validadores e casos de uso (via MediatR).
- `Domain`: entidades e regras de negocio puras (sem dependencia externa).
- `Infrastructure`: EF Core, SQLite, Identity, repositorios e servicos tecnicos.
- `Tests`: testes unitarios, de integracao e cenarios de regras de negocio.

## Stack tecnica

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
- Docker (obrigatorio no escopo)

## Estado atual do repositorio

Implementado no momento:

- estrutura de camadas (`Api`, `Application`, `Domain`, `Infrastructure`, `Tests`);
- configuracao de DI para Application/Infrastructure/Api;
- Identity + JWT;
- seed de perfis `Admin` e `Supervisor`;
- seed de usuario administrador em desenvolvimento;
- middleware global de tratamento de excecoes;
- Serilog;
- migrations aplicadas automaticamente no startup;
- endpoints iniciais:
  - `POST /api/auth/login`
  - `GET /api/health` (requer role `Admin` ou `Supervisor`)

Itens ainda no roadmap obrigatorio:

- comandos/queries/endpoints completos de segurancas;
- modulo de indisponibilidades;
- motor `ScheduleGeneratorService`;
- consultas historicas de escala;
- Dockerfile e `docker-compose.yml`.

## Algoritmo de geracao de escala (especificacao)

Servico obrigatorio: `ScheduleGeneratorService`.

### Estrategia inicial

Abordagem greedy com criterios de desempate:

1. menor quantidade de finais de semana;
2. menor quantidade total de plantoes;
3. maior intervalo desde o ultimo plantao.

### Fluxo obrigatorio de geracao

1. Carregar segurancas ativos e indisponibilidades.
2. Separar dias uteis, sabados e domingos.
3. Distribuir finais de semana primeiro.
4. Distribuir dias uteis.
5. Validar conflitos.
6. Persistir escala.

## Endpoints obrigatorios (alvo do projeto)

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

## Requisitos para rodar localmente

- .NET SDK 10
- SQLite (opcionalmente via arquivo local, sem instalacao global)

## Configuracao local

As configuracoes de desenvolvimento ficam em `src/Api/appsettings.Development.json`:

- connection string SQLite (`DefaultConnection`);
- parametros JWT (`Issuer`, `Audience`, `Key`, `ExpiryMinutes`);
- configuracao de logging/Serilog.

> Importante: a chave JWT atual e de desenvolvimento. Troque para ambiente produtivo.

## Como executar

### 1) Restaurar dependencias

```bash
dotnet restore src/Api/SafetyScale.Api.csproj
```

### 2) Subir API

```bash
dotnet run --project src/Api/SafetyScale.Api.csproj
```

No startup da API:

- migrations do EF Core sao aplicadas automaticamente;
- roles sao criadas (`Admin`, `Supervisor`);
- usuario admin de desenvolvimento e semeado (quando inexistente).

### 3) Swagger

Em ambiente de desenvolvimento, o Swagger fica habilitado automaticamente.

## Autenticacao

Fluxo atual:

1. `POST /api/auth/login` com email e senha.
2. Receber token JWT.
3. Enviar `Authorization: Bearer <token>` nas rotas protegidas.

Credenciais de desenvolvimento (seed):

- Email: `admin@safetyscale.local`
- Senha: `Admin@12345`

## Banco de dados e migrations

- Banco obrigatorio: SQLite.
- ORM obrigatorio: EF Core.
- Toda alteracao de banco deve ter migration correspondente.

## Testes e qualidade

### Diretriz principal

TDD e obrigatorio: desenvolvimento guiado por testes.

### Cobertura esperada

- Unitarios:
  - regras de negocio;
  - validacoes;
  - balanceamento;
  - algoritmo de geracao.
- Integracao:
  - endpoints obrigatorios;
  - persistencia;
  - fluxo completo de geracao.
- Casos extremos:
  - poucos segurancas;
  - todos indisponiveis;
  - excesso de indisponibilidades;
  - meses criticos com muitos finais de semana.

### Executar testes

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj
```

## Logging e observabilidade

Serilog e obrigatorio. Eventos criticos previstos:

- autenticacao;
- geracao de escala;
- falhas e excecoes.

## Convencoes do projeto

- `Command` termina com `Command`
- `Query` termina com `Query`
- `Validator` termina com `Validator`
- `Handler` termina com `Handler`

### Proibicoes

- regra de negocio em controller;
- acoplamento entre camadas;
- dependencia da Domain em Infrastructure;
- classes/metodos com multiplas responsabilidades.

## Perfis de acesso

- `Admin`: gerencia segurancas, gera escala e visualiza escalas.
- `Supervisor`: visualiza escalas e consulta segurancas.

## Roadmap de implementacao

Fases planejadas:

1. Bootstrap e padroes
2. Persistencia e identidade
3. Modulo de segurancas
4. Modulo de indisponibilidades
5. Motor de geracao de escala
6. Consultas de escala e historico
7. Endurecimento, observabilidade e entrega (Docker, checklist final)

## Evolucoes futuras previstas

- multiempresa;
- multiplos postos;
- turnos;
- dashboard;
- exportacao PDF/Excel;
- aplicativo mobile;
- notificacoes;
- IA para otimizacao de escala.

## Licenca

Definir conforme politica do projeto.
