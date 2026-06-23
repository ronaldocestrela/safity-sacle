# Roadmap de Implementacao - SafetyScale

## Objetivo

Entregar um monolito modular em .NET 10 com Clean Architecture, CQRS e TDD, capaz de gerar escalas mensais confiáveis por **setor** (várias vagas diárias somadas via **`Sector.RequiredGuardsPerDay`** e elegibilidade via **`SecurityGuardSector`**), balanceamento justo de finais de semana, resposta HTTP clara (**`ScheduleCoverageFailureResponse`**) quando a cobertura de um dia for impossível, respeitando indisponibilidades e histórico. **Frontend oficial em produção:** **Blazor WASM** em `src/Web.Blazor` (cutover **B10**, 2026-06-23 — ver [`roadmap-blazor-migration.md`](roadmap-blazor-migration.md)). React em `src/Web` permanece legado até **B11**. No backend, **Fases 0 a 5 estão concluídas** — falta **Fase 6** (endurecimento operacional).

## Premissas obrigatorias

- Stack backend: ASP.NET Core Web API, EF Core, SQL Server, Identity + JWT, MediatR, FluentValidation, Serilog, xUnit, FluentAssertions, Docker (inclui Testcontainers nos testes de integracao da API).
- **Stack frontend:** **Blazor WebAssembly** (.NET 10) em `src/Web.Blazor` — **produção** (B10). React (`src/Web`) legado até B11. Migração: [`roadmap-blazor-migration.md`](roadmap-blazor-migration.md).
- Estrutura: `src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure`, `src/Tests`, `src/Web.Blazor`, `src/Web` (legado).
- Regras: sem logica de negocio em controller, Domain sem dependencia externa, migrations para toda mudanca de banco.
- Qualidade: TDD como fluxo padrao no backend; no frontend, testes obrigatorios nas partes criticas quando a trilha for iniciada.

## Estrategia de entrega

- Incremental por fases, cada fase com criterio de pronto.
- Primeiro base arquitetural e seguranca, depois dominio e algoritmo, por fim operacao e hardening.
- **Frontend:** F0–F4 concluídas na UI; **F5** pendente (qualidade, UX). Backend **Fases 3 a 5** concluídas (indisponibilidades; motor `POST /api/schedules/generate`; **consultas/histórico** — `GET /api/schedules/{id}`, `GET /api/schedules/month/{month}/year/{year}`); **isolamento multitenant** e **cadastro público `POST /api/tenants/register`** implementados. **Novas telas:** fluxo Stitch obrigatório por padrão antes do código React — ver `AGENTS.md` (MCP Google Stitch).
- Nenhuma feature avanca sem testes automatizados da propria fase.

## Status atual

### Backend (API)

- [x] Fase 0 - Bootstrap e padroes (concluida)
- [x] Fase 1 - Persistencia e identidade (concluida)
- [x] Fase 2 - Modulo de Segurancas (concluida)
- [x] Fase 3 - Modulo de indisponibilidades (concluida)
- [x] Fase 4 - Motor de geracao de escala
- [x] Fase 5 - Consultas de escala e historico
- [ ] Fase 6 - Endurecimento, observabilidade e entrega (Docker, revisão segurança, hardening para `POST /api/tenants/register` em produção)

### Setores, vagas diárias e elegibilidade (concluído)

