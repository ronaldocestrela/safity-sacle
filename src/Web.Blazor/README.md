# SafetyScale.Web.Blazor

Frontend **Blazor WebAssembly** do SafetyScale (migração React → Blazor). Parte da solution [`SafetyScale.sln`](../../SafetyScale.sln) na raiz.

Spike técnica B0.2 validada; bootstrap B1.1 integrado à solution; **estrutura de pastas B1.2** formalizada; **estilos globais B1.3** consolidados; **dev experience B1.4** com script raiz; **configuração B2.1** (`ApiBaseUrl`) formalizada; **cliente HTTP B2.2** com handlers centralizados; **JWT e sessão B2.3** com `AuthenticationStateProvider`; **DTOs e tipos B2.4** com `JsonSerializerOptions` global; **testes unitários B2.5** da infra auth/HTTP; **roteamento B3.1** com paridade `routes.tsx`; **autorização de rotas B3.2** com `AuthorizeRouteView` e `RoleAuthorizeView`; **AppLayout shell B3.3** com bottom nav, header condicional e logout; **testes bUnit B3.4** de guards e nav ativa; **Home pública B4.1** com smoke de API e links login/signup; **Login B4.2** com formulário, erros e redirect; **Signup B4.3** com cadastro de empresa e redirect pós-cadastro; **testes bUnit B4.4** de fluxos públicos login/signup; **Dashboard B5.1** com sessão multitenant, KPIs, calendário e detalhe do dia; **AccessDenied B5.2** com mensagem de permissão e link de retorno ao dashboard; **AppHeader B5.3** compartilhado nas telas administrativas com título, subtítulo e logout; **testes bUnit B5.4** de Welcome e AccessDenied; **cliente API setores B6.1** com `SectorsApiClient` (list/create/update/active/inactive) e DTOs de request/response; **UI setores B6.2** com listagem, filtros, CRUD Admin, leitura Supervisor e paridade visual React; **testes bUnit B6.3** de setores (Supervisor read-only, Admin create, empty state); **módulo seguranças B7** com API CRUD+setores, UI completa (filtros, modais, permissões) e 6 testes bUnit; **módulo indisponibilidades B8** com calendário mensal navegável, draft local → **SAVE RESTRICTIONS**, API e 10 testes bUnit/unit; **módulo escalas B9** com consulta mensal, geração Admin, erro de cobertura e 8 testes bUnit.

Decisões de arquitetura: [ADR 001](../../docs/adr/001-blazor-wasm-frontend.md).  
Convenções: [docs/frontend-blazor-conventions.md](../../docs/frontend-blazor-conventions.md).

## Estrutura de pastas (B1.2)

```text
src/Web.Blazor/
 ├── Components/          # AppHeader, MonthCalendar, … (B4+)
 │   └── Calendar/
 ├── Layout/              # MainLayout (público), AppLayout (shell autenticado B3.3)
 ├── Pages/
 │   ├── Home.razor       # Home pública (B4.1)
 │   ├── App/             # área autenticada (Welcome B5.1, Sectors B6, SecurityGuards B7, UnavailableDays B8, Schedules B9)
 │   └── Auth/            # login, signup (B4.2/B4.3)
 ├── Services/
 │   ├── Api/             # AppConfiguration, ApiUrlBuilder
 │   └── Auth/            # BrowserSessionStorage
 ├── Models/              # DTOs espelhando JSON camelCase da API (B2+)
 ├── wwwroot/
 │   ├── css/
 │   ├── js/
 │   ├── appsettings.json
 │   ├── appsettings.Development.json
 │   └── icons.svg
 ├── Program.cs
 └── README.md
```

**Notas:**

- A POC da B0.2 foi substituída pela Home pública em `Pages/Home.razor` (B4.1).
- `Pages/Auth/Login.razor` e `Pages/Auth/RegisterTenant.razor` implementados em B4.2/B4.3; `Pages/App/Welcome.razor` implementado em B5.1.
- `NavMenu` do template Blazor foi removido; layout neutro em `Layout/MainLayout.razor`.

## Estilos globais (B1.3)

- `wwwroot/css/app.css` — paridade com [`src/Web/src/index.css`](../../archive/legacy-react-web/src/index.css): reset, `body`, Inter, Material Symbols.
- `wwwroot/index.html` — links Google Fonts (Inter + Material Symbols Outlined), `css/app.css`, `SafetyScale.Web.Blazor.styles.css`.
- `wwwroot/icons.svg` — copiado de `src/Web/public/icons.svg` (B1.2).
- **Telas reais (B4+):** estilos por componente/página via `.razor.css` (scoped CSS), copiando `*.module.css` do React.
- **Exceção temporária:** classes `.spike-*` em `app.css` para placeholders — remover após B4/B5.
- Removidas sobras do template Blazor/Bootstrap (`.btn`, `.content`, validação, `.form-floating`).

