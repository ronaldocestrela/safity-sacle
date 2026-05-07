````markdown
# AGENTS.md — Sistema de Escala de Seguranças

## Visão Geral

Este projeto consiste em um sistema monolítico para gestão e geração automática de escalas de seguranças.

O sistema deve permitir:

- Cadastro de seguranças
- Inativação de seguranças
- Cadastro de indisponibilidades
- Geração automática de escala mensal
- Balanceamento justo de finais de semana
- Controle de histórico das escalas
- Ajustes manuais futuros
- Autenticação e autorização

---

# Stack Obrigatória

## Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- ASP.NET Identity
- JWT Authentication
- FluentValidation
- MediatR
- Serilog
- xUnit
- FluentAssertions
- Docker

## Frontend (React)

> **Status:** **não implementado.** A stack e as convenções abaixo são especificação de referência para quando o SPA for criado; o repositório pode conter apenas backend até lá.

- React (18+)
- TypeScript
- Vite
- React Router
- Cliente HTTP tipado para a API (`fetch` nativo ou camada fina; **TanStack Query** recomendado para cache, estados de loading/erro e invalidação)
- Estilização: CSS Modules ou biblioteca de componentes acordada pelo time (definir na abertura da Fase F0 do frontend)
- Testes: Vitest + React Testing Library (padrão quando o frontend existir)

**Condições e alinhamento com o backend:**

- Autenticação **JWT Bearer** igual à API (`Authorization: Bearer <token>`); refresh/logout conforme política definida na implementação.
- Autorização na UI espelhando perfis **`Admin`** e **`Supervisor`**: rotas, menus e ações condicionais; regras definitivas continuam no backend.
- Formulários com validação de UX (campos obrigatórios, formatos); **validação de negócio permanece na API** (FluentValidation/handlers).
- Tratamento padronizado de erros da API (401, 403, 422, 500) e mensagens ao usuário.
- Base URL da API via variável de ambiente (ex.: `VITE_API_BASE_URL`).

---

# Arquitetura Obrigatória

O projeto DEVE seguir:

- Monólito modular
- Clean Architecture
- CQRS
- SOLID
- Repository Pattern
- Result Pattern
- TDD obrigatório

---

# Estrutura Obrigatória do Projeto

```text
src/
 ├── Api
 ├── Application
 ├── Domain
 ├── Infrastructure
 ├── Tests
 └── Web
```

> **`Web`:** aplicação React (SPA); pasta ausente ou vazia enquanto o frontend não for implementado.

```text
(Web — estrutura sugerida quando existir)
src/Web/
 ├── app/           # providers, router, layout raiz
 ├── features/      # módulos por domínio (security-guards, unavailable-days, schedules)
 ├── shared/        # componentes, hooks, utilitários, tipos API
 └── assets/
```

---

# Estrutura das Camadas

## Api

Responsável apenas por:

* Controllers
* Middlewares
* Configurações
* Swagger
* Autenticação
* Injeção de dependência

NÃO deve conter regra de negócio.

---

## Application

Responsável por:

* Commands
* Queries
* DTOs
* Validators
* Interfaces
* Handlers
* Casos de uso

Todos os fluxos devem usar MediatR.

---

## Domain

Responsável por:

* Entidades
* Regras de negócio
* Value Objects
* Domain Services
* Exceptions

A camada Domain NÃO pode depender de nenhuma outra camada.

---

## Infrastructure

Responsável por:

* EF Core
* SQLite
* Identity
* Repositories
* Serviços externos
* Persistência

---

## Tests

Responsável por:

* Testes unitários
* Testes de integração
* Testes de regras de negócio

TDD é obrigatório.

---

# Frontend (React) — Arquitetura e responsabilidades

> Aplicável **quando** o projeto `Web` for criado. Até lá, esta seção serve apenas como contrato.

## Organização