- **`Sector`**: **`RequiredGuardsPerDay`** (padrão 1), ativo/inativo; CRUD em **`/api/sectors`** (escrita só **`Admin`**; **`GET`** também **`Supervisor`**).
- **`SecurityGuardSector`**: seguranças elegíveis às vagas de cada setor; substituição completa pelo corpo **`PUT /api/security-guards/{id}/sectors`**.
- **`ScheduleItem.SectorId`**: cada posição gravada refere o setor coberto.
- **`POST /api/schedules/generate`**: valida seguranças ativos, setores ativos com carga e monta **`SectorWorkloadDefinition`** para o **`ScheduleGeneratorService`**; **`400`** JSON **`ScheduleCoverageFailureResponse`** (**`code`** `ScheduleCoverageFailed`) quando não for possível preencher todas as vagas de um dia com seguranças **elegíveis, ativos e não indisponíveis naquela data**; em cenários de pré-validação (**sem setores configurados**, **pool elegível vazio**) o **`400`** pode permanecer sem corpo detalhado (vide implementação atual).
- **Baseline SQL Server atual:** **`InitialSqlServerSchema`** cobre multitenant (`TenantId`), setores, vínculos, **`RequiredGuardsPerDay`**, **`ScheduleItems.SectorId`**, unicidades tenant-aware e demais mapeamentos. Upgrades vindos da trilha antiga SQLite nao foram mesclados: use banco novo + seed conforme projeto.

### Multitenancy e onboarding público (concluído)

- **`Tenant`** + `Slug` único para organizações; todas as linhas de negócio levam **`TenantId`**; índice único de escala por **`TenantId + Month + Year`** (substituindo o índice global antigo apenas mes/ano).
- **JWT** com claim **`tenant_id`**; middleware resolve contexto tenant em requests autenticados.
- **`AppUser`** com **`TenantId`** + **`DisplayName`**; filtros EF globais em entidades de negócio, **exceto** `AppUser` (Identity / `FindByEmail` etc.).
- **Endpoint público** `POST /api/tenants/register` (**AllowAnonymous**) — fluxo transacional cria Tenant, garante slug único (sufixo numérico se necessário) e primeiro **Admin**.
- **`/signup`** em `src/Web`, link a partir da tela de login, testes de integração de registro e de isolamento entre tenants onde aplicável.
- Pendências de endurecimento (rate-limit, CAPTCHA, aprovação manual da conta) ficam como **parte-alvo da Fase 6**.

### Frontend (React em `src/Web`) — **parcial (F0–F4 concluídas; F5 pendente)**

- [x] Fase F0 - Bootstrap e convencoes do `Web`
- [x] Fase F1 - Autenticacao, sessao JWT e autorizacao por perfil na UI
- [x] Fase F2 - Modulo de segurancas (telas alinhadas aos endpoints da Fase 2 backend)
- [x] Fase F3 - Modulo de indisponibilidades
- [x] Fase F4 - Modulo de escalas, setores na UI (`/app/sectors`) e integração segurança↔setor (consulta/gestão onde aplicável)
- [ ] Fase F5 - Qualidade, UX e integracao na entrega (testes, Docker opcional multi-servico)

> Detalhamento da trilha F1-F5: secao **Trilha Frontend React**. Stack, CORS e execucao local: `README.md` e `AGENTS.md` (secao Frontend).

## Fase 0 - Bootstrap e padroes (Fundacao)

### Entregaveis

- Solucao .NET com os projetos `Api`, `Application`, `Domain`, `Infrastructure`, `Tests`.
- Referencias entre camadas conforme Clean Architecture.
- Configuracao inicial de DI, MediatR, FluentValidation e Serilog.
- Convencoes de nomenclatura e estrutura de pastas aplicadas.

### Tarefas principais

- Criar solution e projetos.
- Definir `Result Pattern` compartilhado na camada de Application/Domain.
- Configurar pipeline base de excecoes e tratamento padronizado de erros na API.
- Habilitar Swagger em desenvolvimento.

### Criterio de pronto

- Build e testes vazios executando localmente.
- API sobe com Swagger e logging basico.
- Arquitetura valida sem dependencia indevida da Domain.

## Fase 1 - Persistencia e identidade (Base operacional)

### Entregaveis

- `DbContext` com entidades obrigatorias: `SecurityGuard`, `UnavailableDay`, `MonthlySchedule`, `ScheduleItem`.
- Mapeamentos EF Core e repositorios.
- ASP.NET Identity configurado com perfis `Admin` e `Supervisor`.
- Autenticacao JWT funcional.

### Tarefas principais

- Criar migrations iniciais.
- Implementar seed minimo de perfis e usuario admin de desenvolvimento.
- Configurar autorizacao por role nos endpoints.
- Registrar logs de autenticacao e falhas.

