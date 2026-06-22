# SafetyScale.Web.Blazor — Spike B0.2

POC técnica da migração React → Blazor WebAssembly. Valida CORS, `ApiBaseUrl`, interop `sessionStorage` e fluxo login + `/api/health`.

Decisões de arquitetura: [ADR 001](../../docs/adr/001-blazor-wasm-frontend.md).

## Pré-requisitos

- .NET SDK 10
- SQL Server local (mesmo do backend) — a API precisa subir para testes de integração
- API em `http://localhost:5003` (perfil `http`)

## Executar

Terminal 1 — API:

```bash
dotnet run --project src/Api/SafetyScale.Api.csproj
```

Terminal 2 — Blazor spike (porta **4864**):

```bash
dotnet run --project src/Web.Blazor/SafetyScale.Web.Blazor.csproj
```

Abra `http://localhost:4864`.

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
- C#: `Services/BrowserSessionStorage.cs`

## CORS

A API em Development aceita origens `http://localhost:4863` (React) e `http://localhost:4864` (Blazor) — ver `src/Api/appsettings.Development.json`.

## Próximas fases

- **B1** — bootstrap formal (solution, pastas, README definitivo)
- **B2** — auth provider, handlers HTTP, parser JWT

Convenções do time: [docs/frontend-blazor-conventions.md](../../docs/frontend-blazor-conventions.md)
