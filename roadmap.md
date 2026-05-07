# Roadmap de Implementacao - SafetyScale

## Objetivo

Entregar um monolito modular em .NET 10 com Clean Architecture, CQRS e TDD, capaz de gerar escalas mensais confiaveis com balanceamento justo de finais de semana, respeitando indisponibilidades e historico, e uma **SPA React em `src/Web`** (especificada em `AGENTS.md`). **Bootstrap do frontend (Fase F0) esta concluido**; telas de negocio e auth na UI seguem nas fases F1-F5.

## Premissas obrigatorias

- Stack backend: ASP.NET Core Web API, EF Core, SQLite, Identity + JWT, MediatR, FluentValidation, Serilog, xUnit, FluentAssertions, Docker.
- **Stack frontend em andamento:** React 18+, TypeScript, Vite, React Router, cliente HTTP tipado (TanStack Query recomendado nas fases seguintes); testes com Vitest + React Testing Library — detalhes em `AGENTS.md`. **Fase F0** do `Web` concluida.
- Estrutura backend: `src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure`, `src/Tests`; **frontend previsto em `src/Web`**.
- Regras: sem logica de negocio em controller, Domain sem dependencia externa, migrations para toda mudanca de banco.
- Qualidade: TDD como fluxo padrao no backend; no frontend, testes obrigatorios nas partes criticas quando a trilha for iniciada.

## Estrategia de entrega

- Incremental por fases, cada fase com criterio de pronto.
- Primeiro base arquitetural e seguranca, depois dominio e algoritmo, por fim operacao e hardening.
- **Frontend:** F0 concluida; F1-F5 pendentes. Telas de negocio dependem das Fases 2-5 do backend. A trilha **F1-F5** em `Trilha Frontend React` abaixo permanece em execucao conforme o backend avanca.
- Nenhuma feature avanca sem testes automatizados da propria fase.

## Status atual

### Backend (API)

- [x] Fase 0 - Bootstrap e padroes (concluida)
- [x] Fase 1 - Persistencia e identidade (concluida)
- [x] Fase 2 - Modulo de Segurancas (concluida)
- [ ] Fase 3 - Modulo de indisponibilidades
- [ ] Fase 4 - Motor de geracao de escala
- [ ] Fase 5 - Consultas de escala e historico
- [ ] Fase 6 - Endurecimento, observabilidade e entrega

### Frontend (React em `src/Web`) — **parcial (F0 concluida)**

- [x] Fase F0 - Bootstrap e convencoes do `Web`
- [ ] Fase F1 - Autenticacao, sessao JWT e autorizacao por perfil na UI
- [ ] Fase F2 - Modulo de segurancas (telas alinhadas aos endpoints da Fase 2 backend)
- [ ] Fase F3 - Modulo de indisponibilidades
- [ ] Fase F4 - Modulo de escalas (geracao e consultas)
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

## Fase 6 - Endurecimento, observabilidade e entrega

### Entregaveis

- Dockerfile e `docker-compose.yml`.
- Padronizacao final de logs (autenticacao, exceptions, geracao de escala).
- Documentacao de execucao local e fluxo basico de uso.
- Revisao de seguranca e autorizacao por perfil.

### Tarefas principais

- Ajustar health checks e configuracoes por ambiente.
- Executar bateria completa de testes unitarios + integracao.
- Revisar validacoes fora de controllers (100% em validators/handlers/domain).

### Criterio de pronto

- Aplicacao sobe via Docker.
- Pipeline local de testes verde.
- Checklist de conformidade do `AGENTS.md` atendido.

## Trilha Frontend React (F0 concluida; F1-F5 pendentes)

> A **Fase F0** esta implementada no repositorio (`src/Web`, proxy Vite, CORS na API em Development, porta dev `4863` — ver `README.md`). **Fases F1 a F5** seguem a ordem abaixo, desbloqueando F2-F4 conforme endpoints backend correspondentes estiverem prontos.

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

- Tratar 401/403 com redirecionamento ou mensagens claras.
- Mapear permissoes espelhando `AGENTS.md` (Admin: gestao + geracao + visualizacao; Supervisor: visualizacao e consulta).

#### Criterio de pronto

- Usuario consegue autenticar e acessar apenas rotas permitidas ao seu perfil; testes minimos nos guards/hooks de auth.

### Fase F2 - Modulo de segurancas (UI)

#### Entregaveis

- Telas: listagem, criacao, edicao, inativacao de segurancas, alinhadas a:
  - `POST /api/security-guards`
  - `GET /api/security-guards`
  - `PUT /api/security-guards/{id}`
  - `PATCH /api/security-guards/{id}/inactive`