### Criterio de pronto

- Banco SQL Server criado via migration aplicada pela API ao subir (`MigrateAsync`).
- Login retorna JWT valido.
- Rotas protegidas respeitam roles.

## Fase 2 - Modulo de Segurancas (CRUD + inativacao)

### Entregaveis

- Commands:
  - `CreateSecurityGuardCommand`
  - `UpdateSecurityGuardCommand`
  - `InactivateSecurityGuardCommand`
- Query:
  - `GetSecurityGuardsQuery`
- Endpoints:
  - `POST /api/security-guards`
  - `GET /api/security-guards`
  - `PUT /api/security-guards/{id}`
  - `PATCH /api/security-guards/{id}/inactive`

### Tarefas principais

- Implementar handlers CQRS com validadores FluentValidation.
- Garantir que inativacao nao remove historico.
- Cobrir cenarios de sucesso e erro (id inexistente, payload invalido).

### Criterio de pronto

- Testes unitarios dos handlers e validators passando.
- Testes de integracao dos 4 endpoints passando.

### Status de entrega da fase

- [x] Commands implementados: `CreateSecurityGuardCommand`, `UpdateSecurityGuardCommand`, `InactivateSecurityGuardCommand`
- [x] Query implementada: `GetSecurityGuardsQuery`
- [x] Endpoints implementados:
  - `POST /api/security-guards`
  - `GET /api/security-guards`
  - `PUT /api/security-guards/{id}`
  - `PATCH /api/security-guards/{id}/inactive`
- [x] Handlers com FluentValidation e sem logica de negocio em controller
- [x] Testes unitarios dos handlers/validators passando
- [x] Testes de integracao dos 4 endpoints passando

## Fase 3 - Modulo de indisponibilidades

### Entregaveis

- Commands:
  - `AddUnavailableDayCommand`
  - `RemoveUnavailableDayCommand`
- Query:
  - `GetUnavailableDaysQuery`
- Endpoints:
  - `POST /api/security-guards/{id}/unavailable-days`
  - `DELETE /api/unavailable-days/{id}`
  - `GET /api/security-guards/{id}/unavailable-days`

### Tarefas principais

- Regras de consistencia (nao duplicar indisponibilidade para mesma data/seguranca).
- Validar existencia e status do seguranca para cadastro.
- Garantir rastreabilidade da origem da indisponibilidade (motivo opcional).

### Criterio de pronto

- Testes unitarios de regras e validacoes.
- Testes de integracao dos endpoints com persistencia real em SQL Server efemero via Testcontainers.

### Status de entrega da fase

- [x] Commands implementados: `AddUnavailableDayCommand`, `RemoveUnavailableDayCommand`
- [x] Query implementada: `GetUnavailableDaysQuery`
- [x] Endpoints implementados:
  - `POST /api/security-guards/{id}/unavailable-days`
  - `DELETE /api/unavailable-days/{id}`
  - `GET /api/security-guards/{id}/unavailable-days`
- [x] Regras: sem duplicidade data/segurança (validação aplicacional + índice único), segurança inativo não cadastra nova indisponibilidade
- [x] Autorização: `Admin` em POST e DELETE; `Admin` ou `Supervisor` em GET
- [x] Testes unitarios dos handlers, validators e query passando
- [x] Testes de integracao dos tres endpoints passando (`TestWebApplicationFactory` com database dedicado por instancia em SQL Server via Testcontainers)

## Fase 4 - Motor de geracao de escala (Core de negocio)

### Entregaveis

