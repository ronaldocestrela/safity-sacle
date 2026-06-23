# SafetyScale.Web.Blazor

Frontend **Blazor WebAssembly** do SafetyScale (migração React → Blazor). Parte da solution [`SafetyScale.sln`](../../SafetyScale.sln) na raiz.

Spike técnica B0.2 validada; bootstrap B1.1 integrado à solution; **estrutura de pastas B1.2** formalizada; **estilos globais B1.3** consolidados; **dev experience B1.4** com script raiz.

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

## Configuração

| Arquivo | Propósito |
|---|---|
| `wwwroot/appsettings.json` | `ApiBaseUrl` vazio (produção / same-origin) |
| `wwwroot/appsettings.Development.json` | `ApiBaseUrl`: `http://localhost:5003` |

Carregamento: `WebAssemblyHostBuilder.CreateDefault` + `ASPNETCORE_ENVIRONMENT=Development` em `launchSettings.json`.

## SessionStorage (paridade React)

- Chave: `safetyscale.auth.session`
- Valor: `{ "token": "<jwt>" }`
- JS: `wwwroot/js/sessionStorage.js`
- C#: `Services/Auth/BrowserSessionStorage.cs`

## CORS

A API em Development aceita origens `http://localhost:4863` (React) e `http://localhost:4864` (Blazor) — ver `src/Api/appsettings.Development.json`.

## Dev experience (B1.4)

- Porta dev: **4864** (`Properties/launchSettings.json`).
- API em dev: `ApiBaseUrl` = `http://localhost:5003` (`wwwroot/appsettings.Development.json`).
- Sem proxy `/api` no WASM — CORS dual-origin na API (`4863` React + `4864` Blazor).
- Script raiz: [`scripts/dev-blazor.sh`](../../scripts/dev-blazor.sh).

## Próximas fases

- **B2** — auth provider, handlers HTTP, parser JWT, `ApiClient` por domínio
