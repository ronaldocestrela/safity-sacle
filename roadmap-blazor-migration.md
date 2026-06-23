# Roadmap de Migração — React → Blazor (SafetyScale Web)

## Objetivo

Substituir gradualmente a SPA **React** em `src/Web` por uma SPA **Blazor WebAssembly** em `src/Web.Blazor`, **preservando a identidade visual** (tokens Stitch, Material Symbols, Inter, CSS por tela) e **paridade funcional** com as rotas e fluxos já entregues nas fases F0–F4 do frontend React.

A migração é **incremental e calma**: o React permanece em produção até a fase de cutover; cada etapa fecha com critério de pronto, testes e revisão visual antes de avançar.

## Escopo

### Dentro do escopo

- Novo projeto `src/Web.Blazor` (**Blazor WebAssembly Standalone** — ver [ADR 001](docs/adr/001-blazor-wasm-frontend.md)).
- Paridade de rotas, perfis (`Admin` / `Supervisor`), chamadas REST existentes e mensagens de erro da API.
- Port do CSS Modules React para **scoped CSS** (`.razor.css`) ou CSS estático em `wwwroot`.
- Testes com **bUnit** nas partes críticas (auth, rotas, telas principais).
- Atualização de Docker Compose, Nginx (se aplicável) e documentação operacional.
- Descomissionamento controlado de `src/Web` (React) após cutover.

### Fora do escopo (nesta trilha)

- Mudanças de contrato ou regra de negócio na API (`src/Api`).
- Novas features de produto (Fase F5 UX do React **não** será implementada duas vezes — priorizar paridade F0–F4; melhorias entram só no Blazor após cutover).
- Reescrita do backend para Blazor Server ou Minimal APIs no lugar da Web API atual.

## Premissas e decisões

> Decisões formalizadas na fase **B0.1** — detalhes completos em [ADR 001 — Frontend Blazor WebAssembly Standalone](docs/adr/001-blazor-wasm-frontend.md).

| Decisão | Escolha | Observação |
|---|---|---|
| Modelo Blazor | **WebAssembly Standalone** (.NET 10) | Não Hosted; não Blazor Server — ver ADR 001 |
| Projeto | `SafetyScale.Web.Blazor` em `src/Web.Blazor/` | React legado em `src/Web/` até B11 |
| Solution | `SafetyScale.sln` na raiz | Criada na B1.1 (6 projetos .NET) |
| Hospedagem | Build estático WASM + Nginx | Substituir pipeline Node/Vite na B10 |
| Auth | JWT em `sessionStorage` via **JS interop** mínimo | Paridade com React; claim `tenant_id` |
| Estilo | Scoped CSS (`.razor.css`) + `wwwroot/css/app.css` | 1:1 com React; sem lib de UI nesta migração |
| Cliente HTTP | `HttpClient` + `DelegatingHandler` | Equivalente a `shared/api/http.ts` |
| Config API | **`ApiBaseUrl`** em `wwwroot/appsettings.json` | Substitui `VITE_API_BASE_URL` — ver matriz abaixo |
| Testes UI | **bUnit** + smoke E2E onde fizer sentido | Substituir Vitest/RTL gradualmente |
| Coexistência dev | React **4863**, Blazor **4864**, API **5003** | CORS dual: `4863` + `4864` em Development |
| Convenções do time | [docs/frontend-blazor-conventions.md](docs/frontend-blazor-conventions.md) | B0.3 — estrutura, nomenclatura, CSS, freeze React |

### Matriz `ApiBaseUrl`

| Ambiente | Valor | Mecanismo | CORS |
|---|---|---|---|
| Dev local (Blazor) | `http://localhost:5003` | `HttpClient` direto à API | Origem `http://localhost:4864` |
| Dev local (React) | *(vazio)* | Proxy Vite `/api` | Origem `4863` (já existe) |
| Produção / Compose | *(vazio)* | Nginx proxy `/api` → `api:8080` | Desligado (same-origin) |
| Split origin (opcional) | URL absoluta da API | Cross-origin | `CORS_ORIGINS` na API |

## Estratégia de entrega