- `ScheduleGeneratorService` implementado (greedy com **várias vagas por dia**, somadas pelos **`RequiredGuardsPerDay`** dos setores com carga; **um segurança no máximo por dia** na escala; ordem cronológica: **todos os fins de semana do mês antes dos dias úteis**).
- `GenerateMonthlyScheduleCommand` usando **setores ativos**, elegíveis (**vinculados** ao setor **e** seguranças ativos) e **`SectorWorkloadDefinition`**.
- Regras obrigatorias aplicadas:
  - nao escalar indisponiveis;
  - nao escalar inativos;
  - nao escalar fora dos setores vinculados ao seguranca;
  - balancear fins de semana nos desempates (greedy conforme codigo/domínio);
  - balancear total de plantões e maior intervalo entre plantões onde aplicável aos critérios de escolha;
  - preencher **todas** as posições exigidas por dia (**somatório das vagas** dos setores) ou falhar com **`ScheduleCoverageFailed`** onde mapeado.
- Fluxo de geracao CQRS atual: carregar **guards**/indisposições/setores; validar pré-condições; gerar **`MonthlySchedule` + items** atomicamente (ver **`agents.md`** para o espelho de etapas alinhadas à implementação).

### Tarefas principais (TDD primeiro)

- Criar suite de testes de dominio para:
  - indisponibilidade e **elegibilidade por setor**;
  - balanceamento de finais de semana;
  - balanceamento geral;
  - intervalo entre plantoes empatados;
  - **várias vagas/dia combinando setores** e falhas de cobertura.
- Implementar algoritmo greedy com criterios:
  1. menor qtd de finais de semana;
  2. menor qtd total de plantoes;
  3. maior intervalo desde ultimo plantao.
- Persistir `MonthlySchedule` e `ScheduleItem` atomicamente.

### Criterio de pronto

- Casos obrigatorios de escala validados por testes automatizados (**incl. setores combinados / cobertura**).
- Geracao mensal funcional por comando, sem duplicidade do **mesmo segurança no mesmo dia**; itens sempre com **`SectorId`** válido quando persistidos.
- Logs de geracao e falhas auditaveis via Serilog.

### Status de entrega da fase

- [x] `ScheduleGeneratorService` no domínio (**greedy** por slot; fins de semana antes de dias úteis; **`ExpandDailySlots`** determinístico; critérios de desempate `agents.md`/código-fonte `ScheduleGeneratorService`)
- [x] Command + validator: `GenerateMonthlyScheduleCommand` (`Month`/`Year`), FluentValidation 1–12 e ano 2000–2100 (**integração real** com **`ISectorRepository.GetActiveWorkloadSectorsWithLinksAsync`**)
- [x] CRUD **`/api/sectors`**, vínculos **`PUT /api/security-guards/{id}/sectors`** (testes de integração onde aplicável)
- [x] Endpoint `POST /api/schedules/generate` (`Admin` apenas); `409` se mês/ano já gerado (**índice único `TenantId+Month+Year`**); **`400`** sem guardas ativos; **`400`** quando não há setores de carga válidos ou **pool elegível vazio** (status tratado pelo handler atual); **`400`** + **`ScheduleCoverageFailureResponse`** quando a cobertura de um dia falhar
- [x] Persistência: `MonthlySchedule` + `ScheduleItem` (com **`SectorId`**) num único `SaveChanges`; `GetActiveAsync` em seguranças; `GetByDateRangeAsync` em indisponibilidades
- [x] Migrations (**setores**, **`RequiredGuardsPerDay`**, **`SectorId`** em itens — ver subseção “Setores…” acima sobre **limpeza** de dados legados onde necessário)
- [x] Testes: domínio do gerador, handler CQRS, validator e integração API (`SchedulesGenerateEndpointsTests`, incluindo **`ScheduleCoverageFailed`** onde coberto pelo contrato atual)

## Fase 5 - Consultas de escala e historico

### Entregaveis

- Queries:
  - `GetMonthlyScheduleQuery`
  - `GetMonthlySchedulesQuery`
- Endpoints:
  - `GET /api/schedules/{id}`
  - `GET /api/schedules/month/{month}/year/{year}`
- Projecoes de leitura com dados de seguranca **e setor**, marcacao de fim de semana.

### Tarefas principais

- Implementar DTOs de resposta para consultas.
- Garantir que escalas antigas preservem registros mesmo com seguranca inativado.
- Otimizar consultas mais frequentes para leitura.

### Criterio de pronto

