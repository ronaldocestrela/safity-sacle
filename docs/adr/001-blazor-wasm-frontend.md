# ADR 001 — Frontend Blazor WebAssembly Standalone

## Status

**Accepted** — 2026-06-22

## Contexto

O SafetyScale possui uma SPA **React** em `src/Web` (Vite, porta dev **4863**, proxy `/api` → API em `http://localhost:5003`) e uma **Web API** .NET 10 em `src/Api` inalterada nesta trilha.

A migração planejada substitui gradualmente o React por **Blazor WebAssembly**, preservando identidade visual (Stitch, Material Symbols, Inter) e paridade funcional com as rotas F0–F4 já entregues. Detalhamento das fases: [`roadmap-blazor-migration.md`](../../roadmap-blazor-migration.md).

Restrições relevantes:

- A API REST e o contrato JWT (`tenant_id`, roles `Admin`/`Supervisor`) permanecem como estão.
- Produção usa Nginx servindo SPA estática + proxy `/api` → serviço `api:8080` (same-origin, sem CORS).
- React e Blazor devem coexistir em desenvolvimento até o cutover (fase B10).

## Decisão

Adotar **Blazor WebAssembly Standalone** (.NET 10) como substituto do frontend React, com as seguintes definições:

### 1. Modelo de hospedagem

| Item | Valor |
|---|---|
| Template | Blazor WebAssembly **Standalone** |
| Pasta | `src/Web.Blazor/` |
| Projeto / assembly | `SafetyScale.Web.Blazor.csproj` |
| Root namespace | `SafetyScale.Web.Blazor` |
| React legado | `src/Web/` permanece até fase **B11** |

**Solution file:** criar `SafetyScale.sln` na raiz na fase **B1.1**, incluindo Api, Application, Domain, Infrastructure, Tests e Web.Blazor. Hoje os projetos são referenciados apenas via `.csproj`.

### 2. Portas e coexistência em dev

| Serviço | Porta | Observação |
|---|---|---|
| React (Vite) | **4863** | Inalterado — [`src/Web/vite.config.ts`](../../src/Web/vite.config.ts) |
| Blazor WASM dev | **4864** | Nova — `launchSettings.json` na fase B1 |
| API HTTP | **5003** | Perfil `http` padrão — [`src/Api/Properties/launchSettings.json`](../../src/Api/Properties/launchSettings.json) |
| API HTTPS (opcional) | **7104** | Paridade com `VITE_DEV_API_PROXY_TARGET` do React |

Regras:

- Ambos os frontends podem rodar simultaneamente contra a mesma API local.
- **Não reutilizar 4863** para Blazor (conflito de processo e CORS).
- Em Development, a API deve aceitar **duas origens CORS**: `http://localhost:4863` e `http://localhost:4864`. Implementação prevista em **B0.2 ou B1.4** — alteração em [`src/Api/appsettings.Development.json`](../../src/Api/appsettings.Development.json).

### 3. Estratégia de `ApiBaseUrl`

O dev server do Blazor WASM **não possui proxy `/api` nativo** equivalente ao Vite. A estratégia por ambiente:

| Ambiente | `ApiBaseUrl` | Mecanismo | CORS |
|---|---|---|---|
| Dev local (Blazor) | `http://localhost:5003` | `HttpClient` chama API diretamente | Sim — origem `http://localhost:4864` |
| Dev local (React) | *(vazio)* | Proxy Vite `/api` | Sim — origem `4863` (já configurado) |
| Produção / Compose | *(vazio)* | Nginx proxy `/api` → `api:8080` | Desligado (same-origin) |
| Split origin (opcional) | URL absoluta da API | Chamada cross-origin | `CORS_ORIGINS` na API |

**Configuração no Blazor:** [`wwwroot/appsettings.json`](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/configuration) carregado no startup WASM:

```json
{
  "ApiBaseUrl": ""
}
```

- **Dev:** sobrescrever via `wwwroot/appsettings.Development.json` com `"ApiBaseUrl": "http://localhost:5003"` (detalhe fino validado no spike B0.2).
- **Produção:** `ApiBaseUrl` vazio ⇒ URLs relativas `/api/...`, idêntico ao React com `VITE_API_BASE_URL` vazio.

**Paridade com React** ([`src/Web/src/shared/config/env.ts`](../../src/Web/src/shared/config/env.ts)):