1. **Uma fase por vez** — não iniciar a fase seguinte sem cumprir o critério de pronto da anterior.
2. **Paridade antes de polish** — comportamento e layout primeiro; refinamentos de UX depois do cutover.
3. **Referência visual** — usar as telas Stitch já documentadas no `README.md` e comparar lado a lado com o React.
4. **Checklist por tela** — Admin vs Supervisor, loading, empty state, erro 400/401/403/409, mobile (bottom nav).
5. **Sem duplicar F5 no React** — congelar evolução do React após início da fase B4, salvo correções críticas.

## Inventário de referência (React → Blazor)

| Rota | React (origem) | Blazor (destino sugerido) |
|---|---|---|
| `/` | `features/home/HomePage` | `Pages/Home.razor` |
| `/login` | `features/auth/LoginPage` | `Pages/Auth/Login.razor` |
| `/signup` | `features/tenant-registration/RegisterTenantPage` | `Pages/Auth/RegisterTenant.razor` |
| `/app` | `features/app/WelcomePage` | `Pages/App/Welcome.razor` |
| `/app/access-denied` | `app/routes/AccessDeniedPage` | `Pages/App/AccessDenied.razor` |
| `/app/sectors` | `features/sectors/SectorsPage` | `Pages/App/Sectors.razor` |
| `/app/security-guards` | `features/security-guards/SecurityGuardsPage` | `Pages/App/SecurityGuards.razor` |
| `/app/unavailable-days` | `features/unavailable-days/UnavailableDaysPage` | `Pages/App/UnavailableDays.razor` |
| `/app/schedules` | `features/schedules/SchedulesPage` | `Pages/App/Schedules.razor` |
| Layout shell | `app/AppLayout` | `Layout/AppLayout.razor` |
| Header Stitch | `shared/components/AppHeader` | `Components/AppHeader.razor` |
| Calendário | `shared/components/calendar/MonthCalendar` | `Components/Calendar/MonthCalendar.razor` |

## Status geral

- [x] **B0** — Decisão, spike e alinhamento
- [ ] **B1** — Bootstrap do projeto Blazor
- [ ] **B2** — Infra compartilhada (HTTP, auth, erros, config)
- [ ] **B3** — Layout, roteamento e navegação
- [ ] **B4** — Telas públicas (home, login, signup)
- [ ] **B5** — Área autenticada base (dashboard, access denied)
- [ ] **B6** — Módulo setores
- [ ] **B7** — Módulo seguranças
- [ ] **B8** — Módulo indisponibilidades
- [ ] **B9** — Módulo escalas
- [ ] **B10** — Testes finais, deploy e cutover
- [ ] **B11** — Descomissionamento do React

---

## B0 — Decisão, spike e alinhamento

**Objetivo:** Validar viabilidade técnica e fixar convenções antes de portar telas reais.

**Duração sugerida:** 2–3 dias

### B0.1 — Decisões de arquitetura

- [x] Confirmar **Blazor WASM Standalone** (alternativas descartadas: Blazor Server, WASM Hosted) — [ADR 001](docs/adr/001-blazor-wasm-frontend.md).
- [x] Definir nome do projeto: `SafetyScale.Web.Blazor` em `src/Web.Blazor`.
- [x] Definir porta de dev exclusiva **4864** para coexistir com React (**4863**).
- [x] Definir estratégia de `ApiBaseUrl` (`wwwroot/appsettings.json`; dev = `http://localhost:5003`; prod = vazio).

**Gate B0.2 (validado em B0.1):**

- [x] Por que não Blazor Server? — SignalR + auth incompatível com JWT em `sessionStorage` e deploy estático.
- [x] Por que não WASM Hosted? — Host ASP.NET extra desnecessário; produção usa Nginx.
- [x] Como dev Blazor fala com API sem proxy Vite? — `ApiBaseUrl` = `http://localhost:5003` + CORS origem `4864`.
- [x] Como produção evita CORS? — `ApiBaseUrl` vazio; Nginx proxy `/api` same-origin.
- [x] React e Blazor coexistem em quais portas? — **4863** (React) e **4864** (Blazor); API **5003**.
- [x] Qual variável substitui `VITE_API_BASE_URL`? — **`ApiBaseUrl`** em `wwwroot/appsettings.json`.