- Endpoints de consulta retornam historico completo e consistente.
- Testes de integracao cobrindo consulta por id e por mes/ano.

### Status de entrega da fase

- [x] Queries implementadas: `GetMonthlyScheduleQuery`, `GetMonthlySchedulesQuery` (por mês/ano; sem listagem global de todas as escalas)
- [x] Endpoints: `GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}` (`Admin` ou `Supervisor`; `404` quando não existir)
- [x] Resposta com DTOs: cabeçalho da escala e itens ordenados por data, com dados do segurança (id, nome, `IsActive`), **`sectorId`** / **`sectorName`**, `Date` e `IsWeekend`; leitura EF com includes necessários (**`Sector`**) e `AsNoTracking`
- [x] Testes de aplicação (handlers/validators) e de integração (`SchedulesQueryEndpointsTests`), incluindo histórico após inativação de segurança

## Fase 6 - Endurecimento, observabilidade e entrega

### Entregaveis

- Dockerfile e `docker-compose.yml`.
- Padronizacao final de logs (autenticacao, exceptions, geracao de escala).
- Documentacao de execucao local e fluxo basico de uso.
- Revisao de seguranca e autorizacao por perfil.

### Tarefas principais

- Proteções para **`POST /api/tenants/register`** em produção (rate limiting / CAPTCHA / aprovação — conforme produto).
- Ajustar health checks e configuracoes por ambiente.
- Executar bateria completa de testes unitarios + integracao.
- Revisar validacoes fora de controllers (100% em validators/handlers/domain).

### Criterio de pronto

- Aplicacao sobe via Docker.
- Pipeline local de testes verde.
- Checklist de conformidade do `AGENTS.md` atendido.

## Trilha Frontend React (F0–F4 concluídas; F5 pendente)

> A **Fase F0** está implementada no repositório (`src/Web`, proxy Vite, CORS na API em Development, porta dev `4863` — ver `README.md`). As **Fases F1 a F4** (auth JWT na UI; seguranças e setores por seguranca; **`/app/sectors`**; indisponibilidades; escalas **`/app/schedules`**) estão implementadas. **Fase F5** segue abaixo.

### Fase F0 - Bootstrap e convencoes do `Web`

#### Entregaveis

- Projeto em `src/Web` com Vite + React + TypeScript.
- ESLint + Prettier; scripts `dev`, `build`, `test` (Vitest preparado).
- Estrutura sugerida em `AGENTS.md` (`app/`, `features/`, `shared/`).
- Variavel de ambiente para base URL da API (ex.: `VITE_API_BASE_URL`).

#### Tarefas principais

- Criar o pacote frontend e documentar no README como rodar contra a API local.
- Configurar path aliases se necessario e politica de imports.

#### Criterio de pronto

- `npm run build` (ou equivalente) sem erros; pagina inicial carrega e chama health ou endpoint publico se existir.

#### Status de entrega da fase

- [x] Projeto `src/Web` com Vite + React + TypeScript + React Router
- [x] ESLint, Prettier, scripts `dev`, `build`, `test` (Vitest)
- [x] Pastas `app/`, `features/`, `shared/`, `assets/`
- [x] `VITE_API_BASE_URL` e proxy local documentados; smoke na home (`/api/health`, login opcional)
- [x] Estilizacao: CSS Modules na home (definido na F0)
- [x] API: CORS configuravel via `Cors:Origins` (Development inclui `http://localhost:4863`); dev server Vite na porta **4863**

### Fase F1 - Autenticacao, sessao JWT e autorizacao por perfil na UI

#### Entregaveis

- Tela de login integrada ao fluxo Identity/JWT da API.
- Armazenamento seguro do token (preferir memoria + refresh ou estrategia definida pelo time); envio em `Authorization: Bearer`.
- Rotas protegidas e **visibilidade condicional** para `Admin` vs `Supervisor` (menus, botoes, rotas).

#### Tarefas principais

