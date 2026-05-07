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
 └── Tests
````

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

```
```