### B0.2 — Spike técnico (POC mínima)

- [x] Criar projeto WASM em `src/Web.Blazor` com página spike que chama `GET /api/health` (401 sem token; 200 após login).
- [x] Validar **JS interop** para ler/gravar JWT em `sessionStorage` (chave `safetyscale.auth.session`).
- [x] Validar dev Blazor com `ApiBaseUrl` = `http://localhost:5003` + CORS origem **4864** (sem proxy Vite — ver ADR 001).
- [x] Validar carregamento de **Material Symbols** + fonte **Inter** no `index.html` / spike UI.

> Spike: [`src/Web.Blazor/README.md`](src/Web.Blazor/README.md) — `dotnet run --project src/Web.Blazor` na porta **4864**.

### B0.3 — Convenções do time

- [x] Documentar estrutura de pastas Blazor (ver B1) — [docs/frontend-blazor-conventions.md](docs/frontend-blazor-conventions.md).
- [x] Definir padrão de nomenclatura: `*ApiClient`, `*Dto`, páginas em `Pages/App/`.
- [x] Definir regra: **scoped CSS** 1:1 com o React; não introduzir biblioteca de UI nesta migração.
- [x] Acordar freeze do React (a partir de B4) — política documentada no guia e em `agents.md`.

### Critério de pronto (B0)

- POC sobe localmente, consome a API e persiste token em `sessionStorage`.
- Decisões registradas neste arquivo (seção Premissas) sem pendências bloqueantes.
- Convenções do time publicadas e referenciadas (B0.3).

---

## B1 — Bootstrap do projeto Blazor

**Objetivo:** Esqueleto compilável, integrado à solution .NET, com estilos globais base.

**Duração sugerida:** 2–4 dias

### B1.1 — Projeto e solution

- [x] Projeto Blazor validado em `src/Web.Blazor` (criado na B0.2; **não** recriar com `dotnet new`).
- [x] [`SafetyScale.sln`](SafetyScale.sln) na raiz com 6 projetos (Domain, Application, Infrastructure, Api, Tests, Web.Blazor).
- [x] [`Directory.Build.props`](Directory.Build.props) mínimo na raiz (`net10.0`, `Nullable`, `ImplicitUsings`, `LangVersion`).
- [x] **Sem** projeto Contracts compartilhado — DTOs espelhados em `Models/`; WASM **sem** `ProjectReference` ao backend.

### B1.2 — Estrutura de pastas

```text
src/Web.Blazor/
 ├── Components/          # AppHeader, MonthCalendar, etc.
 ├── Layout/              # AppLayout, MainLayout público
 ├── Pages/
 │   ├── App/             # área autenticada
 │   └── Auth/            # login, signup
 ├── Services/
 │   ├── Api/             # *ApiClient, Http setup
 │   └── Auth/            # JwtSessionStorage, AuthStateProvider
 ├── Models/              # DTOs espelhando JSON camelCase da API
 ├── wwwroot/
 │   ├── css/
 │   └── icons.svg        # copiar de src/Web/public
 └── Program.cs
```

- [x] Criar pastas-alvo (`Components/`, `Pages/App/`, `Pages/Auth/`, `Services/Api/`, `Services/Auth/`, `Models/`).
- [x] Mover serviços para `Services/Api/` e `Services/Auth/` com namespaces atualizados.
- [x] Preservar POC B0.2 em `Pages/Home.razor`; placeholders em Auth/App; remover `NavMenu` do template.
- [x] Copiar `icons.svg` para `wwwroot/`; atualizar `_Imports.razor`, `Program.cs` e README.

### B1.3 — Estilos globais

- [x] Portar `src/Web/src/index.css` → `wwwroot/css/app.css` (base global SafetyScale).
- [x] Confirmar links Material Symbols e Inter no `wwwroot/index.html`.
- [x] Confirmar `wwwroot/icons.svg` (copiado na B1.2).
- [x] Remover sobras do template (Bootstrap/validação); manter `.spike-*` temporário e shell Blazor (loading/error).