- **Stitch (padrão):** antes de implementar em React a tela de login, shell de navegação e quaisquer telas novas desta fase, gerar ou revisar referência no MCP `user-stitch` (projeto `SafetyScale Web`; detalhes em `AGENTS.md`).
- Tratar 401/403 com redirecionamento ou mensagens claras.
- Mapear permissoes espelhando `AGENTS.md` (Admin: gestao + geracao + visualizacao; Supervisor: visualizacao e consulta).

#### Criterio de pronto

- Usuario consegue autenticar e acessar apenas rotas permitidas ao seu perfil; testes minimos nos guards/hooks de auth.
- Referências Stitch das telas novas desta fase revisadas; citação da tela/base usada no PR quando aplicável.

#### Status de entrega da fase

- [x] Tela de login + shell + rotas protegidas e por perfil
- [x] Cadastro público de empresa (**`/signup`**) chamando **`POST /api/tenants/register`**; cliente em `features/tenant-registration` (ou pasta equivalente)
- [x] Token em `sessionStorage` + `Authorization: Bearer` via `apiFetch`
- [x] Referências Stitch: Login de Acesso (`1837019a956541aabb147945bb4378ad`) e Shell Administrativo desktop (`7b68e9354acb499f835e008c52c21c57`)
- [x] Testes Vitest + RTL dos guards e login

### Fase F2 - Modulo de segurancas (UI)

#### Entregaveis

- Telas: listagem, criacao, edicao, inativacao de segurancas, alinhadas a:
  - `POST /api/security-guards`
  - `GET /api/security-guards`
  - `PUT /api/security-guards/{id}`
  - `PATCH /api/security-guards/{id}/inactive`

#### Tarefas principais

- **Stitch (padrão):** antes de codar as telas de listagem, criação, edição e inativação, gerar ou revisar referências no MCP `user-stitch` (`AGENTS.md`).
- Formularios com validacao de UX; feedback de erros da API (422, etc.).
- Estado de lista coerente com backend (incl. seguranca inativo visivel conforme regra de produto).

#### Criterio de pronto

- Fluxo completo usavel por `Admin`; testes de componente nas telas criticas.
- Referências Stitch revisadas e citadas no PR quando aplicável.

#### Status de entrega da fase

- [x] Rota `/app/security-guards` com listagem + filtro (todos / ativos / inativos), integrada a `GET /api/security-guards`
- [x] `Admin`: nova segurança (`POST`), edicao (`PUT`), inativacao (`PATCH`) com dialogs; erros HTTP exibidos (ex.: validacao como **400**, permissoes **403**, nao encontrado **404**)
- [x] `Supervisor`: consulta apenas; link **Seguranças** ativo na bottom nav (`GET` permitido na API); **Availability** abre `/app/unavailable-days` com leitura (`GET` indisponibilidades)
- [x] Testes Vitest + RTL em `features/security-guards` (lista por perfil, empty, erro de carga `ApiError`, validacao UX de nome, criacao feliz mockada)

> Referencia Stitch: seguir projeto **SafetyScale Web** (`projectId` **9334796298126275303**); cole o caminho da tela no README/PR apos revisao no MCP `user-stitch` (vide `README.md` secao **Fase F2**).

### Fase F3 - Modulo de indisponibilidades (UI)

#### Entregaveis

- Cadastro, listagem e exclusao de indisponibilidades por seguranca:
  - `POST /api/security-guards/{id}/unavailable-days`
  - `GET /api/security-guards/{id}/unavailable-days`
  - `DELETE /api/unavailable-days/{id}`

#### Tarefas principais

- **Stitch (padrão):** antes das telas de indisponibilidades (listagem, cadastro, exclusão), gerar ou revisar referência no MCP `user-stitch` (`AGENTS.md`).
- Evitar duplicidade de data na UX quando a API rejeitar; datas e motivo opcional alinhados ao contrato.

#### Criterio de pronto

- Integracao completa com Fase 3 backend; testes cobrindo happy path e erro de validacao.
- Referências Stitch revisadas e citadas no PR quando aplicável.

#### Status de entrega da fase

