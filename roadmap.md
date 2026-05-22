# Roadmap de Implementacao - SafetyScale

## Objetivo

Entregar um monolito modular em .NET 10 com Clean Architecture, CQRS e TDD, capaz de gerar escalas mensais confiaveis com balanceamento justo de finais de semana, respeitando indisponibilidades e historico, e uma **SPA React em `src/Web`** (especificada em `AGENTS.md`). **Bootstrap do frontend (Fase F0), auth na UI com cadastro público de empresa (`/signup`),** **módulo de seguranças na UI (Fase F2)**, **módulo de indisponibilidades na UI (Fase F3)** e **módulo de escalas na UI (Fase F4)** estão concluídos; hardening de UX e qualidade segue na **Fase F5** (UI). No backend, **Fases 0 a 5 estão concluídas**, com **isolamento multitenant lógico** (Tenant + JWT `tenant_id`) **e `POST /api/tenants/register`** — falta **Fase 6** (endurecimento operacional ampla, incluindo proteções para endpoints públicos e entrega Docker).

## Premissas obrigatorias

- Stack backend: ASP.NET Core Web API, EF Core, SQLite, Identity + JWT, MediatR, FluentValidation, Serilog, xUnit, FluentAssertions, Docker.
- **Stack frontend em andamento:** React 18+, TypeScript, Vite, React Router, cliente HTTP tipado (TanStack Query recomendado nas fases seguintes); testes com Vitest + React Testing Library — detalhes em `AGENTS.md`. **Fases F0–F4** do `Web` concluídas (bootstrap, auth com **`/signup`**, seguranças UI, indisponibilidades UI, escalas UI).
- Estrutura backend: `src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure`, `src/Tests`; **frontend previsto em `src/Web`**.
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
- [x] Fase F4 - Modulo de escalas (geracao e consultas)
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

- Banco SQLite criado via migration.
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
- Testes de integracao dos endpoints com persistencia real em SQLite de teste.

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
- [x] Testes de integracao dos tres endpoints passando (`TestWebApplicationFactory` com SQLite dedicado por instância para isolar paralelismo)

## Fase 4 - Motor de geracao de escala (Core de negocio)

### Entregaveis

- `ScheduleGeneratorService` implementado.
- `GenerateMonthlyScheduleCommand`.
- Regras obrigatorias aplicadas:
  - nao escalar indisponiveis;
  - nao escalar inativos;
  - balancear sabados/domingo;
  - balancear total de plantoes;
  - evitar conflitos e concentracao.
- Fluxo de geracao em 6 etapas (carregar, separar, distribuir fim de semana, distribuir dias uteis, validar, persistir).

### Tarefas principais (TDD primeiro)

- Criar suite de testes de dominio para:
  - indisponibilidade;
  - balanceamento de finais de semana;
  - balanceamento geral;
  - intervalo entre plantoes.
- Implementar algoritmo greedy com criterios:
  1. menor qtd de finais de semana;
  2. menor qtd total de plantoes;
  3. maior intervalo desde ultimo plantao.
- Persistir `MonthlySchedule` e `ScheduleItem` atomicamente.

### Criterio de pronto

- Casos obrigatorios de escala validados por testes automatizados.
- Geracao mensal funcional por comando, sem duplicidade no mesmo dia.
- Logs de geracao e falhas auditaveis via Serilog.

### Status de entrega da fase

- [x] `ScheduleGeneratorService` no domínio (greedy: fins de semana primeiro; critérios de desempate agents.md)
- [x] Command + validator: `GenerateMonthlyScheduleCommand` (`Month`/`Year`), FluentValidation 1–12 e ano 2000–2100
- [x] Endpoint `POST /api/schedules/generate` (`Admin` apenas); `409` se mês/ano já gerado (índice único DB + checagem); `400` sem guardas ativos ou cobertura impossível
- [x] Persistência: `MonthlySchedule` + `ScheduleItem` num único `SaveChanges`; `GetActiveAsync` em seguranças; `GetByDateRangeAsync` em indisponibilidades
- [x] Migration: índice único **`(TenantId, Month, Year)`** em `MonthlySchedules` (com isolamento multitenant; substitui o índice antigo apenas mes/ano)
- [x] Testes: domínio do gerador, handler CQRS, validator e integração API (`SchedulesGenerateEndpointsTests`)