### B1.4 — Dev experience

- [x] Confirmar `launchSettings.json` (porta 4864, `Development`).
- [x] Documentar estratégia **CORS + ApiBaseUrl absoluto** (sem proxy WASM; ver ADR 001).
- [x] Script raiz [`scripts/dev-blazor.sh`](scripts/dev-blazor.sh) + seção no README raiz para subir API + Blazor.

### Critério de pronto (B1)

- `dotnet build` e `dotnet run --project src/Web.Blazor` OK.
- Página placeholder renderiza com fontes e ícones corretos.
- Projeto listado na solution sem quebrar CI existente.

---

## B2 — Infra compartilhada (HTTP, auth, erros, config)

**Objetivo:** Equivalente Blazor de `shared/api`, `shared/auth` e `shared/config`.

**Duração sugerida:** 3–5 dias

### B2.1 — Configuração

- [ ] `AppConfiguration` lendo `ApiBaseUrl` (vazio = URLs relativas `/api/...`).
- [ ] Documentar paridade com `VITE_API_BASE_URL` no `.env.example`.

### B2.2 — Cliente HTTP

- [ ] Registrar `HttpClient` com base address correta.
- [ ] Implementar `BearerTokenHandler` (injeta `Authorization` quando há sessão).
- [ ] Implementar tratamento de **401** → limpar sessão + navegar para `/login` (equivalente a `setOnUnauthorized`).
- [ ] Implementar `ReadApiError` para corpo JSON de validação (`errors` FluentValidation).

### B2.3 — JWT e sessão

- [ ] Portar lógica de `shared/auth/jwt.ts` → serviço C# (parse claims: roles, `tenant_id`, exp).
- [ ] `JwtSessionStorage` com JS interop (`sessionStorage`).
- [ ] Modelo `AuthSession` (email, roles, tenantId, token).
- [ ] `CustomAuthStateProvider` implementando `AuthenticationStateProvider`.
- [ ] Métodos `LoginAsync`, `LogoutAsync`, `GetSession()`.

### B2.4 — DTOs e tipos

- [ ] Criar DTOs espelhando:
  - auth login request/response;
  - tenant register;
  - sectors, security guards, unavailable days, schedules;
  - `ScheduleCoverageFailureResponse`.
- [ ] Garantir `JsonSerializerOptions` com **camelCase** e case-insensitive.

### B2.5 — Testes (bUnit + unit)

- [ ] Testes unitários do parser JWT (portar casos de `jwt.test.ts`).
- [ ] Testes unitários de persistência de sessão (mock interop).
- [ ] Teste do handler 401 (HttpMessageHandler mock).

### Critério de pronto (B2)

- Login manual via serviço (sem UI final) persiste token e preenche `AuthenticationState`.
- Request autenticado chega na API com header Bearer.
- Testes B2 passando no CI .NET.

---

## B3 — Layout, roteamento e navegação

**Objetivo:** Shell autenticado e guards de rota equivalentes a `ProtectedRoute` e `RoleRoute`.

**Duração sugerida:** 3–4 dias

### B3.1 — Roteamento

- [ ] Configurar rotas no `Router` espelhando `src/Web/src/app/routes.tsx`.
- [ ] Fallback `*` → `/`.
- [ ] Layout diferenciado: público vs `/app`.

### B3.2 — Autorização de rotas

- [ ] Página `/app/*` exige autenticação (`AuthorizeRouteView` ou wrapper).
- [ ] Componente `RoleAuthorizeView` (roles `Admin`, `Supervisor`) → redireciona para `/app/access-denied`.
- [ ] Portar matriz de permissões:

| Rota | Admin | Supervisor |
|---|---|---|
| `/app` | sim | sim |
| `/app/sectors` | sim | sim |
| `/app/security-guards` | sim | sim |
| `/app/unavailable-days` | sim | sim |
| `/app/schedules` | sim | sim |
| Escrita CRUD | Admin | — |

### B3.3 — AppLayout (shell)