- [x] Rota `/app/unavailable-days` com calendário mensal, seleção de segurança e **SAVE RESTRICTIONS** (`Admin`); `Supervisor` somente leitura
- [x] Integração: `GET` / `POST` / `DELETE` conforme perfis da API; motivo opcional (até 250 caracteres) nos novos cadastros
- [x] Referência Stitch: **Cadastro de Indisponibilidade** (`projects/9334796298126275303/screens/7e28e88d0da14a70b894a9586c58ee62`)
- [x] Testes Vitest + RTL em `features/unavailable-days`

### Fase F4 - Modulo de escalas (UI)

#### Entregaveis

- Acionar geracao: `POST /api/schedules/generate`.
- Consultas: `GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}`.
- Gestão **`/app/sectors`**: alinhamento aos endpoints de setores (inclui **`requiredGuardsPerDay`** onde a UI expõe).
- **Seguranças:** superfície na UI para **`PUT /api/security-guards/{id}/sectors`** onde implementado (**`Admin`**).
- Visualizacao clara de itens (**setor**, datas, fim de semana, seguranca); tratamento **`message`** quando a API responder **`ScheduleCoverageFailed`**.

#### Tarefas principais

- **Stitch (padrão):** antes das telas de geração e consulta de escalas **ou** grandes mudanças de layout em setores, gerar ou revisar referência no MCP `user-stitch` (`AGENTS.md`).
- Parametros mes/ano, feedback de geracao (loading, sucesso, falha auditavel na UI com mensagem legível quando a API devolver JSON estruturado).
- Respeitar quem pode gerar (`Admin`) vs somente leitura (`Supervisor`) na interface.

#### Criterio de pronto

- Fluxos de geracao e consulta usáveis **e** fluxo de setores onde existir na UI; testes nos componentes de listagem/detalhe principais.
- Referências Stitch revisadas e citadas no PR quando aplicável.

#### Status de entrega da fase

- [x] Rota **`/app/sectors`** + integração aos endpoints de setores
- [x] Rota `/app/schedules` com consulta por mês/ano e lista de itens (**setor** por linha); `Admin`: geração (**`ScheduleCoverageFailed` → mensagem**); `Supervisor`: consulta apenas
- [x] Rota **`/app/security-guards`** com gestão dos setores do seguranca (**`Admin`**, onde aplicável)
- [x] Referência Stitch (base das escalas): **Regras de Escala** (`projects/9334796298126275303/screens/e1026c6a3524415ca5f749c9496b2f5e`)
- [x] Testes Vitest + RTL em `features/schedules` (mensagem **`ScheduleCoverageFailed`** onde mockado)

### Fase F5 - Qualidade, UX e integracao na entrega

#### Entregaveis

- Tratamento global de erros, acessibilidade basica, consistencia visual.
- Cobertura de testes Vitest + RTL nas areas de auth e um fluxo de negocio principal.
- Opcional: servico `Web` no `docker-compose.yml` junto da API.

#### Tarefas principais

- **Stitch:** qualquer **nova tela** ou revisão grande de layout nesta fase segue o fluxo padrão (`AGENTS.md`); manter consistência com design system / telas já validadas no projeto Stitch quando aplicável.
- Revisar performance de listas e chamadas (TanStack Query).
- Documentar no README o fluxo usuario final (login → cadastros → escala).

#### Criterio de pronto

- Checklist frontend do `AGENTS.md` atendido; build de producao do `Web` documentado.
- Uso do Stitch documentado para telas novas introduzidas ou redesenhadas nesta fase (referência citada no PR quando aplicável).

## Plano de testes (obrigatorio por fase)

- Unitarios (backend):
  - regras de negocio;
  - algoritmo de geracao;
  - balanceamento;
  - validadores.
- Integracao (backend):
  - endpoints obrigatorios (**incl.** **`/api/sectors`** e **`PUT`** de setores do seguranca);
  - persistencia;
  - fluxo completo de geracao;
  - consultas `GET` de escala por id e por mes/ano (`SchedulesQueryEndpointsTests`).
  - **Multitenancy / onboarding:** registros públicos (`TenantsRegistrationEndpointsTests`) e isolamento entre tenants onde aplicável.