## Fase 5 - Consultas de escala e historico

### Entregaveis

- Queries:
  - `GetMonthlyScheduleQuery`
  - `GetMonthlySchedulesQuery`
- Endpoints:
  - `GET /api/schedules/{id}`
  - `GET /api/schedules/month/{month}/year/{year}`
- Projecoes de leitura com dados de seguranca e marcacao de fim de semana.

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
- [x] Resposta com DTOs: cabeçalho da escala e itens ordenados por data, com dados do segurança (id, nome, `IsActive`), `Date` e `IsWeekend`; leitura EF com `Include` / `ThenInclude(SecurityGuard)` e `AsNoTracking`
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

> A **Fase F0** está implementada no repositório (`src/Web`, proxy Vite, CORS na API em Development, porta dev `4863` — ver `README.md`). As **Fases F1 a F4** (auth JWT na UI; seguranças; indisponibilidades; escalas em `/app/schedules`) estão implementadas. **Fase F5** segue abaixo.

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
- Visualizacao clara de itens (datas, fim de semana, seguranca).

#### Tarefas principais

- **Stitch (padrão):** antes das telas de geração e consulta de escalas, gerar ou revisar referência no MCP `user-stitch` (`AGENTS.md`).
- Parametros mes/ano, feedback de geracao (loading, sucesso, falha auditavel na UI).
- Respeitar quem pode gerar (`Admin`) vs somente leitura (`Supervisor`) na interface.

#### Criterio de pronto

- Fluxo de geracao e consulta usavel; testes nos componentes de listagem/detalhe principais.
- Referências Stitch revisadas e citadas no PR quando aplicável.

#### Status de entrega da fase

- [x] Rota `/app/schedules` com consulta por mês/ano e lista de itens; `Admin`: geração (`POST /api/schedules/generate`); `Supervisor`: sem geração
- [x] Referência Stitch: **Regras de Escala** (`projects/9334796298126275303/screens/e1026c6a3524415ca5f749c9496b2f5e`)
- [x] Testes Vitest + RTL em `features/schedules`

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
  - endpoints obrigatorios;
  - persistencia;
  - fluxo completo de geracao;
  - consultas `GET` de escala por id e por mes/ano (`SchedulesQueryEndpointsTests`).
  - **Multitenancy / onboarding:** registros públicos (`TenantsRegistrationEndpointsTests`) e isolamento entre tenants onde aplicável.
- Casos extremos:
  - poucos segurancas;
  - todos indisponiveis;
  - excesso de indisponibilidades;
  - mes com muitos finais de semana.
- **Frontend:** Fases **F0–F4** concluídas (`Web`); Vitest + React Testing Library em guards/login, `security-guards`, `unavailable-days` e `schedules`; opcional E2E (Playwright) na Fase F5.

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
3. **Fase F4** UI de escalas (concluída; backend Fases 4 e 5: geração + consultas).
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
- [x] Fase F2: modulo de segurancas em `/app/security-guards`; testes RTL do fluxo principal de listagem/criacao mockada; referencia Stitch a documentar ao validar no MCP (vide `README.md`).
- [x] Fase F3: indisponibilidades em `/app/unavailable-days`; testes RTL; referência Stitch no `README.md`.
- [x] Fase F4: escalas em `/app/schedules`; testes RTL; referência Stitch no `README.md`.
- [ ] Fase **F5** concluída (qualidade global UX e integração na entrega).
- [x] Fluxo **Stitch** para telas novas da F1; F2+ segue `AGENTS.md`; referências citadas no README/PR quando aplicável.
- [x] Autenticacao JWT e rotas por perfil (`Admin` / `Supervisor`) funcionando na UI.
- [x] Modulo de escalas integrado aos endpoints documentados (**F4** UI).
- [x] Modulo de indisponibilidades integrado (`F3` UI).
- [x] Testes Vitest + React Testing Library: **auth**, **segurancas**, **unavailable-days** e **schedules**.