## Solution e dependências

- Projeto: `src/Web.Blazor/SafetyScale.Web.Blazor.csproj`
- **Sem** `ProjectReference` para Api, Application, Domain ou Infrastructure (WASM standalone).
- DTOs da API são **espelhados** manualmente em `Models/` — não existe projeto `Contracts` compartilhado (decisão B1.1).

Compilar via solution:

```bash
dotnet build SafetyScale.sln
```

## Pré-requisitos

- .NET SDK 10
- SQL Server local (mesmo do backend) — a API precisa subir para testes de integração
- API em `http://localhost:5003` (perfil `http`)

## Executar

**Recomendado — um comando na raiz (API + Blazor):**

```bash
./scripts/dev-blazor.sh
```

O script sobe a API em `http://localhost:5003` (background), aguarda disponibilidade e inicia o Blazor em `http://localhost:4864`. `Ctrl+C` encerra ambos. Documentação completa no [README raiz](../../README.md#rodar-o-blazor-contra-a-api-local).

**Alternativa manual — dois terminais:**

Terminal 1 — API:

```bash
dotnet run --project src/Api/SafetyScale.Api.csproj
```

Terminal 2 — Blazor (porta **4864**):

```bash
dotnet run --project src/Web.Blazor/SafetyScale.Web.Blazor.csproj
```

Abra `http://localhost:4864`.

### Rotas disponíveis (B1.2)

| Rota | Página | Estado |
|---|---|---|
| `/` | `Pages/Home.razor` | Home pública B4.1 |
| `/login` | `Pages/Auth/Login.razor` | Login B4.2 |
| `/signup` | `Pages/Auth/RegisterTenant.razor` | Signup B4.3 |
| `/app` | `Pages/App/Welcome.razor` | Dashboard B5.1 |

## Home pública (B4.1)

Paridade com [`src/Web/src/features/home/HomePage.tsx`](../../archive/legacy-react-web/src/features/home/HomePage.tsx):

- Título, lead e painel de smoke da API.
- Links para `/login` e `/signup`.
- Smoke em `Services/Api/HomeApiSmoke.cs`: `GET /api/health` sem token; estados `loading` / `ok` / `error` com `aria-live`.
- `401/403` tratados como resposta válida da API (mensagem informativa, não erro).

Arquivos: `Pages/Home.razor`, `Pages/Home.razor.css`.

## Login (B4.2)

Paridade com [`src/Web/src/features/auth/LoginPage.tsx`](../../archive/legacy-react-web/src/features/auth/LoginPage.tsx):

- Card Stitch (header escuro, campos e-mail/senha, botão **Entrar**).
- Submit via `AuthSessionService.LoginAsync` → `POST /api/auth/login`.
- Erros: credenciais inválidas (`401`) e rede/token inválido.
- Redirect pós-sucesso para `returnUrl` seguro ou `/app`; se já autenticado, redirect imediato.
- Query params: `reason=session-expired`, `registrationSuccess=true`, `email=` (pré-preenchimento pós-signup B4.3).
- Links para `/signup` e `/`.

Arquivos: `Pages/Auth/Login.razor`, `Pages/Auth/Login.razor.css`.

## Signup (B4.3)

Paridade com [`src/Web/src/features/tenant-registration/RegisterTenantPage.tsx`](../../archive/legacy-react-web/src/features/tenant-registration/RegisterTenantPage.tsx):

- Card wide com 5 campos: empresa, administrador, e-mail, senha, confirmação.
- Validação local (obrigatoriedade + senhas iguais).
- Submit via `TenantsRegistrationClient` → `POST /api/tenants/register`.
- Erros: `400` validação/senha, `409` e-mail ou slug, rede.
- Sucesso → `/login?registrationSuccess=true&email=...` (banner e pré-preenchimento na Login B4.2).
- Redirect para `/app` se já autenticado.

Arquivos: `Pages/Auth/RegisterTenant.razor`, `Pages/Auth/RegisterTenant.razor.css`, `Services/Api/TenantsRegistrationClient.cs`.

## Testes de fluxos públicos (B4.4)

Suíte bUnit para login e signup com HTTP stubado (sem chamadas reais à API):

| Arquivo | Cobertura |
|---|---|
| `Pages/LoginPageTests.cs` | Submit com HTTP 200 → navega para `/app`; 401 → mensagem de credenciais inválidas |
| `Pages/RegisterTenantPageTests.cs` | Submit com HTTP 409 → mensagem amigável de e-mail duplicado |
| `TestHelpers/PublicAuthTestHelper.cs` | Factory de `AuthSessionService`, `TenantsRegistrationClient` e `TestNavigationManager` com `FuncHttpMessageHandler` |

## Dashboard (B5.1)

Paridade com [`src/Web/src/features/app/WelcomePage.tsx`](../../archive/legacy-react-web/src/features/app/WelcomePage.tsx):

- `AppHeader` com título, avatar/iniciais, notificações e logout.
- Faixa de sessão: e-mail, perfil (roles) e tenant.
- KPIs: guards ativos/inativos, assignments e weekend shifts.
- Calendário mensal (`MonthCalendar` + `MonthGrid`) com seleção de dia e lista de turnos.
- Empty state quando não há escala do mês; CTA Admin vs Supervisor → `/app/schedules`.
- Atalhos para setores, seguranças, disponibilidade e agendamentos.
- Loading skeleton e banners de erro com retry.

Clients read-only antecipados (mínimo para dashboard):

| Cliente | Endpoint |
|---|---|
| `SecurityGuardsApiClient.ListAsync` | `GET /api/security-guards` |
| `SchedulesApiClient.GetByMonthYearAsync` | `GET /api/schedules/month/{m}/year/{y}` (404 → null) |

Arquivos: `Pages/App/Welcome.razor`, `Pages/App/Welcome.razor.css`, `Components/AppHeader.razor`, `Components/Calendar/MonthCalendar.razor`, `Services/Calendar/MonthGrid.cs`, `Services/Api/SecurityGuardsApiClient.cs`, `Services/Api/SchedulesApiClient.cs`.

## Access denied (B5.2)

Paridade com [`src/Web/src/app/routes/AccessDeniedPage.tsx`](../../archive/legacy-react-web/src/app/routes/AccessDeniedPage.tsx):

- Mensagem de permissão negada com contexto de perfil Supervisor/Admin.
- Link **Voltar ao início** → `/app` via `NavLink`.
- CSS scoped em `Pages/App/AccessDenied.razor.css`.
- Integrada ao pipeline existente: `RoleAuthorizeView` / `RedirectToAccessDenied` → `/app/access-denied`; layout `AppLayout` com header visível.

Arquivos: `Pages/App/AccessDenied.razor`, `Pages/App/AccessDenied.razor.css`.

## AppHeader compartilhado (B5.3)

Componente reutilizável em `Components/AppHeader.razor` para as cinco rotas Stitch administrativas (`/app`, `/app/sectors`, `/app/security-guards`, `/app/unavailable-days`, `/app/schedules`).

Props suportadas:

| Prop | Uso |
|---|---|
| `Title` | Título principal da tela |
| `Subtitle` | Subtítulo contextual abaixo do título |
| `Email` | Iniciais do avatar |
| `AvatarSrc` / `AvatarAlt` | Avatar com imagem (ex.: Schedules) |
| `ShowNotifications` | Botão de notificações |
| `ShowLogout` + `OnLogout` | Logout via `AuthSessionService` |

Paridade por rota:

| Rota | Título | Logout |
|---|---|---|
| `/app` | SentryOps | sim |
| `/app/sectors` | Gestão de setores | sim |
| `/app/security-guards` | Gestão de seguranças | sim |
| `/app/unavailable-days` | Availability | não (paridade React) |
| `/app/schedules` | SentryOps Management | sim (+ avatar Stitch) |

`/app/access-denied` continua usando apenas o header do `AppLayout` shell (sem `AppHeader` de página).

Arquivos: `Components/AppHeader.razor`, `Components/AppHeader.razor.css`, páginas em `Pages/App/*.razor`.

## Testes da área autenticada base (B5.4)

Suíte bUnit para Welcome e AccessDenied:

| Arquivo | Cobertura |
|---|---|
| `Pages/WelcomePageTests.cs` | Sessão mock exibe `user@example.com` na faixa de sessão |
| `Pages/AccessDeniedPageTests.cs` | Link **Voltar ao início** aponta para `/app` |
| `TestHelpers/AppDashboardTestHelper.cs` | Sessão autenticada + stubs de guards/schedules para Welcome |

## Cliente API setores (B6.1)

Paridade com [`src/Web/src/features/sectors/sectorsApi.ts`](../../archive/legacy-react-web/src/features/sectors/sectorsApi.ts):

| Método | HTTP | Endpoint |
|---|---|---|
| `ListAsync(isActive?)` | GET | `/api/sectors[?isActive=true\|false]` |
| `CreateAsync` | POST | `/api/sectors` → `{ id }` |
| `UpdateAsync` | PUT | `/api/sectors/{id}` |
| `InactivateAsync` | PATCH | `/api/sectors/{id}/inactive` |
| `ActivateAsync` | PATCH | `/api/sectors/{id}/active` |

- DTOs: `CreateSectorRequestDto`, `UpdateSectorRequestDto`, `CreateSectorResponseDto` em `Models/Sectors/` (abordagem DTO-first).
- `Description` normalizada para `null` quando vazia (paridade React).
- Erros via `ApiClientResponseHelper.EnsureOkAsync` com fallbacks em PT.
- Registro DI: `AddScoped<SectorsApiClient>()` em `Program.cs`.

Arquivos: `Services/Api/SectorsApiClient.cs`, `Models/Sectors/*.cs`.

## UI setores (B6.2)

Paridade com [`src/Web/src/features/sectors/SectorsPage.tsx`](../../archive/legacy-react-web/src/features/sectors/SectorsPage.tsx):

- Busca client-side por nome/descrição + chips **Todos os setores** / **Apenas ativos** (reload via API).
- Cards com badge Ativo/Inativo, meta de posições/dia e toggle de status.
- **Admin:** FAB criar, editar pelo nome, create/edit modal, ativar/inativar (confirmação na inativação).
- **Supervisor:** leitura completa; toggle e ações de escrita desabilitadas/ocultas.
- Validação UX: nome obrigatório; posições inteiras 1–500; erros 400 da API no formulário.
- Banners de sucesso/erro com dismiss; erro de carga com **Tentar novamente**; empty state por filtro.

Arquivos: `Pages/App/Sectors.razor`, `Pages/App/Sectors.razor.css`.

## Testes do módulo setores (B6.3)

Suíte bUnit em [`src/Tests/Web.Blazor/Pages/SectorsPageTests.cs`](../Tests/Web.Blazor/Pages/SectorsPageTests.cs), paridade com `SectorsPage.test.tsx`:

| Teste | Cobertura |
|---|---|
| `Supervisor_WithLoadedList_HidesWriteControls` | Lista renderizada; sem FAB/edit; toggle `disabled` |
| `Admin_SubmitCreate_RefreshesListWithNewSector` | Mock HTTP stateful (GET → POST → GET); valida payload e novo item |
| `AuthenticatedUser_WithEmptyList_ShowsEmptyStateMessage` | Empty state `Não há setores encontrados para este filtro.` |

Helper: `TestHelpers/SectorsPageTestHelper.cs` (sessão JWT + stubs `/api/sectors`).

## Cliente API seguranças (B7.1)

Paridade com [`src/Web/src/features/security-guards/securityGuardsApi.ts`](../../archive/legacy-react-web/src/features/security-guards/securityGuardsApi.ts):

| Método | HTTP | Endpoint |
|---|---|---|
| `ListAsync(isActive?)` | GET | `/api/security-guards[?isActive=true\|false]` |
| `CreateAsync` | POST | `/api/security-guards` → `{ id }` |
| `UpdateAsync` | PUT | `/api/security-guards/{id}` |
| `InactivateAsync` | PATCH | `/api/security-guards/{id}/inactive` |
| `ActivateAsync` | PATCH | `/api/security-guards/{id}/active` |
| `SetSectorsAsync` | PUT | `/api/security-guards/{id}/sectors` |

- DTOs: `CreateSecurityGuardRequestDto`, `UpdateSecurityGuardRequestDto`, `CreateSecurityGuardResponseDto`, `SetSecurityGuardSectorsRequestDto`.
- Integração com `SectorsApiClient` para catálogo de setores (filtro e multi-select no modal).

Arquivos: `Services/Api/SecurityGuardsApiClient.cs`, `Models/SecurityGuards/*.cs`.

## UI seguranças (B7.2–B7.4)

Paridade com [`src/Web/src/features/security-guards/SecurityGuardsPage.tsx`](../../archive/legacy-react-web/src/features/security-guards/SecurityGuardsPage.tsx):

- Busca por nome/ID/setor + chips **All Personnel** / **Active Only** + filtro por setor.
- Cards com ID sintético `#SO-XXXX`, badge Ativo/Inativo, setores ou **Férias** (inativo).
- **Admin:** FAB criar, editar pelo nome, modal create/edit com checkboxes de setores ativos, ativar/inativar (confirmação na inativação).
- **Supervisor:** leitura completa; controles de escrita ocultos/desabilitados.
- Fluxo create/edit em duas etapas: nome (POST/PUT) + setores (`PUT .../sectors`).

Arquivos: `Pages/App/SecurityGuards.razor`, `Pages/App/SecurityGuards.razor.css`.

## Testes do módulo seguranças (B7.5)

Suíte bUnit em [`src/Tests/Web.Blazor/Pages/SecurityGuardsPageTests.cs`](../Tests/Web.Blazor/Pages/SecurityGuardsPageTests.cs):

| Teste | Cobertura |
|---|---|
| `Supervisor_WithLoadedList_HidesWriteControls` | Lista; sem FAB/edit; toggle `disabled` |
| `Admin_WithLoadedList_ShowsWriteControls` | FAB e botão de edição visíveis |
| `AuthenticatedUser_WithEmptyList_ShowsEmptyStateMessage` | Empty state PT-BR |
| `Admin_WithForbiddenList_ShowsAlertMessage` | Erro de carga com mensagem da API |
| `Admin_SubmitCreateWithoutName_ShowsValidationError` | Validação `Enter a name.` |
| `Admin_SubmitCreate_AssignsSectorsAndRefreshesList` | POST + PUT sectors + refresh |

Helper: `TestHelpers/SecurityGuardsPageTestHelper.cs`.

## Cliente API indisponibilidades (B8.2)

Paridade com [`src/Web/src/features/unavailable-days/unavailableDaysApi.ts`](../../archive/legacy-react-web/src/features/unavailable-days/unavailableDaysApi.ts):

| Método | HTTP | Endpoint |
|---|---|---|
| `ListByGuardAsync` | GET | `/api/security-guards/{guardId}/unavailable-days` |
| `AddAsync` | POST | `/api/security-guards/{guardId}/unavailable-days` → `{ id }` |
| `DeleteAsync` | DELETE | `/api/unavailable-days/{id}` |

- DTOs: `AddUnavailableDayRequestDto`, `CreateUnavailableDayResponseDto`, `UnavailableDayDto`.
- Estado draft: `Services/UnavailableDays/UnavailableDayPendingState.cs` (baseline, toggle, effective).

Arquivos: `Services/Api/UnavailableDaysApiClient.cs`, `Models/UnavailableDays/*.cs`.

## UI indisponibilidades (B8.3)

Paridade com [`src/Web/src/features/unavailable-days/UnavailableDaysPage.tsx`](../../archive/legacy-react-web/src/features/unavailable-days/UnavailableDaysPage.tsx):

- Seletor de segurança (lista completa via `SecurityGuardsApiClient.ListAsync()`).
- Calendário mensal com nav anterior/próximo (`MonthCalendar` + `MonthGrid`).
- Estados: loading/error/retry, dia **UNAVAIL**, foco, pending add/remove.
- **Admin:** toggle de dias, reason opcional, barra fixa **SAVE RESTRICTIONS** (DELETE removes → POST adds).
- **Supervisor:** calendário somente leitura (sem save, reason ou toggle).

Arquivos: `Pages/App/UnavailableDays.razor`, `Pages/App/UnavailableDays.razor.css`.

## Testes do módulo indisponibilidades (B8.4)

Suíte bUnit em [`src/Tests/Web.Blazor/Pages/UnavailableDaysPageTests.cs`](../Tests/Web.Blazor/Pages/UnavailableDaysPageTests.cs):

| Teste | Cobertura |
|---|---|
| `Supervisor_WithLoadedCalendar_HidesAdminControlsAndDisablesDayButtons` | Sem save/reason; dias `disabled` |
| `Admin_WithForbiddenGuardsList_ShowsAlertMessage` | Erro de carga de seguranças |
| `Admin_WithExistingUnavailableDay_ShowsUnavailTag` | Tag **UNAVAIL** da API |
| `Admin_WithPendingAdd_SubmitsSaveAndRefreshesList` | POST add + refresh |
| `Admin_WithPendingRemove_SubmitsDeleteOnly` | DELETE remove |
| `Admin_WithDuplicateDateError_ShowsAlertMessage` | Erro 409 |
| `Admin_WithDaysLoadError_ShowsAlertMessage` | Erro de carga de dias |

Unitários: [`src/Tests/Web.Blazor/Calendar/MonthGridTests.cs`](../Tests/Web.Blazor/Calendar/MonthGridTests.cs) (grade, keys, padding).

Helper: `TestHelpers/UnavailableDaysPageTestHelper.cs`.

## Cliente API escalas (B9.1)

Paridade com [`src/Web/src/features/schedules/schedulesApi.ts`](../../archive/legacy-react-web/src/features/schedules/schedulesApi.ts):

| Método | HTTP | Endpoint |
|---|---|---|
| `GetByMonthYearAsync` | GET | `/api/schedules/month/{month}/year/{year}` (404 → `null`) |
| `GenerateAsync` | POST | `/api/schedules/generate` → `{ id }` |

- DTOs: `GenerateMonthlyScheduleRequestDto`, `CreateScheduleResponseDto`, `ScheduleCoverageFailureResponse`.
- Erro 400 de cobertura: parse de `ScheduleCoverageFailureResponse.message` em `ApiException`.

Arquivos: `Services/Api/SchedulesApiClient.cs`, `Models/Schedules/*.cs`.

## UI escalas (B9.2)

Paridade com [`src/Web/src/features/schedules/SchedulesPage.tsx`](../../archive/legacy-react-web/src/features/schedules/SchedulesPage.tsx):

- Seletores mês/ano + validação de período.
- Lista de agendamentos com setor, badges **Final de semana** / **Inativo**.
- **Admin:** botão **Gerar agendamento** + recarga pós-sucesso.
- **Supervisor:** consulta somente leitura (sem botão de geração).
- Banner único para sucesso, not-found e erro de cobertura (`ScheduleCoverageFailed`).

Arquivos: `Pages/App/Schedules.razor`, `Pages/App/Schedules.razor.css`.

## Testes do módulo escalas (B9.3)

Suíte bUnit em [`src/Tests/Web.Blazor/Pages/SchedulesPageTests.cs`](../Tests/Web.Blazor/Pages/SchedulesPageTests.cs):

| Teste | Cobertura |
|---|---|
| `Mount_LoadsRosterOnInit` | GET inicial ao montar |
| `Admin_ShowsGenerateButton` | Botão gerar visível |
| `Supervisor_HidesGenerateButton` | Botão gerar oculto |
| `WithLoadedSchedule_ShowsAssignments` | Nomes, setor, badges |
| `WithMissingSchedule_ShowsNotFoundBanner` | Alert not-found |
| `Admin_Generate_SubmitsPostAndReloads` | POST generate + refresh |
| `Admin_GenerateCoverageFailure_ShowsApiMessage` | Erro 400 coverage |

Unitário: [`src/Tests/Web.Blazor/Api/SchedulesDtoDeserializationTests.cs`](../Tests/Web.Blazor/Api/SchedulesDtoDeserializationTests.cs).

Helper: `TestHelpers/SchedulesPageTestHelper.cs`.

## Configuração (B2.1)

| Arquivo | Propósito |
|---|---|
| `wwwroot/appsettings.json` | `ApiBaseUrl` vazio — produção / same-origin (`/api/...` relativo) |
| `wwwroot/appsettings.Development.json` | `ApiBaseUrl`: `http://localhost:5003` — dev com CORS |

Carregamento: `WebAssemblyHostBuilder.CreateDefault` + `ASPNETCORE_ENVIRONMENT=Development` em `launchSettings.json`.

Serviços: `Services/Api/AppConfiguration.cs` (lê e normaliza `ApiBaseUrl`) e `Services/Api/ApiUrlBuilder.cs` (monta URLs).

### Paridade com React

| React | Blazor |
|---|---|
| `VITE_API_BASE_URL` em `.env` | `ApiBaseUrl` em `wwwroot/appsettings*.json` |
| `normalizeApiBase()` em `shared/config/env.ts` | `AppConfiguration.NormalizeApiBase()` |
| `buildApiUrl()` / `apiUrl()` | `ApiUrlBuilder.Build(path)` |
| Dev vazio + proxy Vite `/api` | Dev `http://localhost:5003` + CORS (sem proxy WASM) |
| Prod vazio → `/api` relativo (Nginx) | Prod vazio → `/api` relativo |

Exemplos de `ApiUrlBuilder.Build`:

- Base vazia + `"api/health"` → `/api/health`
- Base `http://localhost:5003` + `"/api/health"` → `http://localhost:5003/api/health`

## Cliente HTTP (B2.2)

Camada compartilhada em `Services/Api/` — paridade com React `shared/api/http.ts` e `readApiError.ts`:

| Componente | Função |
|---|---|
| `ApiHttpClient` | Monta URLs via `ApiUrlBuilder`; `Accept: application/json` |
| `BearerTokenHandler` | Injeta `Authorization: Bearer` quando há token em sessionStorage |
| `UnauthorizedRedirectHandler` | 401 com token pré-existente → limpa sessão + navega `/login` |
| `ApiErrorReader` | Extrai mensagens de `errors[]`, `errors{}`, `detail`, `title`, `message` |
| `ApiRequestOptions` | `SkipAuthRedirect` (login público), `SkipBearerInjection` (POC sem Bearer) |

Registro em `Program.cs`: pipeline manual `UnauthorizedRedirectHandler` → `BearerTokenHandler` → `HttpClientHandler`.

## JWT e sessão (B2.3)

| Componente | Função |
|---|---|
| `JwtParser` | Parse payload JWT (roles, `tenant_id`, `exp`, email) — paridade `jwt.ts` |
| `JwtSessionStorage` | `GetSessionAsync`, `SaveTokenAsync`, `ClearAsync` sobre `BrowserSessionStorage` |
| `AuthSession` | Modelo com `Token`, `Email`, `Roles`, `TenantId` |
| `CustomAuthStateProvider` | `AuthenticationStateProvider` + `NotifyAuthenticationStateChanged` |
| `AuthSessionService` | `LoginAsync`, `LogoutAsync`, `GetSessionAsync` |

- `App.razor` usa `CascadingAuthenticationState`; `Program.cs` registra `AddAuthorizationCore()`.
- 401 no handler limpa sessão **e** notifica auth state antes de navegar `/login`.
- Storage continua `{ "token": "..." }`; claims derivadas do JWT na leitura.

## DTOs e serialização JSON (B2.4)

| Componente | Função |
|---|---|
| `AppJsonSerializerOptions` | Opções globais: camelCase + case-insensitive (paridade API) |
| `Models/Auth/` | `LoginRequestDto`, `LoginResponseDto` (+ `AuthSession`, `UserRole`) |
| `Models/Tenants/` | `RegisterTenantRequestDto`, `RegisterTenantResponseDto` |
| `Models/Sectors/` | `SectorDto`, `CreateSectorRequestDto`, `UpdateSectorRequestDto`, `CreateSectorResponseDto` |
| `Models/SecurityGuards/` | `SecurityGuardDto`, `CreateSecurityGuardRequestDto`, `UpdateSecurityGuardRequestDto`, `CreateSecurityGuardResponseDto`, `SetSecurityGuardSectorsRequestDto` |
| `Models/UnavailableDays/` | `UnavailableDayDto` |
| `Models/Schedules/` | `ScheduleItemDto`, `MonthlyScheduleDto`, `ScheduleCoverageFailureResponse` |

- DTOs espelham manualmente `src/Api/Contracts/` e `src/Application/*/Common` (sem `ProjectReference` ao backend).
- `ApiHttpClient.PostJsonAsync` e `ReadJsonAsync` usam as opções globais; `BrowserSessionStorage` serializa `{ "token" }` compatível com React.
- `AuthSessionService.LoginAsync` usa `LoginRequestDto` / `LoginResponseDto` tipados.

## Testes unitários de infra (B2.5)

Suíte em [`src/Tests/Web.Blazor/`](../Tests/Web.Blazor/) (xUnit + FluentAssertions + bUnit):

| Arquivo | Cobertura |
|---|---|
| `Auth/JwtParserTests.cs` | Parse claims, roles, expiração, tenant — paridade `jwt.test.ts` |
| `Auth/SessionStorageTests.cs` | `BrowserSessionStorage` + `JwtSessionStorage` — paridade `session.test.ts` |
| `Api/UnauthorizedRedirectHandlerTests.cs` | 401 com/sem token, `SkipAuthRedirect`, pass-through |
| `Routing/RouteAuthorizationTests.cs` | Não autenticado em `/app` → `/login?returnUrl=...` |
| `Components/RoleAuthorizeViewTests.cs` | Supervisor bloqueado em gate Admin-only; Admin renderiza conteúdo |
| `Layout/AppLayoutNavTests.cs` | Bottom nav marca item ativo por rota (`/app`, `/app/sectors`) |
| `Pages/LoginPageTests.cs` | Login submit sucesso e 401 (B4.4) |
| `Pages/RegisterTenantPageTests.cs` | Signup conflito 409 (B4.4) |
| `Pages/WelcomePageTests.cs` | Welcome renderiza e-mail autenticado mock (B5.4) |
| `Pages/AccessDeniedPageTests.cs` | AccessDenied link para `/app` (B5.4) |
| `Pages/SectorsPageTests.cs` | Setores: Supervisor read-only, Admin create, empty state (B6.3) |
| `Pages/SecurityGuardsPageTests.cs` | Seguranças: read-only, create+sectors, empty/error (B7.5) |
| `Pages/UnavailableDaysPageTests.cs` | Indisponibilidades: read-only, save add/remove, erros (B8.4) |
| `Pages/SchedulesPageTests.cs` | Escalas: role-gating, load, generate, coverage error (B9.3) |
| `Calendar/MonthGridTests.cs` | Grade mensal, keys de data, padding (B8.4) |
| `Api/SchedulesDtoDeserializationTests.cs` | Deserialização DTOs de escala (B9.3) |
| `TestHelpers/BlazorComponentTestBase.cs` | Base bUnit (auth, config, navegação) |
| `TestHelpers/PublicAuthTestHelper.cs` | Stubs HTTP para páginas públicas de auth (B4.4) |
| `TestHelpers/AppDashboardTestHelper.cs` | Stubs HTTP para dashboard Welcome (B5.4) |
| `TestHelpers/SectorsPageTestHelper.cs` | Stubs HTTP para página de setores (B6.3) |
| `TestHelpers/SecurityGuardsPageTestHelper.cs` | Stubs HTTP para página de seguranças (B7.5) |
| `TestHelpers/UnavailableDaysPageTestHelper.cs` | Stubs HTTP para página de indisponibilidades (B8.4) |
| `TestHelpers/SchedulesPageTestHelper.cs` | Stubs HTTP para página de escalas (B9.3) |
| `TestHelpers/JwtTestUtils.cs` | Geração de JWT unsigned para testes |
| `TestHelpers/FakeJsRuntime.cs` | Mock in-memory de `sessionStorageInterop` |

Executar:

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj --filter "FullyQualifiedName~SafetyScale.Tests.Web.Blazor"
```

## Roteamento (B3.1)

Paridade estrutural com [`src/Web/src/app/routes.tsx`](../../archive/legacy-react-web/src/app/routes.tsx):

| Rota | Página | Layout |
|---|---|---|
| `/` | `Pages/Home.razor` | `MainLayout` (público) |
| `/login` | `Pages/Auth/Login.razor` | `MainLayout` |
| `/signup` | `Pages/Auth/RegisterTenant.razor` | `MainLayout` |
| `/app` | `Pages/App/Welcome.razor` | `AppLayout` |
| `/app/access-denied` | `Pages/App/AccessDenied.razor` | `AppLayout` |
| `/app/sectors` | `Pages/App/Sectors.razor` | `AppLayout` |
| `/app/security-guards` | `Pages/App/SecurityGuards.razor` | `AppLayout` |
| `/app/unavailable-days` | `Pages/App/UnavailableDays.razor` | `AppLayout` |
| `/app/schedules` | `Pages/App/Schedules.razor` | `AppLayout` |
| `*` (desconhecida) | redirect → `/` | `Components/RedirectToHome.razor` |

## AppLayout shell (B3.3)

Paridade com [`src/Web/src/app/AppLayout.tsx`](../../archive/legacy-react-web/src/app/AppLayout.tsx):

- **Bottom nav fixa** — 5 itens: Dashboard (`/app`, match exato), Sectors, Guards, Availability, Schedules.
- **Estado ativo** — `NavLink` + `ActiveClass`; ícones Material Symbols com `FILL 1` no item ativo.
- **Header condicional** — oculto nas 5 rotas principais (telas Stitch); visível em `/app/access-denied` com e-mail, badge Admin/Supervisor e botão **Sair**.
- **Logout** — `AuthSessionService.LogoutAsync(navigateToLogin: true)` → `/login` (replace).

Arquivos: `Layout/AppLayout.razor`, `Layout/AppLayout.razor.css`.

## Autorização de rotas (B3.2)

| Componente | Função |
|---|---|
| `AuthorizeRouteView` (`App.razor`) | Exige autenticação em páginas com `[Authorize]` |
| `RedirectToLogin` | Não autenticado → `/login?returnUrl=<path>` |
| `RedirectToAccessDenied` | Autenticado sem permissão de rota → `/app/access-denied` |
| `RoleAuthorizeView` | Rotas de módulo exigem `Admin` ou `Supervisor` |

**Matriz (paridade React):**

| Rota | Auth | Role guard |
|---|---|---|
| `/app`, `/app/access-denied` | sim | não |
| `/app/sectors`, `/app/security-guards`, `/app/unavailable-days`, `/app/schedules` | sim | `Admin` ou `Supervisor` |

- `Login.razor` lê `returnUrl` e `reason=session-expired` (consumo completo na B4.2).
- `UnauthorizedRedirectHandler` redireciona 401 com token para `/login?reason=session-expired`.
- Escrita CRUD (Admin only) permanece em UI/API (B6–B9), não no guard de rota.

## SessionStorage (paridade React)

- Chave: `safetyscale.auth.session`
- Valor: `{ "token": "<jwt>" }`
- JS: `wwwroot/js/sessionStorage.js`
- C#: `Services/Auth/BrowserSessionStorage.cs` (interop) + `JwtSessionStorage.cs` (sessão)

## CORS

A API em Development aceita origem `http://localhost:4864` (Blazor) — ver `src/Api/appsettings.Development.json`.

## Dev experience (B1.4)

- Porta dev: **4864** (`Properties/launchSettings.json`).
- API em dev: `ApiBaseUrl` = `http://localhost:5003` (`wwwroot/appsettings.Development.json`).
- Sem proxy `/api` no WASM — CORS na API para origem `4864`.
- Script raiz: [`scripts/dev-blazor.sh`](../../scripts/dev-blazor.sh).

## Produção e cutover (B10)

- **Dockerfile:** [`Dockerfile`](Dockerfile) — `dotnet publish` WASM + Nginx.
- **Nginx:** [`nginx.conf`](nginx.conf) — proxy `/api`, MIME WASM, cache `_framework`.
- **Compose prod:** [`docker-compose.prod.yml`](../../docker-compose.prod.yml) — serviço `web` aponta para este projeto.
- **Staging:** [`docker-compose.staging.yml`](../../docker-compose.staging.yml).
- **Gate testes:** [`scripts/test-blazor.sh`](../../scripts/test-blazor.sh) (59 bUnit).
- **Verify deploy:** [`scripts/verify-blazor-deploy.sh`](../../scripts/verify-blazor-deploy.sh).
- **Smoke manual:** [`docs/smoke-cutover-checklist.md`](../../docs/smoke-cutover-checklist.md).
- **Runbook:** [`docs/cutover-runbook.md`](../../docs/cutover-runbook.md).

Cutover produção registrado em **2026-06-23** — ver [`roadmap-blazor-migration.md`](../../roadmap-blazor-migration.md) B10.

## Próximas fases

- Migração React → Blazor **concluída (B0–B11)**. Melhorias de produto seguem em `src/Web.Blazor`.