- **Por features** (`features/security-guards`, `features/schedules`, etc.), não por tipo de arquivo isolado em todo o projeto.
- **Camada de API**: módulos que chamam os endpoints documentados neste arquivo; DTOs/tipos alinhados aos contratos da API.
- **Componentes apresentacionais** versus **containers/hooks** com lógica de dados quando necessário.
- Sem duplicar regras de negócio complexas no cliente; confiar no servidor para decisões finais.

## Telas e fluxos previstos (espelho dos endpoints)

- **Seguranças:** listagem, criação, edição, inativação — alinhado a `/api/security-guards` e `PATCH .../inactive`.
- **Indisponibilidades:** CRUD por segurança — alinhado a `/api/security-guards/{id}/unavailable-days` e `DELETE /api/unavailable-days/{id}`.
- **Escalas:** geração mensal e consultas — alinhado a `POST /api/schedules/generate`, `GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}`.

## Qualidade (frontend)

- Linter (ESLint) e formatter (Prettier) no `Web`.
- Testes de componentes e hooks nas regras críticas de UI (permissões, formulários, estados de erro).
- Opcional: E2E (Playwright) após fluxos principais estáveis.

---

# Entidades Obrigatórias

## SecurityGuard

```csharp
public class SecurityGuard
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## UnavailableDay

```csharp
public class UnavailableDay
{
    public Guid Id { get; set; }
    public Guid SecurityGuardId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
}
```

---

## MonthlySchedule

```csharp
public class MonthlySchedule
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime GeneratedAt { get; set; }
}
```

---

## ScheduleItem

```csharp
public class ScheduleItem
{
    public Guid Id { get; set; }
    public Guid MonthlyScheduleId { get; set; }
    public Guid SecurityGuardId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsWeekend { get; set; }
}
```

---

# Regras Obrigatórias de Negócio

## Regra 1 — Indisponibilidade

Um segurança NUNCA pode ser escalado em um dia indisponível.

---

## Regra 2 — Balanceamento de finais de semana

Sábados e domingos DEVEM ser distribuídos de forma equilibrada entre os seguranças.

A distribuição deve considerar:

* Quantidade de sábados
* Quantidade de domingos
* Quantidade total de finais de semana

Evitar concentração em poucos seguranças.

---

## Regra 3 — Balanceamento geral

A escala deve tentar equilibrar:

* Quantidade total de plantões
* Quantidade de finais de semana
* Intervalo entre plantões

---

## Regra 4 — Segurança inativo

Seguranças inativos:

* NÃO podem entrar em novas escalas
* DEVEM permanecer no histórico

---

# Algoritmo Obrigatório

O sistema deve possuir:

```text
ScheduleGeneratorService
```

Responsável por:

* Gerar escalas
* Validar indisponibilidades
* Balancear finais de semana
* Evitar conflitos
* Distribuir carga de trabalho

---

# Estratégia Inicial do Algoritmo

Usar abordagem:

* Greedy Algorithm

Critérios de ordenação:

1. Menor quantidade de finais de semana
2. Menor quantidade total de plantões
3. Maior intervalo desde último plantão

---

# Fluxo Obrigatório da Geração

## Etapa 1

Carregar:

* Seguranças ativos
* Indisponibilidades

---

## Etapa 2

Separar:

* Dias úteis
* Sábados
* Domingos

---

## Etapa 3

Distribuir finais de semana primeiro.

---

## Etapa 4

Distribuir dias úteis.

---

## Etapa 5

Validar conflitos.

---

## Etapa 6

Persistir escala.

---

# CQRS Obrigatório

Todos os fluxos devem seguir CQRS.

---

# Commands Obrigatórios

```text
CreateSecurityGuardCommand
UpdateSecurityGuardCommand
InactivateSecurityGuardCommand

AddUnavailableDayCommand
RemoveUnavailableDayCommand