- [ ] Portar `AppLayout.tsx` + `AppLayout.module.css` → `Layout/AppLayout.razor`.
- [ ] Bottom navigation com 5 itens (Dashboard, Sectors, Guards, Availability, Schedules).
- [ ] Estados ativo/inativo dos ícones Material (FILL 1 quando ativo).
- [ ] Header condicional (oculto nas telas Stitch, igual ao React).
- [ ] Botão Sair → `LogoutAsync`.

### B3.4 — Testes

- [ ] bUnit: usuário não autenticado em `/app` → redirect login.
- [ ] bUnit: Supervisor em rota Admin-only de escrita (preparar para B7 — UI desabilitada, API continua mandando).
- [ ] bUnit: bottom nav marca item ativo.

### Critério de pronto (B3)

- Navegação entre rotas placeholder autenticadas funciona.
- Guards de auth/role com paridade ao React.
- Layout visual comparável ao React (screenshot diff manual).

---

## B4 — Telas públicas (home, login, signup)

**Objetivo:** Fluxos anônimos completos.

**Duração sugerida:** 4–5 dias

### B4.1 — Home (`/`)

- [ ] Portar `HomePage.tsx` + CSS.
- [ ] Smoke opcional da API (`/api/health`) — paridade com `apiSmoke.ts`.
- [ ] Links para login e signup.

### B4.2 — Login (`/login`)

- [ ] Portar `LoginPage.tsx` + CSS (referência Stitch Login de Acesso).
- [ ] Formulário e-mail/senha → `POST /api/auth/login`.
- [ ] Erros: credenciais inválidas, rede, token inválido.
- [ ] Redirect para `/app` após sucesso.
- [ ] Query/state: mensagem pós-signup e sessão expirada (`reason=session-expired`).
- [ ] Link “Cadastrar minha empresa” → `/signup`.

### B4.3 — Signup (`/signup`)

- [ ] Portar `RegisterTenantPage.tsx` + CSS.
- [ ] Formulário → `POST /api/tenants/register`.
- [ ] Tratar 400 validação, 409 e-mail/slug.
- [ ] Redirect para `/login` com e-mail pré-preenchido.

### B4.4 — Testes

- [ ] bUnit Login: submit com mock HTTP 200 → navega para `/app`.
- [ ] bUnit Login: 401 → mensagem de erro.
- [ ] bUnit Signup: 409 → mensagem amigável.

### Critério de pronto (B4)

- Fluxo completo signup → login → `/app` (mesmo com dashboard placeholder).
- Paridade visual com React/Stitch aceita pelo time.
- **Freeze** de novas features no React a partir daqui.

---

## B5 — Área autenticada base (dashboard, access denied)

**Objetivo:** Primeira tela útil pós-login e tratamento de permissão negada.

**Duração sugerida:** 3–4 dias

### B5.1 — Welcome / Dashboard (`/app`)

- [ ] Portar `WelcomePage.tsx` + CSS.
- [ ] Exibir dados da sessão (e-mail, perfil, tenant).
- [ ] Detalhe do dia / resumo se existir no React (consultar API de escala do mês corrente).

### B5.2 — Access denied (`/app/access-denied`)

- [ ] Portar `AccessDeniedPage.tsx` + CSS.
- [ ] Link de volta ao dashboard.

### B5.3 — AppHeader compartilhado

- [ ] Portar `AppHeader.tsx` + CSS para uso nas telas administrativas.
- [ ] Props: título, subtítulo, ações (logout).

### B5.4 — Testes

- [ ] bUnit Welcome: renderiza e-mail do usuário autenticado mock.
- [ ] bUnit AccessDenied: link para `/app`.

### Critério de pronto (B5)

- Dashboard útil para validar sessão multitenant (`tenantId` visível ou inferido).
- Rotas protegidas integradas ao layout shell.

---

## B6 — Módulo setores (`/app/sectors`)

**Objetivo:** CRUD de setores e `requiredGuardsPerDay`.

**Duração sugerida:** 4–5 dias

### B6.1 — Cliente API