- Casos extremos:
  - poucos segurancas;
  - todos indisponiveis;
  - excesso de indisponibilidades;
  - mes com muitos finais de semana.
- Vitest + React Testing Library: **auth**, **segurancas**, **unavailable-days**, **schedules** e **sectors** (onde há testes no repositório).

## Ordem sugerida de execucao

### Backend

1. Fase 0
2. Fase 1
3. Fase 2
4. Fase 3
5. Fase 4
6. Fase 5
7. Fase 6

### Frontend (F0–F4 concluídas; **F5** em aberto)

1. **Fase F2:** modulo de segurancas na UI (concluída; backend Fase 2 já disponível).
2. **Fase F3:** modulo de indisponibilidades na UI (concluída; backend Fase 3 disponível).
3. **Fase F4** UI de escalas **+ `/app/sectors`** (concluída; backend Fases 4 e 5: geração + consultas por setor).
4. **Fase F5** alinhada a Fase 6 backend ou como hardening imediato da UI.

## Riscos e mitigacoes

- Risco: algoritmo gerar distribuicao injusta em meses criticos.
  - Mitigacao: testes parametrizados por calendario e metricas de equilibrio.
- Risco: acoplamento indevido entre camadas.
  - Mitigacao: revisao arquitetural por PR e testes por modulo.
- Risco: regressao ao ajustar regras.
  - Mitigacao: suite de testes de dominio ampla e obrigatoria antes de merge.
- Risco: divergencia entre contrato API e tipos do frontend.
  - Mitigacao: camada de API centralizada no `Web`, revisao ao mudar DTOs, testes de integracao ou contrato quando a trilha frontend estiver ativa.

## Checklist de conformidade final

### Backend

- [ ] Estrutura de pastas obrigatoria atendida.
- [ ] Commands, queries e endpoints obrigatorios implementados.
- [ ] Identity + JWT + roles `Admin` e `Supervisor` + **claim `tenant_id` no JWT**.
- [ ] Regras de negocio obrigatorias cobertas por testes.
- [ ] `ScheduleGeneratorService` em producao.
- [ ] Migrations para todas alteracoes de banco.
- [ ] Docker pronto para execucao.
- [ ] Swagger habilitado em desenvolvimento.

### Frontend (`src/Web`) — **parcial (F0–F4 ok; F5 pendente)**

- [x] Fase F0 da secao **Trilha Frontend React** concluida conforme `AGENTS.md` / `README.md`.
- [x] Fase **F1** estendida: **`/signup`**, sessão com **`tenantId`** no cliente.
- [x] Fase F2: modulo de segurancas em `/app/security-guards` (**setores por seguranca** onde implementado na UI); testes RTL do fluxo principal de listagem/criacao mockada; referencia Stitch a documentar ao validar no MCP (vide `README.md`).
- [x] Fase F3: indisponibilidades em `/app/unavailable-days`; testes RTL; referência Stitch no `README.md`.
- [x] Fase F4: escalas em `/app/schedules` (lista com setor quando existir escala; geração com mensagem quando a API retorna **`ScheduleCoverageFailed`**); **`/app/sectors`** para CRUD/visualização conforme perfil; referências Stitch principalmente das escalas no `README.md`.
- [ ] Fase **F5** concluída (qualidade global UX e integração na entrega).
- [x] Fluxo **Stitch** para telas novas da F1; F2+ segue `AGENTS.md`; referências citadas no README/PR quando aplicável.
- [x] Autenticacao JWT e rotas por perfil (`Admin` / `Supervisor`) funcionando na UI.
- [x] Modulo de escalas integrado aos endpoints documentados (**F4** UI).
- [x] Modulo **de setores** integrado aos endpoints **`/api/sectors`** (UI).
- [x] Modulo de indisponibilidades integrado (`F3` UI).
- [x] Testes Vitest + React Testing Library: **auth**, **segurancas**, **unavailable-days**, **schedules** e **sectors** (onde há suítes no repo).
