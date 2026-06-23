# AGENTS.md — Sistema de Escala de Seguranças

## Visão Geral

Este projeto consiste em um sistema monolítico para gestão e geração automática de escalas de seguranças.

O sistema deve permitir:

- Cadastro de seguranças
- Cadastro de **setores** (com vagas diárias por setor **`RequiredGuardsPerDay`**)
- **Vincular seguranças aos setores** em que podem trabalhar (**elegibilidade** para a escala daquele setor)
- Inativação de seguranças
- Cadastro de indisponibilidades
- Geração automática de escala mensal (por vagas combinadas dos setores ativos configurados para carga)
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
- SQL Server
- ASP.NET Identity
- JWT Authentication
- FluentValidation
- MediatR
- Serilog
- xUnit
- FluentAssertions
- Docker

## Frontend (React) — legado em migração

> **Status:** **Fases F0–F4 concluídas** no React legado (arquivado). **Migração Blazor B0–B11 concluída** — ver [`roadmap-blazor-migration.md`](roadmap-blazor-migration.md). Frontend oficial: `src/Web.Blazor`.

- React (18+)
- TypeScript
- Vite
- React Router
- Cliente HTTP tipado para a API (`fetch` nativo ou camada fina; **TanStack Query** recomendado para cache, estados de loading/erro e invalidação)
- Estilização: **CSS Modules** adotados na Fase F0 (home); biblioteca de componentes permanece opcional nas fases seguintes, se o time acordar.
- Testes: Vitest + React Testing Library (Vitest em uso; RTL prioritário a partir de telas com componentes mais complexos)

**Condições e alinhamento com o backend:**

- Autenticação **JWT Bearer** igual à API (`Authorization: Bearer <token>`); o token inclui a claim **`tenant_id`** para delimitar o tenant em todas as rotas `/api` autenticadas; refresh/logout conforme política definida na implementação.
- Autorização na UI espelhando perfis **`Admin`** e **`Supervisor`**: rotas, menus e ações condicionais; regras definitivas continuam no backend.
- Formulários com validação de UX (campos obrigatórios, formatos); **validação de negócio permanece na API** (FluentValidation/handlers).
- Tratamento padronizado de erros da API (401, 403, 422, 500) e mensagens ao usuário.
- Base URL da API via variável de ambiente (ex.: `VITE_API_BASE_URL` no React; `ApiBaseUrl` em `wwwroot/appsettings.json` no Blazor).

---

## Frontend (Blazor WebAssembly) — trilha de migração

> **Status:** **B0–B11 concluídas** — Blazor WASM é o **frontend único** (cutover B10, 2026-06-23; React arquivado B11). Projeto em `src/Web.Blazor`. Convenções: [`docs/frontend-blazor-conventions.md`](docs/frontend-blazor-conventions.md).

- Blazor WebAssembly Standalone (.NET 10)
- Porta dev **4864**
- `ApiBaseUrl` + CORS em dev; produção same-origin via Nginx
- JWT em `sessionStorage` (chave `safetyscale.auth.session`) — paridade React
- Estilo: **scoped CSS** (`.razor.css`) 1:1 com CSS Modules do React; **sem biblioteca de UI nova** nesta migração
- Testes: **bUnit** (a partir de B2/B3)

**Convenções obrigatórias:** [`docs/frontend-blazor-conventions.md`](docs/frontend-blazor-conventions.md)

---

## Convivência React + Blazor (transição B0–B11)

| Período | React (`src/Web`) | Blazor (`src/Web.Blazor`) |
|---|---|---|
| B0–B3 | Produção / referência de paridade | Spike, bootstrap, infra, layout |
| **B4+ (freeze)** | Apenas bugfix P0/P1 e manutenção até B11 | Todas as novas features e telas |
| B10 | Coexistência até cutover | **Frontend alvo em produção** (cutover 2026-06-23) |
| B11 | Arquivado (`archive/legacy-react-web/`) | Frontend oficial único |

### Regra de freeze do React (a partir da fase **B4**)

- **Proibido** no React: novas features, novas telas, F5 UX, refactors amplos.
- **Permitido** no React: correções críticas (P0/P1), regressões bloqueantes, ajustes mínimos de CI/build até descomissionamento.
- **Exceção:** PR deve incluir seção **“Exceção freeze React”** com justificativa e impacto.