- [ ] `SectorsApiClient`: list, create, update, inactive, active.
- [ ] DTOs e filtros (`isActive`).

### B6.2 — UI

- [ ] Portar `SectorsPage.tsx` + CSS.
- [ ] Lista com filtros; formulário create/edit.
- [ ] Admin: todas ações; Supervisor: somente leitura.
- [ ] Validação UX (nome, vagas ≥ 1); exibir erros 400 da API.

### B6.3 — Testes

- [ ] bUnit: Supervisor não vê botões de escrita.
- [ ] bUnit: Admin cria setor (HTTP mock).
- [ ] bUnit: empty state sem setores.

### Critério de pronto (B6)

- Paridade funcional com `SectorsPage.test.tsx` do React.
- Revisão visual lado a lado com React.

---

## B7 — Módulo seguranças (`/app/security-guards`)

**Objetivo:** Tela mais complexa do sistema — lista, filtros, setores por segurança, ativar/inativar.

**Duração sugerida:** 5–7 dias

### B7.1 — Cliente API

- [ ] `SecurityGuardsApiClient`: list, create, update, inactive, active, set sectors.
- [ ] Integração com catálogo de setores (`SectorsApiClient`).

### B7.2 — UI — listagem e filtros

- [ ] Portar `SecurityGuardsPage.tsx` + CSS (referência Stitch Gestão de Seguranças).
- [ ] Chips all/active; filtro por setor; busca por nome.
- [ ] Banner success/error.

### B7.3 — UI — formulários e modais

- [ ] Modal/sheet create e edit (nome).
- [ ] Multi-select de setores (Admin).
- [ ] Confirmação de inativação.
- [ ] Reativar segurança inativo.

### B7.4 — Permissões

- [ ] Supervisor: lista + filtros apenas.
- [ ] Admin: CRUD completo + setores.

### B7.5 — Testes

- [ ] Portar cenários principais de `SecurityGuardsPage.test.tsx`.
- [ ] Teste de atribuição de setores (mock PUT sectors).

### Critério de pronto (B7)

- Todos os fluxos Admin/Supervisor validados manualmente.
- Nenhuma regressão nos filtros e estados loading/error.

---

## B8 — Módulo indisponibilidades (`/app/unavailable-days`)

**Objetivo:** Calendário mensal, seleção de segurança, batch save (SAVE RESTRICTIONS).

**Duração sugerida:** 5–7 dias

### B8.1 — Componente calendário

- [ ] Portar `monthGrid.ts` → serviço/helper C#.
- [ ] Portar `MonthCalendar.tsx` + CSS.
- [ ] Seleção de dias, estado indisponível, navegação mês anterior/próximo.

### B8.2 — Cliente API

- [ ] `UnavailableDaysApiClient`: list by guard, add, delete.
- [ ] Carregar lista de seguranças para seletor.

### B8.3 — UI da página

- [ ] Portar `UnavailableDaysPage.tsx` + CSS (referência Stitch Cadastro de Indisponibilidade).
- [ ] Admin: alterações locais + **SAVE RESTRICTIONS** (batch).
- [ ] Supervisor: calendário somente leitura.

### B8.4 — Testes

- [ ] Unit: `monthGrid` equivalente.
- [ ] bUnit: Supervisor sem botão save.
- [ ] bUnit: Admin marca dias e dispara saves (mock).

### Critério de pronto (B8)

- Paridade com fluxo “draft local → save” do React.
- Calendário responsivo igual ao mock mobile/desktop.

---

## B9 — Módulo escalas (`/app/schedules`)

**Objetivo:** Consulta mensal, geração Admin, exibição de `ScheduleCoverageFailed`.

**Duração sugerida:** 4–6 dias

### B9.1 — Cliente API

- [ ] `SchedulesApiClient`: get by month/year, generate.
- [ ] Parse de erro `ScheduleCoverageFailureResponse`.

### B9.2 — UI

- [ ] Portar `SchedulesPage.tsx` + CSS (referência Stitch Regras de Escala).
- [ ] Seletores mês/ano; lista com setor por item.
- [ ] Admin: botão gerar escala; Supervisor: oculto.
- [ ] Mensagem amigável quando API retorna `ScheduleCoverageFailed` (usar `message`).

