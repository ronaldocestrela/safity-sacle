# Roadmap de Implementacao - SafetyScale

## Objetivo

Entregar um monolito modular em .NET 10 com Clean Architecture, CQRS e TDD, capaz de gerar escalas mensais confiaveis com balanceamento justo de finais de semana, respeitando indisponibilidades e historico.

## Premissas obrigatorias

- Stack: ASP.NET Core Web API, EF Core, SQLite, Identity + JWT, MediatR, FluentValidation, Serilog, xUnit, FluentAssertions, Docker.
- Estrutura: `src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure`, `src/Tests`.
- Regras: sem logica de negocio em controller, Domain sem dependencia externa, migrations para toda mudanca de banco.
- Qualidade: TDD como fluxo padrao, cobertura de regras criticas e fluxo completo de geracao.

## Estrategia de entrega

- Incremental por fases, cada fase com criterio de pronto.
- Primeiro base arquitetural e seguranca, depois dominio e algoritmo, por fim operacao e hardening.
- Nenhuma feature avanca sem testes automatizados da propria fase.

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

## Plano de testes (obrigatorio por fase)

- Unitarios:
  - regras de negocio;
  - algoritmo de geracao;
  - balanceamento;
  - validadores.
- Integracao:
  - endpoints obrigatorios;
  - persistencia;
  - fluxo completo de geracao.
- Casos extremos:
  - poucos segurancas;
  - todos indisponiveis;
  - excesso de indisponibilidades;
  - mes com muitos finais de semana.

## Ordem sugerida de execucao

1. Fase 0
2. Fase 1
3. Fase 2
4. Fase 3
5. Fase 4
6. Fase 5
7. Fase 6

## Riscos e mitigacoes

- Risco: algoritmo gerar distribuicao injusta em meses criticos.
  - Mitigacao: testes parametrizados por calendario e metricas de equilibrio.
- Risco: acoplamento indevido entre camadas.
  - Mitigacao: revisao arquitetural por PR e testes por modulo.
- Risco: regressao ao ajustar regras.
  - Mitigacao: suite de testes de dominio ampla e obrigatoria antes de merge.

## Checklist de conformidade final

- [ ] Estrutura de pastas obrigatoria atendida.
- [ ] Commands, queries e endpoints obrigatorios implementados.
- [ ] Identity + JWT + roles `Admin` e `Supervisor`.
- [ ] Regras de negocio obrigatorias cobertas por testes.
- [ ] `ScheduleGeneratorService` em producao.
- [ ] Migrations para todas alteracoes de banco.
- [ ] Docker pronto para execucao.
- [ ] Swagger habilitado em desenvolvimento.