- Base vazia → path relativo (`/api/health`).
- Base preenchida → URL absoluta (`http://localhost:5003/api/health`).
- Normalizar removendo barra final.

**Docker (fase B10):** substituir `VITE_API_BASE_URL` por **`ApiBaseUrl`** injetado no build/publish do WASM. Semântica mantida: vazio = same-origin via Nginx.

### 4. Convenções derivadas (aplicar a partir de B1)

| Tipo | Local |
|---|---|
| Páginas autenticadas | `Pages/App/` |
| Páginas públicas | `Pages/Auth/` |
| Clientes HTTP | `Services/Api/*ApiClient.cs` |
| DTOs (JSON camelCase) | `Models/` |
| Estilo | Scoped CSS (`.razor.css`) 1:1 com React; sem biblioteca de UI nesta migração |

## Alternativas consideradas

### Blazor Server — descartado

| Critério | WASM Standalone | Blazor Server |
|---|---|---|
| Modelo atual | SPA estática + API REST | Estado no servidor + SignalR |
| Auth JWT | Paridade com React (`sessionStorage`) | Exige repensar auth (cookie/circuito) |
| Deploy | `dotnet publish` + Nginx | Escala horizontal mais complexa |
| Multitenancy | Token no browser; API resolve `tenant_id` | UX de reconexão SignalR |
| Risco | Reescreve UI; API intacta | Mudaria modelo operacional inteiro |

### Blazor WASM Hosted — descartado

Hosted (projeto Server + Client) facilitaria proxy `/api` em dev, mas adiciona um host ASP.NET extra só para servir WASM. Produção já usa Nginx como host estático; Standalone mantém simetria com o React atual.

### Proxy customizado no dev server Blazor — descartado

Possível com host customizado, mas aumenta complexidade sem benefício em produção. Descartado em favor de **CORS + URL absoluta em dev**.

## Consequências

### Positivas

- Stack frontend unificada em .NET 10 após cutover.
- Deploy de produção permanece same-origin (Nginx + `/api`), sem mudança operacional na API.
- Migração incremental: React e Blazor coexistem em dev.
- Auth JWT e multitenancy reutilizam o mesmo contrato da API.

### Negativas / trade-offs

- Dev local Blazor exige CORS explícito (duas origens: 4863 + 4864).
- Reescrita completa da UI — zero reutilização de código React.
- Bundle WASM inicial maior que SPA React otimizada com Vite.
- Testes Vitest/RTL serão substituídos por bUnit (reescrita gradual).

### Encadeamento com fases

| Fase | Dependência desta ADR |
|---|---|
| B0.2 | Spike usa porta 4864, `ApiBaseUrl` dev `http://localhost:5003`, CORS 4864 |
| B1 | Projeto `SafetyScale.Web.Blazor`, `launchSettings` 4864, `SafetyScale.sln` |
| B2 | `HttpClient` + leitura de `ApiBaseUrl` de `wwwroot/appsettings.json` |
| B10 | `ApiBaseUrl` vazio em prod; Dockerfile Blazor substitui Node |

## Gate checklist (B0.2)

| Pergunta | Resposta |
|---|---|
| Por que não Blazor Server? | Modelo SignalR + servidor incompatível com SPA estática + JWT em `sessionStorage`; mudaria deploy e auth. |
| Por que não WASM Hosted? | Host ASP.NET extra desnecessário; produção já usa Nginx. |
| Como dev Blazor fala com API sem proxy Vite? | `ApiBaseUrl` = `http://localhost:5003` + CORS para origem `4864`. |
| Como produção evita CORS? | `ApiBaseUrl` vazio; Nginx proxy `/api` na mesma origem. |
| React e Blazor coexistem em quais portas? | React **4863**, Blazor **4864**, API **5003**. |
| Qual variável substitui `VITE_API_BASE_URL`? | **`ApiBaseUrl`** em `wwwroot/appsettings.json` (build/publish no Docker). |

## Referências

- [`roadmap-blazor-migration.md`](../../roadmap-blazor-migration.md) — trilha completa B0–B11
- [`README.md`](../../README.md) — Frontend React atual, CORS, deploy
- [`src/Web/vite.config.ts`](../../src/Web/vite.config.ts) — proxy dev React
- [`src/Web/nginx.conf`](../../src/Web/nginx.conf) — produção same-origin
- [`src/Web/src/shared/config/env.ts`](../../src/Web/src/shared/config/env.ts) — lógica de base URL React