### B9.3 — Testes

- [ ] Portar cenários de `SchedulesPage.test.tsx`.
- [ ] bUnit: erro 400 coverage → exibe mensagem da API.

### Critério de pronto (B9)

- Geração e consulta funcionam contra API de dev.
- Erro de cobertura exibido igual ao React.

---

## B10 — Testes finais, deploy e cutover

**Objetivo:** Confiança para trocar o frontend em produção.

**Duração sugerida:** 5–7 dias

### B10.1 — Suíte de testes

- [ ] Cobertura bUnit mínima acordada (auth, guards, 4 módulos).
- [ ] Smoke E2E manual documentado (checklist abaixo).
- [ ] Opcional: Playwright contra Blazor na porta 4864.

### B10.2 — Docker e Nginx

- [ ] Novo `src/Web.Blazor/Dockerfile` (SDK publish WASM + Nginx).
- [ ] Atualizar `docker-compose.prod.yml` para build Blazor em vez de Node.
- [ ] Manter proxy `/api` → serviço `api`.
- [ ] Validar `ApiBaseUrl` vazio (same-origin) e cenário split com CORS.

### B10.3 — CI

- [ ] Pipeline: `dotnet test` inclui testes bUnit do Web.Blazor.
- [ ] Remover ou marcar `allow-failure` temporário nos testes npm do React até B11.

### B10.4 — Cutover (janela controlada)

- [ ] Deploy Blazor em staging com mesma API.
- [ ] Executar checklist de fumaça (seção abaixo).
- [ ] Trocar tráfego produção: Nginx passa a servir WASM.
- [ ] Monitorar 401/403/5xx e logs por 24–48h.

### B10.5 — Documentação

- [ ] Atualizar `README.md` (comandos Blazor, remover React como primário).
- [ ] Atualizar `roadmap.md` e `agents.md` (stack frontend Blazor).
- [ ] Registrar data do cutover neste arquivo.

### Critério de pronto (B10)

- Compose prod sobe com Blazor; smoke checklist 100% OK.
- Time consegue rodar só API + Blazor localmente seguindo README.

---

## B11 — Descomissionamento do React

**Objetivo:** Remover dívida duplicada com segurança.

**Duração sugerida:** 2–3 dias (após período de observação)

### B11.1 — Período de observação

- [ ] Mínimo **1 sprint** com Blazor em produção sem incidentes P1/P2 de UI.
- [ ] Lista de bugs Blazor triada; blockers zerados.

### B11.2 — Remoção

- [ ] Remover ou arquivar `src/Web` (React).
- [ ] Remover `package.json`, pipeline Node, Dockerfile React antigo.
- [ ] Limpar referências a Vite/`VITE_*` onde não aplicável.
- [ ] Renomear opcional: `src/Web.Blazor` → `src/Web` (somente se o time quiser path final limpo — **etapa opcional separada**).

### B11.3 — Pós-remoção

- [ ] CI mais rápido (sem `npm ci`).
- [ ] Atualizar `.env.example` e portas documentadas.

### Critério de pronto (B11)

- Repositório sem dependência Node para o frontend.
- Documentação única coerente com Blazor.

---

## Checklist de fumaça (cutover — B10.4)

Executar como **Admin** e como **Supervisor**:

- [ ] `/signup` cria tenant; login com novo admin funciona.
- [ ] Login seed dev (`admin@local.com`) funciona.
- [ ] Logout limpa sessão; rota `/app` redireciona para login.
- [ ] Token expirado → redirect login com feedback.
- [ ] `/app/sectors`: Admin CRUD; Supervisor read-only.
- [ ] `/app/security-guards`: filtros; Admin edita setores; inativar/reativar.
- [ ] `/app/unavailable-days`: Admin save batch; Supervisor read-only.
- [ ] `/app/schedules`: consulta mês; Admin gera; erro coverage legível.
- [ ] Bottom nav e ícones ativos corretos em todas as telas.
- [ ] Isolamento multitenant: usuário de tenant A não vê dados de B (smoke manual).