GenerateMonthlyScheduleCommand
```

---

# Queries Obrigatórias

```text
GetSecurityGuardsQuery
GetUnavailableDaysQuery
GetMonthlyScheduleQuery
GetMonthlySchedulesQuery
```

---

# Endpoints Obrigatórios

## Security Guards

```http
POST   /api/security-guards
GET    /api/security-guards
PUT    /api/security-guards/{id}
PATCH  /api/security-guards/{id}/inactive
```

---

## Unavailable Days

```http
POST   /api/security-guards/{id}/unavailable-days
DELETE /api/unavailable-days/{id}
GET    /api/security-guards/{id}/unavailable-days
```

---

## Schedules

```http
POST /api/schedules/generate
GET  /api/schedules/{id}
GET  /api/schedules/month/{month}/year/{year}
```

---

# Banco de Dados

## Banco obrigatório

* SQLite

---

# ORM obrigatório

* Entity Framework Core

---

# Migrations

Toda alteração de banco DEVE possuir migration.

---

# Autenticação Obrigatória

Utilizar:

* ASP.NET Identity
* JWT Bearer Authentication

---

# Perfis Obrigatórios

## Admin

Pode:

* Gerenciar seguranças
* Gerar escala
* Visualizar escalas

---

## Supervisor

Pode:

* Visualizar escalas
* Consultar seguranças

---

# Logging Obrigatório

Utilizar:

* Serilog

Logs obrigatórios:

* Geração de escala
* Falhas
* Autenticação
* Exceptions

---

# Validações Obrigatórias

Utilizar:

* FluentValidation

Nenhuma validação deve ficar em controller.

---

# TDD Obrigatório

Todo desenvolvimento deve começar pelos testes.

---

# Testes Obrigatórios

## Unitários

Cobrir:

* Regras de negócio
* Balanceamento
* Algoritmo de geração
* Validações

---

## Integração

Cobrir:

* Endpoints
* Persistência
* Fluxo completo de geração

---

# Casos de Teste Obrigatórios

## Escala

* Não escalar indisponíveis
* Não duplicar segurança no mesmo dia
* Balancear finais de semana
* Respeitar quantidade mínima diária
* Não usar segurança inativo

---

## Casos extremos

* Poucos seguranças
* Todos indisponíveis
* Excesso de indisponibilidades
* Mês com muitos finais de semana

---

# Docker Obrigatório

Deve possuir:

* Dockerfile
* docker-compose.yml

Quando o frontend `Web` existir, o `docker-compose.yml` **pode** incluir serviço da SPA em build multi-stage (opcional até a trilha frontend avançar).

---

# Swagger Obrigatório

Swagger deve estar habilitado em ambiente de desenvolvimento.

---

# Convenções Obrigatórias

## Código

* Código limpo
* Métodos pequenos
* Classes coesas
* Sem lógica em controllers

---

## Nomenclatura

* Commands terminam com `Command`
* Queries terminam com `Query`
* Validators terminam com `Validator`
* Handlers terminam com `Handler`

---

## Frontend (quando implementado)

* Pastas e features com nomes estáveis e alinhados ao domínio (inglês ou PT-BR consistente com o restante do repo).
* Componentes com responsabilidade única; evitar arquivos “catch-all”.
* Não embutir tokens ou lógica sensível no código cliente além do necessário; preferir env e boas práticas de JWT.

---

# Proibições

NÃO fazer:

* Regra de negócio em controller
* SQL manual sem necessidade
* Services gigantes
* Acoplamento entre camadas
* Dependência da Domain em Infrastructure
* Métodos com múltiplas responsabilidades

---

# Futuras Evoluções

O sistema deve nascer preparado para:

* **SPA React em `src/Web`** (especificado neste documento; implementação pendente — ver também `roadmap.md`, trilha Frontend)
* Multiempresa
* Múltiplos postos
* Turnos
* Dashboard
* Exportação PDF/Excel
* Aplicativo mobile
* Notificações
* IA para otimização de escala

---

# Objetivo Principal

O foco principal do sistema é:

* Confiabilidade da escala
* Balanceamento justo
* Facilidade de manutenção
* Facilidade de evolução
* Alta testabilidade

````
