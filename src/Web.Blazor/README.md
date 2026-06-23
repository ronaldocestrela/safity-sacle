# SafetyScale.Web.Blazor

Frontend **Blazor WebAssembly** do SafetyScale (migração React → Blazor). Parte da solution [`SafetyScale.sln`](../../SafetyScale.sln) na raiz.

Spike técnica B0.2 validada; bootstrap B1.1 integrado à solution; **estrutura de pastas B1.2** formalizada; **estilos globais B1.3** consolidados; **dev experience B1.4** com script raiz; **configuração B2.1** (`ApiBaseUrl`) formalizada; **cliente HTTP B2.2** com handlers centralizados; **JWT e sessão B2.3** com `AuthenticationStateProvider`; **DTOs e tipos B2.4** com `JsonSerializerOptions` global; **testes unitários B2.5** da infra auth/HTTP.

Decisões de arquitetura: [ADR 001](../../docs/adr/001-blazor-wasm-frontend.md).  
Convenções: [docs/frontend-blazor-conventions.md](../../docs/frontend-blazor-conventions.md).

## Estrutura de pastas (B1.2)

```text
src/Web.Blazor/
 ├── Components/          # AppHeader, MonthCalendar, … (B4+)
 │   └── Calendar/
 ├── Layout/              # MainLayout (shell neutro até B3)
 ├── Pages/
 │   ├── Home.razor       # POC B0.2 temporária em /
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

- A POC da B0.2 foi preservada em `Pages/Home.razor` (rota `/`) até a fase B2.
- `Pages/Auth/Login.razor`, `Pages/Auth/RegisterTenant.razor` e `Pages/App/Welcome.razor` são **placeholders** preparatórios para B4/B5 — sem lógica de negócio ainda.
- `NavMenu` do template Blazor foi removido; layout neutro em `Layout/MainLayout.razor`.

## Estilos globais (B1.3)

- `wwwroot/css/app.css` — paridade com [`src/Web/src/index.css`](../Web/src/index.css): reset, `body`, Inter, Material Symbols.
- `wwwroot/index.html` — links Google Fonts (Inter + Material Symbols Outlined), `css/app.css`, `SafetyScale.Web.Blazor.styles.css`.
- `wwwroot/icons.svg` — copiado de `src/Web/public/icons.svg` (B1.2).
- **Telas reais (B4+):** estilos por componente/página via `.razor.css` (scoped CSS), copiando `*.module.css` do React.
- **Exceção temporária:** classes `.spike-*` em `app.css` para POC B0.2 e placeholders — remover após B2/B4.
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
| `/` | `Pages/Home.razor` | POC B0.2 completa |
| `/login` | `Pages/Auth/Login.razor` | Placeholder |
| `/signup` | `Pages/Auth/RegisterTenant.razor` | Placeholder |
| `/app` | `Pages/App/Welcome.razor` | Placeholder |

## O que a spike valida

| Item | Como testar na UI |
|---|---|
| Inter + Material Symbols | Ícone `shield_person` no header |
| `ApiBaseUrl` dev | Painel Configuração → `http://localhost:5003` |
| Health sem token | Botão → HTTP **401** (sucesso esperado) |
| Token inválido | Botão → HTTP **401** |
| Interop sessionStorage | Salvar token fake → ler de volta; limpar sessão |
| Login dev | E-mail/senha padrão → token salvo → health **200** |
| Persistência | Após login, recarregar página → token ainda visível |

Credenciais padrão no form (seed Development): `admin@local.com` / `Mudar@13`.

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

A POC em `Pages/Home.razor` usa `AuthSessionService.LoginAsync` para login dev e `ApiHttpClient` para health.

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

Suíte em [`src/Tests/Web.Blazor/`](../Tests/Web.Blazor/) (xUnit + FluentAssertions):

| Arquivo | Cobertura |
|---|---|
| `Auth/JwtParserTests.cs` | Parse claims, roles, expiração, tenant — paridade `jwt.test.ts` |
| `Auth/SessionStorageTests.cs` | `BrowserSessionStorage` + `JwtSessionStorage` — paridade `session.test.ts` |
| `Api/UnauthorizedRedirectHandlerTests.cs` | 401 com/sem token, `SkipAuthRedirect`, pass-through |
| `TestHelpers/JwtTestUtils.cs` | Geração de JWT unsigned para testes |
| `TestHelpers/FakeJsRuntime.cs` | Mock in-memory de `sessionStorageInterop` |

Executar:

```bash
dotnet test src/Tests/SafetyScale.Tests.csproj --filter "FullyQualifiedName~SafetyScale.Tests.Web.Blazor"
```

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

- **B3** — layout, roteamento e guards de rota