---

## Estimativa consolidada (calma, 1 dev .NET)

| Fase | Duração | Acumulado |
|---|---|---|
| B0 | 2–3 dias | ~3 dias |
| B1 | 2–4 dias | ~1 semana |
| B2 | 3–5 dias | ~2 semanas |
| B3 | 3–4 dias | ~2,5 semanas |
| B4 | 4–5 dias | ~3,5 semanas |
| B5 | 3–4 dias | ~4 semanas |
| B6 | 4–5 dias | ~5 semanas |
| B7 | 5–7 dias | ~6,5 semanas |
| B8 | 5–7 dias | ~8 semanas |
| B9 | 4–6 dias | ~9 semanas |
| B10 | 5–7 dias | ~10 semanas |
| B11 | 2–3 dias | ~10,5 semanas |

**Total orientativo:** 9–11 semanas com 1 desenvolvedor, ou **5–6 semanas** com 2 devs (B6–B9 em paralelo após B5).

> Respeitar buffers entre fases para revisão visual e correções — não comprimir B7 e B8 na mesma semana.

---

## Riscos e mitigação

| Risco | Impacto | Mitigação |
|---|---|---|
| Regressão Admin vs Supervisor | Alto | Matriz de permissões + bUnit por tela |
| Divergência visual | Médio | Copiar CSS literalmente; screenshot diff |
| JWT/interop frágil | Alto | Testes unitários + spike B0 |
| Duplicar trabalho React/Blazor | Médio | Freeze React na B4 |
| Calendário/indisponibilidades | Alto | Fase B8 isolada; não misturar com B7 |
| Deploy WASM (cache, MIME) | Médio | Validar Nginx cedo em B10.2 |

---

## Definition of Done (global da migração)

A migração só é considerada **concluída** quando:

1. Todas as fases **B0–B11** estão marcadas concluídas.
2. Checklist de fumaça passa em staging e produção.
3. `README.md` e `agents.md` descrevem Blazor como frontend oficial.
4. `src/Web` React foi removido ou arquivado (B11).
5. Não há regressão conhecida P1/P2 aberta na UI Blazor.

---

## Referências no repositório

- **ADR 001:** [docs/adr/001-blazor-wasm-frontend.md](docs/adr/001-blazor-wasm-frontend.md)
- **Convenções Blazor (B0.3):** [docs/frontend-blazor-conventions.md](docs/frontend-blazor-conventions.md)
- **Solution .NET:** [SafetyScale.sln](SafetyScale.sln)
- Frontend atual (React): `src/Web`
- **Spike Blazor (B0.2):** `src/Web.Blazor`
- Rotas: `src/Web/src/app/routes.tsx`
- Auth: `src/Web/src/shared/auth/`
- API (inalterada nesta trilha): `src/Api`
- Roadmap geral do produto: `roadmap.md`
- Convenções de agentes: `agents.md`

---

## Histórico de revisões

| Data | Versão | Notas |
|---|---|---|
| 2026-06-22 | 1.0 | Roadmap inicial da migração React → Blazor WASM |
| 2026-06-22 | 1.1 | B0.1 concluída — ADR 001; Premissas expandidas; matriz `ApiBaseUrl`; CORS dual dev |
| 2026-06-22 | 1.2 | B0.2 concluída — POC `src/Web.Blazor` (spike, sessionStorage, CORS 4864, fonts) |
| 2026-06-22 | 1.3 | B0.3 concluída — guia de convenções; freeze React; fase B0 fechada |
| 2026-06-22 | 1.4 | B1.1 concluída — `SafetyScale.sln`, `Directory.Build.props`, WASM standalone |
| 2026-06-22 | 1.5 | B1.2 concluída — estrutura de pastas formalizada em `src/Web.Blazor` |
| 2026-06-22 | 1.6 | B1.3 concluída — estilos globais consolidados (`app.css`, fonts, icons.svg) |
| 2026-06-22 | 1.7 | B1.4 concluída — dev experience (`scripts/dev-blazor.sh`, docs raiz, CORS + ApiBaseUrl) |