Detalhes e checklist de PR: [`docs/frontend-blazor-conventions.md`](docs/frontend-blazor-conventions.md) (seções 6 e 7).

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
 ├── Web.Blazor       # SPA Blazor WASM (frontend oficial)
```

Solution .NET na raiz: **`SafetyScale.sln`** (Api, Application, Domain, Infrastructure, Tests, Web.Blazor).

> **`Web.Blazor`:** aplicação Blazor WASM em `src/Web.Blazor`; estrutura-alvo em [`docs/frontend-blazor-conventions.md`](docs/frontend-blazor-conventions.md).

> **React legado (arquivado B11):** [`archive/legacy-react-web/`](archive/legacy-react-web/) — somente referência histórica; não buildar.

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
* SQL Server
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

> **Bootstrap (F0), auth na UI (F1)** incluindo **cadastro público de tenant (`/signup` → `POST /api/tenants/register`)**, **módulo de Seguranças (F2)**, **módulo de indisponibilidades (F3)** e **Escalas na UI (F4)** **atendidos** no repositório. Esta seção permanece como contrato para **F5 em diante** (qualidade, UX ampla).

## Organização

- **Por features** (`features/sectors`, `features/security-guards`, `features/schedules`, `features/tenant-registration`, etc.), não por tipo de arquivo isolado em todo o projeto.
- **Camada de API**: módulos que chamam os endpoints documentados neste arquivo; DTOs/tipos alinhados aos contratos da API.
- **Componentes apresentacionais** versus **containers/hooks** com lógica de dados quando necessário.
- Sem duplicar regras de negócio complexas no cliente; confiar no servidor para decisões finais.

## Telas e fluxos previstos (espelho dos endpoints)

- **Seguranças:** listagem, criação, edição, inativação e **reativação** — alinhado a `/api/security-guards`, `PATCH .../inactive` e `PATCH .../active`.
- **Setores (por segurança):** atualização substitutiva dos vínculos elegíveis — alinhado a **`PUT /api/security-guards/{id}/sectors`** (lista de GUIDs de setores **ativos**).
- **Setores:** CRUD de **`Sector`** (nome, descrição, **`requiredGuardsPerDay`**), ativação/inativação — `POST /api/sectors`, `GET /api/sectors`, `PUT /api/sectors/{id}`, `PATCH .../inactive`, `PATCH .../active`.
- **Indisponibilidades:** CRUD por segurança — alinhado a `/api/security-guards/{id}/unavailable-days` e `DELETE /api/unavailable-days/{id}`.
- **Escalas:** geração mensal e consultas — alinhado a `POST /api/schedules/generate` (**`400`** com **`ScheduleCoverageFailureResponse`** quando um dia não puder ser coberto), `GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}` (itens com **`sectorId`/`sectorName`**).
- **Onboarding de empresa (público):** **`/signup`** — `POST /api/tenants/register` (`AllowAnonymous`); após sucesso, login com o Admin criado. **Stitch:** seguir o padrão quando houver **composição de tela nova** de porte similar às demais; telas muito próximas ao login já existente podem reutilizar o mesmo tratamento visual (CSS Modules `Login`-like) desde que UX e contrato com a API estejam claros — preferir Stitch se o time julgar mudança de layout relevante.

Novas telas administrativas (pós-login) devem seguir o **fluxo obrigatório** descrito em **MCP Google Stitch** antes da implementação em código.

## Qualidade (frontend)

- Linter (ESLint) e formatter (Prettier) no `Web`.
- Testes de componentes e hooks nas regras críticas de UI (permissões, formulários, estados de erro).
- Opcional: E2E (Playwright) após fluxos principais estáveis.

---

# MCP Google Stitch (fluxo padrão para novas telas)

> **Regra deste repositório:** toda **nova tela** administrativa no frontend deve passar pelo Stitch **antes** da implementação em Blazor, exceto nos casos listados em **Onde é opcional**.

O MCP `user-stitch` é a etapa padrão de descoberta visual e validação de UX; o código React deve **espelhar** a referência já aceita, adaptando-a aos padrões deste arquivo e aos contratos reais da API.

## Projeto Stitch de referência

- MCP server: `user-stitch`
- Projeto Stitch: `SafetyScale Web`
- Project resource: `projects/9334796298126275303`
- Project ID para chamadas MCP: `9334796298126275303`
- Device type padrão: `MOBILE`
- Uso principal: gerar e evoluir telas administrativas do SafetyScale alinhadas aos fluxos de backend e ao frontend React.

## Fluxo obrigatório (nova tela)

1. **Gerar ou revisar** a referência no projeto **SafetyScale Web** (`projectId = 9334796298126275303`).
2. **Prompt** objetivo, citando: perfil (`Admin` / `Supervisor`), **endpoint(s)** da API envolvidos, estados de **loading**, **empty state**, **erro** (incl. 401/403/422 quando fizer sentido na UX), validações visíveis ao usuário e o que deve estar habilitado ou escondido por perfil.
3. **Revisar** no Stitch; usar `generate_variants` / `edit_screens` quando precisar de alternativas ou ajustes.
4. **Só então** implementar em `src/Web.Blazor` (pastas `Pages/`, `Components/`, `Services/`, scoped CSS, DTOs alinhados à API).
5. **Registrar** na descrição do PR ou do trabalho qual tela / nome / identificador Stitch foi usada como base, quando aplicável.

## Onde é obrigatório

- Novas telas administrativas **completas**: login, listagens, formulários (criar/editar), dashboards, detalhes, fluxos com mais de um passo ou composição nova de layout.

## Onde é opcional

- Ajustes pequenos em **componentes já existentes**, correções pontuais de texto ou estilo, **refactors** sem mudança de UX, correções de bug que **não** criem nova composição de tela.
- Formulários públicos simples já alinhados ao **login existente** (ex.: **`/signup`**) quando não houver layout novo acordado com design — registar na descrição do PR que a Stitch foi dispensada neste caso.

## Ferramentas principais

- `create_project`: criar novo projeto Stitch somente se o projeto de referência não existir ou se o time solicitar outro projeto.
- `list_projects` / `get_project`: localizar e inspecionar o projeto Stitch.
- `generate_screen_from_text`: gerar tela a partir de prompt usando `projectId = 9334796298126275303`.
- `list_screens` / `get_screen`: consultar telas geradas.
- `create_design_system`, `update_design_system`, `list_design_systems`, `apply_design_system`: gerenciar e aplicar design system.
- `generate_variants`: gerar alternativas visuais para uma tela.
- `edit_screens`: ajustar telas existentes.

## Diretrizes de uso

- **Não** usar Stitch como fonte de regra de negócio, autorização, validação de domínio ou contrato de API.
- O **backend** continua sendo a fonte oficial de permissões, validações e contratos.
- As saídas do Stitch devem ser **adaptadas** às convenções Blazor deste documento antes de virar código em `src/Web.Blazor`.
- **Não** incluir segredos, tokens, senhas reais ou dados sensíveis em prompts ou na documentação do Stitch.

---

# Entidades Obrigatórias

> **Multiempresa (implementação atual):** banco SQL Server **compartilhado** entre tenants (`Tenant`). As entidades de negócio abaixo carregam **`TenantId`**; `AppUser` (Identity) também possui **`TenantId`** e **`DisplayName`**. Índices e unicidades (ex.: indisponibilidade por segurança/data, uma escala por **tenant+mês+ano**) incluem **`TenantId`**.

## Tenant

Representa a empresa / organização lógica (slug único, nome, estado ativo).

---

## Sector

Setor lógico de escala dentro do tenant. Define quantas vagas precisam ser preenchidas **por dia** na geração (`RequiredGuardsPerDay`). Apenas seguranças vinculados via `SecurityGuardSector` são elegíveis às vagas daquele setor.

```csharp
public class Sector
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>Vagas por dia na geração (mínimo 1 quando o setor entra na carga).</summary>
    public int RequiredGuardsPerDay { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

---

## SecurityGuardSector

Vínculo N:N segurança ↔ setor (compartilhado por `TenantId`). Determina elegibilidade: um segurança só pode ser escolhido para vagas de setores aos quais está vinculado.

```csharp
public class SecurityGuardSector
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SecurityGuardId { get; set; }
    public Guid SectorId { get; set; }
}
```

---

## SecurityGuard

```csharp
public class SecurityGuard
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
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
    public Guid TenantId { get; set; }
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
    public Guid TenantId { get; set; }
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
    public Guid TenantId { get; set; }
    public Guid MonthlyScheduleId { get; set; }
    public Guid SecurityGuardId { get; set; }
    public Guid SectorId { get; set; }
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

## Regra 2b — Cobertura diária por setor e unicidade por dia

- Para cada **dia** do mês e cada **setor ativo com carga**, a soma **`RequiredGuardsPerDay`** de todos os setores define quantas linhas devem existir naquele dia (ordem determinística nos slots: repetir por setor em ordem crescente de `SectorId`, `RequiredGuardsPerDay` vezes cada um).
- Não escalar seguranças **sem vínculo** ao setor cuja vaga está sendo preenchida.
- **Um segurança ativo aparece no máximo uma vez no mesmo dia** na escala (índice único tenant + segurança + data nos itens).
- Quando não houver pool elegível e disponível suficiente para **cobrir** todas as vagas de um dia, a geração falha antes de persistir (**resposta HTTP 400** com **`ScheduleCoverageFailureResponse`**: **`code`** `ScheduleCoverageFailed`, **`message`** em português, **`failedDate`** opcional conforme caso).

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

* Gerar escalas a partir das **definições de setor/vagas/elegibilidade**
* Validar indisponibilidades
* Preencher **todas as posições** de cada dia (fins de semana antes dos dias úteis na ordem cronológica)
* Balancear finais de semana na escolha de candidatos (**greedy**, ver critérios de desempate)
* Garantir **no máximo um plantão por segurança por dia** e só candidatos elegíveis e disponíveis

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

> **Implementação atual (por setor):** o pipeline CQRS monta **`SectorWorkloadDefinition`** por cada setor ativo com **`RequiredGuardsPerDay ≥ 1`**, lista de seguranças **ativos vinculados** ao setor em ordem estável (`Guid`). O **`ScheduleGeneratorService`** percorre as datas na ordem **todos os sábados/domingos do mês primeiro**, depois **dias úteis**. Para cada data, expande os **slots diários** (padrão repetido: para cada setor com carga, `RequiredGuardsPerDay` posições) e escolhe um elegível disponível segundo o **greedy** documentado na seção **Estratégia Inicial**. Se não houver candidato para algum slot interrompe com falha (**cobertura**).

## Etapa 1

Carregar:

* Seguranças ativos e **vínculos setor ↔ segurança**
* Setores ativos configurados como carga (**`RequiredGuardsPerDay` ≥ 1**)
* Indisponibilidades do intervalo `[primeiro dia, último dia]` do mês

---

## Etapa 2

Validações de pré-condição (exemplo no handler da aplicação):

* Escala já existe para mês/ano (**conflito**)
* Ausência total de seguranças ativos (**erro cliente** sem payload especial)
* Ausência de setores com definições de carga ou **pool elegível vazio** (**erro cliente** conforme código da API atual)

---

## Etapa 3

Ordenação de dias:

* Todos os **sábados e domingos** do mês (nessa ordem de data no calendário)
* Em seguida os **demais dias úteis** do mês

---

## Etapa 4

Para cada data, distribuir todas as vagas (slots) combinando setores até obter **`MonthlySchedule`** com **`ScheduleItem`** contendo **`SectorId`**.

---

## Etapa 5

Ao detectar falta de cobertura em algum dia, **não persistir**; retornar status de erro de aplicação mapeado em HTTP conforme **`SchedulesController`**.

---

## Etapa 6

Persistir escala válida (**`MonthlySchedule`** + **`ScheduleItems`**) atomicamente (**um `SaveChanges`**).

---

# CQRS Obrigatório

Todos os fluxos devem seguir CQRS.

---

# Commands Obrigatórios

```text
CreateSecurityGuardCommand
UpdateSecurityGuardCommand
InactivateSecurityGuardCommand
ActivateSecurityGuardCommand

AddUnavailableDayCommand
RemoveUnavailableDayCommand

GenerateMonthlyScheduleCommand
CreateSectorCommand
UpdateSectorCommand
InactivateSectorCommand
ActivateSectorCommand
SetSecurityGuardSectorsCommand
```

---

# Queries Obrigatórias

```text
GetSecurityGuardsQuery
GetSectorsQuery
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
PATCH  /api/security-guards/{id}/active
PUT    /api/security-guards/{id}/sectors   # lista de SectorIds — substitui vínculos; somente Admin; todos os ids devem ser setores ativos
```

---

## Sectors (`Admin`; `GET` também `Supervisor`)

```http
POST   /api/sectors
GET    /api/sectors                      # optional ?isActive=
PUT    /api/sectors/{id}
PATCH  /api/sectors/{id}/inactive
PATCH  /api/sectors/{id}/active
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
POST /api/schedules/generate        # Admin; 400 vazio quando sem guards ativos; 400 vazio quando setores elegíveis indisponíveis (pré-cheque); 400 + ScheduleCoverageFailureResponse quando falha ao cobrir dia
GET  /api/schedules/{id}
GET  /api/schedules/month/{month}/year/{year}
```

Contrato **`ScheduleCoverageFailureResponse`**: **`code`** (ex.: `ScheduleCoverageFailed`), **`message`** legível ao usuário, **`failedDate`** (`yyyy-MM-dd` ou omitido quando a API não conseguiu isolar uma data específica). Ver código em **`src/Api/Contracts/Schedules/`** e montagem na API.

---

## Tenants — cadastro público (multitenant)

```http
POST /api/tenants/register
```

Anônimo (sem `Authorization`). Cria `Tenant` + primeiro `Admin`; ver contratos/DTO em `src/Api/Contracts/Tenants/` e tratamento (`201`/`400`/`409`) na documentação Swagger em Development.

---

# Banco de Dados

## Banco obrigatório

* SQL Server

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

JWT deve transportar **`tenant_id`** (GUID da organização) para rotas `/api` autenticadas, alinhando o contexto de execução ao tenant do usuário Identity.

---

# Perfis Obrigatórios

## Admin

Pode:

* Gerenciar seguranças (**incl.** vínculos de setores do segurança)
* Gerenciar setores (**CRUD**/ativar/desativar, **`requiredGuardsPerDay`**)
* Gerar escala
* Visualizar escalas

---

## Supervisor

Pode:

* Visualizar escalas e **consultar setores**
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

* Endpoints obrigatórios listados neste arquivo (**incl.** setores + `PUT` de setores por segurança quando integrado ao roadmap)
* Persistência por tenant onde aplicável
* Fluxo completo de geração de escala
* Cadastro público de tenant (**`TenantsRegistrationEndpointsTests`** ou equivalente)
* Isolamento lógico entre tenants em operações autenticadas (**`MultiTenantIsolationIntegrationTests`** ou equivalente)

---

# Casos de Teste Obrigatórios

## Escala

* Não escalar indisponíveis e não escalar segurança fora dos setores aos quais está vinculado
* Não duplicar segurança no mesmo dia
* Cobrir todas as vagas somadas pelos **`RequiredGuardsPerDay`** dos setores ou falhar antes de gravar (**payload** de cobertura quando aplicável)
* Balancear finais de semana (critério greedy / desempate)
* Respeitar carga combinada dos setores (não apenas “quantidade fixa única por dia sem setor”)
* Não usar segurança inativo

---

## Casos extremos

* Poucos seguranças para o número de vagas diárias (somatório entre setores)
* **Setores ou vínculos** insuficientes para formar um pool elegível
* Todos indisponíveis
* Excesso de indisponibilidades
* Mês com muitos finais de semana

---

# Docker Obrigatório

Deve possuir:

* Dockerfile
* docker-compose.yml

Quando fizer sentido na entrega, o `docker-compose.yml` **pode** incluir serviço da SPA Blazor em build multi-stage (`src/Web.Blazor/Dockerfile`).

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

* **SPA Blazor em `src/Web.Blazor`** (migração B0–B11 concluída); React arquivado em `archive/legacy-react-web/`
* **Multiempresa lógica (Tenants)** já em uso — evoluir com governança, faturação e limites por plano quando necessário
* **`Sector` + vagas combinadas**: **setores já modelam várias vagas/postos simultâneos** — evoluir com turnos diferentes, especialização ou regra de ocupação física quando necessário (ver domínio)
* Múltiplos postos (conceitos além dos setores atuais, se produto distinguir **posto físico × setor lógico**)
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

