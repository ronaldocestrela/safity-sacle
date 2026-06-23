# SafetyScale.Web.Blazor

Frontend **Blazor WebAssembly** do SafetyScale (migração React → Blazor). Parte da solution [`SafetyScale.sln`](../../SafetyScale.sln) na raiz.

Spike técnica B0.2 validada; bootstrap B1.1 integrado à solution; **estrutura de pastas B1.2** formalizada; **estilos globais B1.3** consolidados; **dev experience B1.4** com script raiz; **configuração B2.1** (`ApiBaseUrl`) formalizada; **cliente HTTP B2.2** com handlers centralizados; **JWT e sessão B2.3** com `AuthenticationStateProvider`; **DTOs e tipos B2.4** com `JsonSerializerOptions` global; **testes unitários B2.5** da infra auth/HTTP; **roteamento B3.1** com paridade `routes.tsx`; **autorização de rotas B3.2** com `AuthorizeRouteView` e `RoleAuthorizeView`; **AppLayout shell B3.3** com bottom nav, header condicional e logout; **testes bUnit B3.4** de guards e nav ativa; **Home pública B4.1** com smoke de API e links login/signup; **Login B4.2** com formulário, erros e redirect; **Signup B4.3** com cadastro de empresa e redirect pós-cadastro; **testes bUnit B4.4** de fluxos públicos login/signup.

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
 │   ├── App/             # área autenticada (placeholder B1.2)
 │   └── Auth/            # login, signup (placeholder B1.2)
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
- `Pages/Auth/Login.razor` e `Pages/Auth/RegisterTenant.razor` implementados em B4.2/B4.3; `Pages/App/Welcome.razor` permanece **placeholder** até B5.
- `NavMenu` do template Blazor foi removido; layout neutro em `Layout/MainLayout.razor`.

## Estilos globais (B1.3)

- `wwwroot/css/app.css` — paridade com [`src/Web/src/index.css`](../Web/src/index.css): reset, `body`, Inter, Material Symbols.
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
| `/app` | `Pages/App/Welcome.razor` | Placeholder |

## Home pública (B4.1)

Paridade com [`src/Web/src/features/home/HomePage.tsx`](../Web/src/features/home/HomePage.tsx):

- Título, lead e painel de smoke da API.
- Links para `/login` e `/signup`.
- Smoke em `Services/Api/HomeApiSmoke.cs`: `GET /api/health` sem token; estados `loading` / `ok` / `error` com `aria-live`.
- `401/403` tratados como resposta válida da API (mensagem informativa, não erro).

Arquivos: `Pages/Home.razor`, `Pages/Home.razor.css`.

## Login (B4.2)

Paridade com [`src/Web/src/features/auth/LoginPage.tsx`](../Web/src/features/auth/LoginPage.tsx):

- Card Stitch (header escuro, campos e-mail/senha, botão **Entrar**).
- Submit via `AuthSessionService.LoginAsync` → `POST /api/auth/login`.
- Erros: credenciais inválidas (`401`) e rede/token inválido.
- Redirect pós-sucesso para `returnUrl` seguro ou `/app`; se já autenticado, redirect imediato.
- Query params: `reason=session-expired`, `registrationSuccess=true`, `email=` (pré-preenchimento pós-signup B4.3).
- Links para `/signup` e `/`.

Arquivos: `Pages/Auth/Login.razor`, `Pages/Auth/Login.razor.css`.

## Signup (B4.3)

Paridade com [`src/Web/src/features/tenant-registration/RegisterTenantPage.tsx`](../Web/src/features/tenant-registration/RegisterTenantPage.tsx):

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
| `Models/Sectors/` | `SectorDto` |
| `Models/SecurityGuards/` | `SecurityGuardDto` |
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
| `TestHelpers/BlazorComponentTestBase.cs` | Base bUnit (auth, config, navegação) |
| `TestHelpers/PublicAuthTestHelper.cs` | Stubs HTTP para páginas públicas de auth (B4.4) |
| `TestHelpers/JwtTestUtils.cs` | Geração de JWT unsigned para testes |
| `TestHelpers/FakeJsRuntime.cs` | Mock in-memory de `sessionStorageInterop` |

Executar:

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj --filter "FullyQualifiedName~SafetyScale.Tests.Web.Blazor"
```

## Roteamento (B3.1)

Paridade estrutural com [`src/Web/src/app/routes.tsx`](../Web/src/app/routes.tsx):

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

Paridade com [`src/Web/src/app/AppLayout.tsx`](../Web/src/app/AppLayout.tsx):

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

A API em Development aceita origens `http://localhost:4863` (React) e `http://localhost:4864` (Blazor) — ver `src/Api/appsettings.Development.json`.

## Dev experience (B1.4)

- Porta dev: **4864** (`Properties/launchSettings.json`).
- API em dev: `ApiBaseUrl` = `http://localhost:5003` (`wwwroot/appsettings.Development.json`).
- Sem proxy `/api` no WASM — CORS dual-origin na API (`4863` React + `4864` Blazor).
- Script raiz: [`scripts/dev-blazor.sh`](../../scripts/dev-blazor.sh).

## Próximas fases

- **B5** — Welcome/Dashboard, AccessDenied, AppHeader