#### Tarefas principais

- Formularios com validacao de UX; feedback de erros da API (422, etc.).
- Estado de lista coerente com backend (incl. seguranca inativo visivel conforme regra de produto).

#### Criterio de pronto

- Fluxo completo usavel por `Admin`; testes de componente nas telas criticas.

### Fase F3 - Modulo de indisponibilidades (UI)

#### Entregaveis

- Cadastro, listagem e exclusao de indisponibilidades por seguranca:
  - `POST /api/security-guards/{id}/unavailable-days`
  - `GET /api/security-guards/{id}/unavailable-days`
  - `DELETE /api/unavailable-days/{id}`

#### Tarefas principais

- Evitar duplicidade de data na UX quando a API rejeitar; datas e motivo opcional alinhados ao contrato.

#### Criterio de pronto

- Integracao completa com Fase 3 backend; testes cobrindo happy path e erro de validacao.

### Fase F4 - Modulo de escalas (UI)

#### Entregaveis

- Acionar geracao: `POST /api/schedules/generate`.
- Consultas: `GET /api/schedules/{id}` e `GET /api/schedules/month/{month}/year/{year}`.
- Visualizacao clara de itens (datas, fim de semana, seguranca).

#### Tarefas principais

- Parametros mes/ano, feedback de geracao (loading, sucesso, falha auditavel na UI).
- Respeitar quem pode gerar (`Admin`) vs somente leitura (`Supervisor`) na interface.

#### Criterio de pronto

- Fluxo de geracao e consulta usavel; testes nos componentes de listagem/detalhe principais.

### Fase F5 - Qualidade, UX e integracao na entrega

#### Entregaveis

- Tratamento global de erros, acessibilidade basica, consistencia visual.
- Cobertura de testes Vitest + RTL nas areas de auth e um fluxo de negocio principal.
- Opcional: servico `Web` no `docker-compose.yml` junto da API.

#### Tarefas principais

- Revisar performance de listas e chamadas (TanStack Query).
- Documentar no README o fluxo usuario final (login → cadastros → escala).

#### Criterio de pronto

- Checklist frontend do `AGENTS.md` atendido; build de producao do `Web` documentado.

## Plano de testes (obrigatorio por fase)

- Unitarios (backend):
  - regras de negocio;
  - algoritmo de geracao;
  - balanceamento;
  - validadores.
- Integracao (backend):
  - endpoints obrigatorios;
  - persistencia;
  - fluxo completo de geracao.
- Casos extremos:
  - poucos segurancas;
  - todos indisponiveis;
  - excesso de indisponibilidades;
  - mes com muitos finais de semana.
- **Frontend:** trilha `Web` iniciada (**F0** concluida); Vitest + React Testing Library em hooks/guards e fluxos criticos nas fases seguintes; opcional E2E (Playwright) na Fase F5.

## Ordem sugerida de execucao

### Backend

1. Fase 0
2. Fase 1
3. Fase 2
4. Fase 3
5. Fase 4
6. Fase 5
7. Fase 6

### Frontend (F0 concluida; **F1-F5** em aberto)

1. **Fase F1** (JWT e perfis na UI) — proxima da trilha frontend.
2. **Fase F2** quando Fase 2 backend estiver disponivel (ja concluida no backend).
3. Fase F3 apos Fase 3 backend.
4. Fase F4 apos Fases 4 e 5 backend (geracao + consultas).
5. Fase F5 alinhada a Fase 6 backend ou logo apos F4 frontend.

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
- [ ] Identity + JWT + roles `Admin` e `Supervisor`.
- [ ] Regras de negocio obrigatorias cobertas por testes.
- [ ] `ScheduleGeneratorService` em producao.
- [ ] Migrations para todas alteracoes de banco.
- [ ] Docker pronto para execucao.
- [ ] Swagger habilitado em desenvolvimento.

### Frontend (`src/Web`) — **parcial (F0 ok; F1-F5 pendentes)**

- [x] Fase F0 da secao **Trilha Frontend React** concluida conforme `AGENTS.md` / `README.md`.
- [ ] Trilha F1-F5 concluida (auth na UI, modulos de negocio, qualidade F5).
- [ ] Autenticacao JWT e rotas por perfil (`Admin` / `Supervisor`) funcionando na UI.
- [ ] Modulos de segurancas, indisponibilidades e escalas integrados aos endpoints documentados.
- [ ] Testes Vitest + React Testing Library nas areas criticas (auth + pelo menos um fluxo de negocio).
